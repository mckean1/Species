using Species.Domain.Catalogs;
using Species.Domain.Discovery;
using Species.Domain.Enums;
using Species.Domain.Models;
using Species.Domain.Simulation;
using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;

namespace Species.Client.ViewModelFactories;

public static class RegionsViewModelFactory
{
    public static int GetKnownRegionCount(World world, string focalPolityId)
    {
        return GetKnownRegions(world, PlayerFocus.ResolveLeadGroup(world, focalPolityId)).Count;
    }

    public static RegionsViewModel Build(
        World world,
        string focalPolityId,
        int selectedRegionIndex,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog,
        bool isSimulationRunning = false)
    {
        var isPrimitiveWorldMode = world.Polities.Count == 0;
        var focusPolity = PlayerFocus.Resolve(world, focalPolityId);
        var focusContext = PlayerFocus.ResolveContext(world, focalPolityId);
        var focusGroup = PlayerFocus.ResolveLeadGroup(world, focalPolityId);
        
        // In primitive-world mode, show all regions since there are no groups to limit discovery
        var regionCandidates = isPrimitiveWorldMode 
            ? world.Regions.OrderBy(r => r.Name, StringComparer.Ordinal).ToArray()
            : GetKnownRegions(world, focusGroup);
            
        var selectedIndex = regionCandidates.Count == 0
            ? 0
            : Math.Clamp(selectedRegionIndex, 0, regionCandidates.Count - 1);
        var discoveryContext = focusGroup is null
            ? null
            : GroupDiscoveryContext.Create(world, focusGroup, discoveryCatalog, floraCatalog, faunaCatalog);

        var summaries = regionCandidates
            .Select(region => BuildSummary(region, world, focusPolity, focusContext, focusGroup, discoveryContext, floraCatalog, faunaCatalog, discoveryCatalog))
            .ToArray();

        var polityName = isPrimitiveWorldMode ? "Primitive World" : (focusPolity?.Name ?? "Unknown polity");

        return new RegionsViewModel(
            polityName,
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            isSimulationRunning,
            summaries,
            summaries.Length == 0 ? null : summaries[selectedIndex],
            selectedIndex);
    }

