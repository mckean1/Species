using Species.Domain.Catalogs;
using Species.Domain.Enums;
using Species.Domain.Models;

public static class RegionsScreenDataBuilder
{
    public static RegionsScreenData Build(
        World world,
        int selectedRegionIndex,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog)
    {
        var focusGroup = SelectFocusGroup(world);
        var regionCandidates = GetKnownRegions(world, focusGroup);
        var selectedIndex = regionCandidates.Count == 0
            ? 0
            : Math.Clamp(selectedRegionIndex, 0, regionCandidates.Count - 1);

        var summaries = regionCandidates
            .Select(region => BuildSummary(region, world, focusGroup, floraCatalog, faunaCatalog, discoveryCatalog))
            .ToArray();

        return new RegionsScreenData(
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            summaries,
            summaries.Length == 0 ? null : summaries[selectedIndex],
            selectedIndex);
    }

    private static PopulationGroup? SelectFocusGroup(World world)
    {
        var latestEntry = world.Chronicle.GetVisibleFeedEntries().FirstOrDefault();
        if (latestEntry is not null)
        {
            var matchingGroup = world.PopulationGroups.FirstOrDefault(group =>
                string.Equals(group.Id, latestEntry.GroupId, StringComparison.Ordinal));

            if (matchingGroup is not null)
            {
                return matchingGroup;
            }
        }

        return world.PopulationGroups
            .OrderByDescending(group => group.Population)
            .ThenBy(group => group.Name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static IReadOnlyList<Region> GetKnownRegions(World world, PopulationGroup? focusGroup)
    {
        if (focusGroup is null || focusGroup.KnownRegionIds.Count == 0)
        {
            return world.Regions
                .OrderBy(region => region.Name, StringComparer.Ordinal)
                .ToArray();
        }

        return world.Regions
            .Where(region => focusGroup.KnownRegionIds.Contains(region.Id))
            .OrderBy(region => region.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static RegionSummary BuildSummary(
        Region region,
        World world,
        PopulationGroup? focusGroup,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog)
    {
        var groupsHere = world.PopulationGroups
            .Where(group => string.Equals(group.CurrentRegionId, region.Id, StringComparison.Ordinal))
            .OrderByDescending(group => group.Population)
            .ThenBy(group => group.Name, StringComparer.Ordinal)
            .ToArray();

        var presencePopulation = groupsHere.Sum(group => group.Population);
        var topFlora = ResolveTopPopulationNames(region.Ecosystem.FloraPopulations, floraCatalog.GetById).Take(3).ToArray();
        var topFauna = ResolveTopPopulationNames(region.Ecosystem.FaunaPopulations, faunaCatalog.GetById).Take(3).ToArray();
        var knowledge = BuildKnowledge(region, focusGroup, discoveryCatalog);
        var carnivoreThreat = region.Ecosystem.FaunaPopulations.Sum(entry =>
        {
            var fauna = faunaCatalog.GetById(entry.Key);
            return fauna?.DietCategory == DietCategory.Carnivore ? entry.Value : 0;
        });

        var groupThreat = groupsHere.Length == 0
            ? 0
            : (int)Math.Round(groupsHere.Average(group => group.Pressures.ThreatPressure), MidpointRounding.AwayFromZero);

        var threatScore = Math.Clamp(Math.Max(groupThreat, carnivoreThreat / 2), 0, 100);
        var threatText = threatScore switch
        {
            >= 70 => "Dangerous",
            >= 40 => "Watchful",
            _ => "Calm"
        };

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

        if (region.WaterAvailability == WaterAvailability.High)
        {
            context.Add("Water is reliable");
        }
        else if (region.WaterAvailability == WaterAvailability.Low)
        {
            context.Add("Water is scarce");
        }

        if (region.Fertility >= 0.70)
        {
            context.Add("Land looks productive");
        }
        else if (region.Fertility <= 0.35)
        {
            context.Add("Resources appear thin");
        }

        var opportunities = new List<string>();
        if (region.WaterAvailability == WaterAvailability.High)
        {
            opportunities.Add("Strong water access");
        }

        if (region.Fertility >= 0.65)
        {
            opportunities.Add("Fertile ground");
        }

        if (topFlora.Length > 0)
        {
            opportunities.Add($"Useful flora: {string.Join(", ", topFlora.Take(2))}");
        }

        var risks = new List<string>();
        if (threatScore >= 40)
        {
            risks.Add($"{threatText} local conditions");
        }

        if (region.WaterAvailability == WaterAvailability.Low)
        {
            risks.Add("Limited water");
        }

        if (groupsHere.Any(group => group.Pressures.OvercrowdingPressure >= 60))
        {
            risks.Add("Crowding pressure");
        }

        return new RegionSummary(
            region.Id,
            region.Name,
            region.Biome.ToString(),
            region.WaterAvailability.ToString(),
            region.Fertility.ToString("0.00"),
            presencePopulation,
            groupsHere.Select(group => $"{group.Name} ({group.Population:N0})").ToArray(),
            topFlora.Length > 0 ? topFlora : ["None known"],
            topFauna.Length > 0 ? topFauna : ["None known"],
            knowledge,
            context.Count > 0 ? context.ToArray() : ["No special connection noted"],
            opportunities.Count > 0 ? opportunities.ToArray() : ["No standout opportunities"],
            risks.Count > 0 ? risks.ToArray() : ["No major risks detected"],
            threatText,
            threatScore);
    }

    private static IReadOnlyList<string> BuildKnowledge(Region region, PopulationGroup? focusGroup, DiscoveryCatalog discoveryCatalog)
    {
        if (focusGroup is null)
        {
            return ["General regional knowledge only"];
        }

        var knowledge = new List<string>();

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
    string Biome,
    string WaterAvailability,
    string Fertility,
    int PresencePopulation,
    IReadOnlyList<string> GroupPresence,
    IReadOnlyList<string> Flora,
    IReadOnlyList<string> Fauna,
    IReadOnlyList<string> Knowledge,
    IReadOnlyList<string> Context,
    IReadOnlyList<string> Opportunities,
    IReadOnlyList<string> Risks,
    string ThreatText,
    int ThreatScore);
