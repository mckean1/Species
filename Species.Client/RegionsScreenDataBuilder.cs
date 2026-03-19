using Species.Domain.Catalogs;
using Species.Domain.Enums;
using Species.Domain.Knowledge;
using Species.Domain.Models;

public static class RegionsScreenDataBuilder
{
    public static RegionsScreenData Build(
        World world,
        string focalGroupId,
        int selectedRegionIndex,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog)
    {
        var focusGroup = PlayerFocus.Resolve(world, focalGroupId);
        var regionCandidates = GetKnownRegions(world, focusGroup);
        var selectedIndex = regionCandidates.Count == 0
            ? 0
            : Math.Clamp(selectedRegionIndex, 0, regionCandidates.Count - 1);
        var knowledgeContext = focusGroup is null
            ? null
            : GroupKnowledgeContext.Create(world, focusGroup, discoveryCatalog, floraCatalog, faunaCatalog);

        var summaries = regionCandidates
            .Select(region => BuildSummary(region, world, focusGroup, knowledgeContext, floraCatalog, faunaCatalog, discoveryCatalog))
            .ToArray();

        return new RegionsScreenData(
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
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
        PopulationGroup? focusGroup,
        GroupKnowledgeContext? knowledgeContext,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog)
    {
        var snapshot = focusGroup is null || knowledgeContext is null
            ? null
            : knowledgeContext.ObserveRegion(region, focusGroup.CurrentRegionId);
        var groupsHere = world.PopulationGroups
            .Where(group => string.Equals(group.CurrentRegionId, region.Id, StringComparison.Ordinal))
            .OrderByDescending(group => group.Population)
            .ThenBy(group => group.Name, StringComparer.Ordinal)
            .ToArray();

        var exactPresenceVisible = snapshot?.IsCurrentRegion == true || snapshot?.ConditionsKnowledge == KnowledgeLevel.Known;
        var presencePopulation = groupsHere.Sum(group => group.Population);
        var topFlora = snapshot?.FloraKnowledge == KnowledgeLevel.Known
            ? ResolveTopPopulationNames(region.Ecosystem.FloraPopulations, floraCatalog.GetById).Take(3).ToArray()
            : Array.Empty<string>();
        var topFauna = snapshot?.FaunaKnowledge == KnowledgeLevel.Known
            ? ResolveTopPopulationNames(region.Ecosystem.FaunaPopulations, faunaCatalog.GetById).Take(3).ToArray()
            : Array.Empty<string>();
        var knowledge = BuildKnowledge(region, snapshot, focusGroup, discoveryCatalog);
        var carnivoreThreat = region.Ecosystem.FaunaPopulations.Sum(entry =>
        {
            var fauna = faunaCatalog.GetById(entry.Key);
            return fauna?.DietCategory == DietCategory.Carnivore ? entry.Value : 0;
        });

        var groupThreat = groupsHere.Length == 0
            ? 0
            : (int)Math.Round(groupsHere.Average(group => group.Pressures.ThreatPressure), MidpointRounding.AwayFromZero);

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
            : KnowledgePresentation.DescribeThreat(snapshot);

        var context = new List<string>();
        if (focusGroup is not null)
        {
            if (string.Equals(region.Id, focusGroup.CurrentRegionId, StringComparison.Ordinal))
            {
                context.Add("Current region of the polity");
            }

            if (string.Equals(region.Id, focusGroup.OriginRegionId, StringComparison.Ordinal))
            {
                context.Add("Home region");
            }

            if (region.NeighborIds.Contains(focusGroup.CurrentRegionId, StringComparer.Ordinal))
            {
                context.Add("Connected to the polity's current path");
            }
        }

        if (groupsHere.Length > 1)
        {
            context.Add("Other groups are present");
        }

        if (snapshot?.WaterKnowledge == KnowledgeLevel.Known && region.WaterAvailability == WaterAvailability.High)
        {
            context.Add("Water is reliable");
        }
        else if (snapshot?.WaterKnowledge == KnowledgeLevel.Known && region.WaterAvailability == WaterAvailability.Low)
        {
            context.Add("Water is scarce");
        }

        if (snapshot?.ConditionsKnowledge == KnowledgeLevel.Known && region.Fertility >= 0.70)
        {
            context.Add("Land looks productive");
        }
        else if (snapshot?.ConditionsKnowledge == KnowledgeLevel.Known && region.Fertility <= 0.35)
        {
            context.Add("Resources appear thin");
        }

        var opportunities = new List<string>();
        if (snapshot is not null && snapshot.WaterKnowledge != KnowledgeLevel.Unknown)
        {
            opportunities.Add(KnowledgePresentation.DescribeWater(snapshot));
        }

        if (snapshot?.ConditionsKnowledge == KnowledgeLevel.Known && region.Fertility >= 0.65)
        {
            opportunities.Add("Fertile ground");
        }
        else if (snapshot?.ConditionsKnowledge == KnowledgeLevel.Partial)
        {
            opportunities.Add("Land quality only partly known");
        }

        if (topFlora.Length > 0)
        {
            opportunities.Add($"Useful flora: {string.Join(", ", topFlora.Take(2))}");
        }
        else if (snapshot is not null)
        {
            opportunities.Add(KnowledgePresentation.DescribeFoodSigns(snapshot.FloraKnowledge, "Local flora cataloged", "Useful plants observed", "Rumors of forage", "Flora not yet observed"));
        }

        var risks = new List<string>();
        if (snapshot is not null)
        {
            risks.Add(KnowledgePresentation.DescribeThreat(snapshot));
        }
        else if (threatScore >= 40)
        {
            risks.Add($"{threatText} local conditions");
        }

        if (snapshot?.WaterKnowledge == KnowledgeLevel.Known && region.WaterAvailability == WaterAvailability.Low)
        {
            risks.Add("Limited water");
        }
        else if (snapshot?.WaterKnowledge == KnowledgeLevel.Partial)
        {
            risks.Add("Water availability uncertain");
        }

        if (groupsHere.Any(group => group.Pressures.OvercrowdingPressure >= 60))
        {
            risks.Add("Crowding pressure");
        }

        return new RegionSummary(
            region.Id,
            region.Name,
            snapshot is null ? "Known" : KnowledgePresentation.DescribeRegionFamiliarity(snapshot),
            snapshot?.ConditionsKnowledge == KnowledgeLevel.Known || snapshot?.IsKnownRegion == true ? region.Biome.ToString() : "Unknown",
            snapshot is null ? region.WaterAvailability.ToString() : KnowledgePresentation.DescribeWater(snapshot),
            snapshot?.ConditionsKnowledge == KnowledgeLevel.Known ? region.Fertility.ToString("0.00") : snapshot?.ConditionsKnowledge == KnowledgeLevel.Partial ? "Estimated" : "Unknown",
            exactPresenceVisible ? presencePopulation : 0,
            exactPresenceVisible ? presencePopulation.ToString("N0") : (groupsHere.Length > 0 ? "Signs" : "None"),
            BuildGroupPresence(groupsHere, exactPresenceVisible),
            topFlora.Length > 0 ? topFlora : ["None known"],
            topFauna.Length > 0 ? topFauna : ["None known"],
            knowledge,
            context.Count > 0 ? context.ToArray() : ["No special connection noted"],
            opportunities.Count > 0 ? opportunities.ToArray() : ["No standout opportunities"],
            risks.Count > 0 ? risks.ToArray() : ["No major risks detected"],
            threatText,
            threatScore);
    }

    private static IReadOnlyList<string> BuildKnowledge(Region region, RegionKnowledgeSnapshot? snapshot, PopulationGroup? focusGroup, DiscoveryCatalog discoveryCatalog)
    {
        if (focusGroup is null)
        {
            return ["General regional knowledge only"];
        }

        var knowledge = new List<string>();

        if (snapshot is not null)
        {
            knowledge.Add($"Familiarity: {KnowledgePresentation.Describe(snapshot.OverallKnowledge)}");
        }

        if (focusGroup.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalWaterSourcesDiscoveryId(region.Id)))
        {
            knowledge.Add("Water sources known");
        }

        if (focusGroup.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalFloraDiscoveryId(region.Id)))
        {
            knowledge.Add("Local flora identified");
        }

        if (focusGroup.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalFaunaDiscoveryId(region.Id)))
        {
            knowledge.Add("Local fauna identified");
        }

        if (focusGroup.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalRegionConditionsDiscoveryId(region.Id)))
        {
            knowledge.Add("Regional conditions learned");
        }

        return knowledge.Count > 0 ? knowledge : ["Not yet discovered"];
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

public sealed record RegionsScreenData(
    string CurrentDate,
    IReadOnlyList<RegionSummary> Regions,
    RegionSummary? SelectedRegion,
    int SelectedIndex);

public sealed record RegionSummary(
    string Id,
    string Name,
    string Familiarity,
    string Biome,
    string WaterAvailability,
    string Fertility,
    int PresencePopulation,
    string PresenceText,
    IReadOnlyList<string> GroupPresence,
    IReadOnlyList<string> Flora,
    IReadOnlyList<string> Fauna,
    IReadOnlyList<string> Knowledge,
    IReadOnlyList<string> Context,
    IReadOnlyList<string> Opportunities,
    IReadOnlyList<string> Risks,
    string ThreatText,
    int ThreatScore);