    private static IReadOnlyList<Region> GetKnownRegions(World world, PopulationGroup? focusGroup)
    {
        if (focusGroup is null)
        {
            return [];
        }

        if (focusGroup.KnownRegionIds.Count == 0)
        {
            return world.Regions
                .Where(region => string.Equals(region.Id, focusGroup.CurrentRegionId, StringComparison.Ordinal))
                .OrderBy(region => region.Name, StringComparer.Ordinal)
                .ToArray();
        }

        return world.Regions
            .Where(region => focusGroup.KnownRegionIds.Contains(region.Id) || string.Equals(region.Id, focusGroup.CurrentRegionId, StringComparison.Ordinal))
            .OrderBy(region => region.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static RegionSummary BuildSummary(
        Region region,
        World world,
        Polity? focusPolity,
        PolityContext? focusContext,
        PopulationGroup? focusGroup,
        GroupDiscoveryContext? discoveryContext,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog)
    {
        var snapshot = focusGroup is null || discoveryContext is null
            ? null
            : discoveryContext.ObserveRegion(region, focusGroup.CurrentRegionId);
        var groupsHere = world.PopulationGroups
            .Where(group => string.Equals(group.CurrentRegionId, region.Id, StringComparison.Ordinal))
            .OrderByDescending(group => group.Population)
            .ThenBy(group => group.Name, StringComparer.Ordinal)
            .ToArray();

        var exactPresenceVisible = snapshot?.IsCurrentRegion == true || snapshot?.RegionStage == DiscoveryStage.Discovered;
        var presencePopulation = groupsHere.Sum(group => group.Population);
        var topFlora = snapshot?.FloraStage == DiscoveryStage.Discovered
            ? ResolveTopPopulationNames(region.Ecosystem.FloraPopulations, floraCatalog.GetById).Take(3).ToArray()
            : Array.Empty<string>();
        var faunaVisibility = RegionFaunaVisibilityResolver.Resolve(region, snapshot, faunaCatalog);
        var discoveries = BuildDiscoveries(region, snapshot, focusGroup, discoveryCatalog);
        var carnivoreThreat = region.Ecosystem.FaunaPopulations.Sum(entry =>
        {
            var fauna = faunaCatalog.GetById(entry.Key);
            return fauna?.DietCategory == DietCategory.Carnivore ? entry.Value : 0;
        });

        var groupThreat = groupsHere.Length == 0
            ? 0
            : (int)Math.Round(groupsHere.Average(group => group.Pressures.Threat.DisplayValue), MidpointRounding.AwayFromZero);

        var threatScore = snapshot is null
            ? Math.Clamp(Math.Max(groupThreat, carnivoreThreat / 2), 0, 100)
            : (int)MathF.Round(snapshot.ThreatPressure, MidpointRounding.AwayFromZero);
        var threatText = snapshot is null
            ? threatScore switch
            {
                >= 70 => "Dangerous",
                >= 40 => "Watchful",
                _ => "Calm"
            }
            : DiscoveryPresentation.DescribeThreat(snapshot);

        var context = new List<string>();
        if (focusGroup is not null)
        {
            if (string.Equals(region.Id, focusGroup.CurrentRegionId, StringComparison.Ordinal))
            {
                context.Add("Current region of the polity");
            }

            if (focusContext is not null && string.Equals(region.Id, focusContext.HomeRegionId, StringComparison.Ordinal))
            {
                context.Add("Home region");
            }

            if (focusContext is not null && string.Equals(region.Id, focusContext.CoreRegionId, StringComparison.Ordinal))
            {
                context.Add("Core polity region");
            }

            if (region.NeighborIds.Contains(focusGroup.CurrentRegionId, StringComparer.Ordinal))
            {
                context.Add("Connected to the polity's current path");
            }
        }

        if (focusPolity is not null)
        {
            var presence = focusPolity.RegionalPresences.FirstOrDefault(item => string.Equals(item.RegionId, region.Id, StringComparison.Ordinal));
            if (presence is not null && (presence.IsCurrent || presence.MonthsSinceLastPresence <= 6))
            {
                context.Add($"Polity presence: {PolityPresentation.DescribePresenceKind(presence.Kind)}");
            }

            var settlement = focusPolity.Settlements.FirstOrDefault(item => item.IsActive && string.Equals(item.RegionId, region.Id, StringComparison.Ordinal));
            if (settlement is not null)
            {
                context.Add($"Primary site nearby: {settlement.Name}");
            }
        }

        if (groupsHere.Length > 1)
        {
            context.Add("Other groups are present");
        }

        if (snapshot?.WaterStage == DiscoveryStage.Discovered && region.WaterAvailability == WaterAvailability.High)
        {
            context.Add("Water is reliable");
        }
        else if (snapshot?.WaterStage == DiscoveryStage.Discovered && region.WaterAvailability == WaterAvailability.Low)
        {
            context.Add("Water is scarce");
        }

        if (snapshot?.RegionStage == DiscoveryStage.Discovered && region.Fertility >= 0.70)
        {
            context.Add("Land looks productive");
        }
        else if (snapshot?.RegionStage == DiscoveryStage.Discovered && region.Fertility <= 0.35)
        {
            context.Add("Resources appear thin");
        }

        var opportunities = new List<string>();
        if (snapshot is not null && snapshot.WaterStage != DiscoveryStage.Unknown)
        {
            opportunities.Add(DiscoveryPresentation.DescribeWater(snapshot));
        }

        if (snapshot?.RegionStage == DiscoveryStage.Discovered && region.Fertility >= 0.65)
        {
            opportunities.Add("Fertile ground");
        }
        else if (snapshot?.RegionStage == DiscoveryStage.Encountered)
        {
            opportunities.Add("Land quality remains uncertain");
        }

        if (topFlora.Length > 0)
        {
            opportunities.Add($"Useful flora: {string.Join(", ", topFlora.Take(2))}");
        }
        else if (snapshot is not null)
        {
            opportunities.Add(DiscoveryPresentation.DescribeFoodSigns(snapshot.FloraStage, "Useful plants discovered", "Useful plants observed", "Flora not yet observed"));
        }

        opportunities.Add(DescribeMaterials(region, snapshot));

        var risks = new List<string>();
        if (snapshot is not null)
        {
            risks.Add(DiscoveryPresentation.DescribeThreat(snapshot));
        }
        else if (threatScore >= 40)
        {
            risks.Add($"{threatText} local threats");
        }

        if (snapshot?.WaterStage == DiscoveryStage.Discovered && region.WaterAvailability == WaterAvailability.Low)
        {
            risks.Add("Limited water");
        }
        else if (snapshot?.WaterStage == DiscoveryStage.Encountered)
        {
            risks.Add("Water availability uncertain");
        }

        if (groupsHere.Any(group => group.Pressures.Overcrowding.DisplayValue >= 60))
        {
            risks.Add("Crowding pressure");
        }

        return new RegionSummary(
            region.Id,
            region.Name,
            snapshot is null ? "Known" : DiscoveryPresentation.DescribeRegionFamiliarity(snapshot),
            snapshot?.RegionStage == DiscoveryStage.Discovered || snapshot?.IsKnownRegion == true ? region.Biome.ToString() : "Unknown",
            snapshot is null ? region.WaterAvailability.ToString() : DiscoveryPresentation.DescribeWater(snapshot),
            snapshot?.RegionStage == DiscoveryStage.Discovered ? region.Fertility.ToString("0.00") : snapshot?.RegionStage == DiscoveryStage.Encountered ? "Encountered" : "Unknown",
            exactPresenceVisible ? presencePopulation : 0,
            exactPresenceVisible ? presencePopulation.ToString("N0") : (groupsHere.Length > 0 ? "Signs" : "None"),
            BuildGroupPresence(groupsHere, exactPresenceVisible),
            topFlora.Length > 0 ? topFlora : ["None known"],
            faunaVisibility.DisplayLabel,
            faunaVisibility.VisibleEntries,
            BuildMaterialLines(region, snapshot),
            discoveries,
            context.Count > 0 ? context.ToArray() : ["No special connection noted"],
            opportunities.Count > 0 ? opportunities.ToArray() : ["No standout opportunities"],
            risks.Count > 0 ? risks.ToArray() : ["No major risks detected"],
            BuildBiology(region, snapshot, floraCatalog, faunaCatalog),
            BuildFossils(region, snapshot),
            threatText,
            threatScore);
    }

    private static IReadOnlyList<string> BuildDiscoveries(Region region, RegionDiscoverySnapshot? snapshot, PopulationGroup? focusGroup, DiscoveryCatalog discoveryCatalog)
    {
        if (focusGroup is null)
        {
            return ["General regional discovery only"];
        }

        var discoveries = new List<string>();

        if (snapshot is not null)
        {
            discoveries.Add($"Familiarity: {DiscoveryPresentation.Describe(snapshot.OverallStage)}");
        }

        if (focusGroup.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalWaterSourcesDiscoveryId(region.Id)))
        {
            discoveries.Add("Water sources discovered");
        }

        if (focusGroup.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalFloraDiscoveryId(region.Id)))
        {
            discoveries.Add("Local flora discovered");
        }

        if (focusGroup.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalFaunaDiscoveryId(region.Id)))
        {
            discoveries.Add("Local fauna discovered");
        }

        if (discoveryCatalog.IsLocalRegionDiscoveryKnown(focusGroup.KnownDiscoveryIds, region.Id))
        {
            discoveries.Add($"Region discovered: {region.Name}");
        }

        return discoveries.Count > 0 ? discoveries : ["Not yet discovered"];
    }

    private static IReadOnlyList<string> BuildMaterialLines(Region region, RegionDiscoverySnapshot? snapshot)
    {
        if (snapshot?.RegionStage == DiscoveryStage.Discovered || snapshot?.IsCurrentRegion == true)
        {
            var stores = region.MaterialProfile.Opportunities;
            return
            [
                $"Timber {stores.Timber}",
                $"Stone {stores.Stone}",
                $"Fiber {stores.Fiber}",
                $"Clay {stores.Clay}",
                $"Hides {stores.Hides}"
            ];
        }

        if (snapshot?.RegionStage == DiscoveryStage.Encountered)
        {
            return ["Material strengths remain uncertain"];
        }

        return ["Material potential not yet known"];
    }

    private static IReadOnlyList<string> BuildBiology(Region region, RegionDiscoverySnapshot? snapshot, FloraSpeciesCatalog floraCatalog, FaunaSpeciesCatalog faunaCatalog)
    {
        if (!(snapshot?.IsCurrentRegion == true || snapshot?.RegionStage == DiscoveryStage.Discovered || snapshot?.FaunaStage == DiscoveryStage.Discovered || snapshot?.FloraStage == DiscoveryStage.Discovered))
        {
            return ["Biological differences are not yet understood"];
        }

        var lines = new List<string>();
        lines.AddRange(region.Ecosystem.FaunaProfiles.Values
            .Where(profile => !profile.IsExtinct && profile.DivergenceScore >= 40)
            .OrderByDescending(profile => profile.DivergenceScore)
            .Take(2)
            .Select(profile =>
            {
                var definition = faunaCatalog.GetById(profile.SpeciesId);
                var name = definition?.Name ?? profile.SpeciesId;
                return $"{name}: {profile.DivergenceStage} [{profile.Traits.ToSummary()}]";
            }));
        lines.AddRange(region.Ecosystem.FloraProfiles.Values
            .Where(profile => !profile.IsExtinct && profile.DivergenceScore >= 40)
            .OrderByDescending(profile => profile.DivergenceScore)
            .Take(2)
            .Select(profile =>
            {
                var definition = floraCatalog.GetById(profile.SpeciesId);
                var name = definition?.Name ?? profile.SpeciesId;
                return $"{name}: {profile.DivergenceStage} [{profile.Traits.ToSummary()}]";
            }));

        return lines.Count > 0 ? lines : ["No major regional divergence is evident"];
    }

    private static IReadOnlyList<string> BuildFossils(Region region, RegionDiscoverySnapshot? snapshot)
    {
        if (!(snapshot?.IsCurrentRegion == true || snapshot?.RegionStage == DiscoveryStage.Discovered))
        {
            return ["Deep history is unknown here"];
        }

        return region.Ecosystem.FossilRecords
            .OrderByDescending(record => record.RecordedYear)
            .ThenByDescending(record => record.RecordedMonth)
            .Take(2)
            .Select(record => $"{record.FormName}: {record.TraitSummary}")
            .DefaultIfEmpty("No notable fossils known")
            .ToArray();
    }

    private static string DescribeMaterials(Region region, RegionDiscoverySnapshot? snapshot)
    {
        if (snapshot?.RegionStage == DiscoveryStage.Discovered || snapshot?.IsCurrentRegion == true)
        {
            var top = region.MaterialProfile.Opportunities.AsDictionary()
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key)
                .Take(2)
                .Select(entry => $"{entry.Key.ToString().ToLowerInvariant()} {entry.Value}")
                .ToArray();
            return top.Length == 0
                ? "Material opportunities look thin"
                : $"Material strengths: {string.Join(", ", top)}";
        }

        return snapshot?.RegionStage == DiscoveryStage.Encountered
            ? "Material opportunities remain uncertain"
            : "Material opportunities not yet known";
    }

    private static IReadOnlyList<string> BuildGroupPresence(IReadOnlyList<PopulationGroup> groupsHere, bool exactPresenceVisible)
    {
        if (groupsHere.Count == 0)
        {
            return ["No notable presence"];
        }

        if (!exactPresenceVisible)
        {
            return ["Signs of habitation reported"];
        }

        return groupsHere.Select(group => $"{group.Name} ({group.Population:N0})").ToArray();
    }

    private static IReadOnlyList<string> ResolveTopPopulationNames<TDefinition>(
        IReadOnlyDictionary<string, int> populations,
        Func<string, TDefinition?> resolveDefinition)
        where TDefinition : class
    {
        return populations
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => resolveDefinition(entry.Key) switch
            {
                FloraSpeciesDefinition flora => flora.Name,
                FaunaSpeciesDefinition fauna => fauna.Name,
                _ => entry.Key
            })
            .ToArray();
    }

    private static string FormatMonthYear(int month, int year)
    {
        var monthText = month switch
        {
            1 => "Jan",
            2 => "Feb",
            3 => "Mar",
            4 => "Apr",
            5 => "May",
            6 => "Jun",
            7 => "Jul",
            8 => "Aug",
            9 => "Sep",
            10 => "Oct",
            11 => "Nov",
            12 => "Dec",
            _ => "Jan"
        };

        return $"{monthText} {year:D3}";
    }
}
