using Species.Domain.Catalogs;
using Species.Domain.Models;

public static class KnownPolitiesScreenDataBuilder
{
    public static KnownPolitiesScreenData Build(
        World world,
        string focalGroupId,
        int selectedPolityIndex,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var focusGroup = PlayerFocus.Resolve(world, focalGroupId);
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var knownPolities = GetKnownPolities(world, focusGroup, regionsById, discoveryCatalog, advancementCatalog);
        var clampedIndex = knownPolities.Count == 0
            ? 0
            : Math.Clamp(selectedPolityIndex, 0, knownPolities.Count - 1);

        return new KnownPolitiesScreenData(
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            knownPolities,
            knownPolities.Count == 0 ? null : knownPolities[clampedIndex],
            clampedIndex);
    }

    private static IReadOnlyList<KnownPolitySummary> GetKnownPolities(
        World world,
        PopulationGroup? focusGroup,
        IReadOnlyDictionary<string, Region> regionsById,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var knownRegionIds = focusGroup?.KnownRegionIds ?? new HashSet<string>(StringComparer.Ordinal);

        var candidates = world.PopulationGroups
            .Where(group => focusGroup is null || !string.Equals(group.Id, focusGroup.Id, StringComparison.Ordinal))
            .Where(group =>
                focusGroup is null ||
                knownRegionIds.Contains(group.CurrentRegionId) ||
                knownRegionIds.Contains(group.OriginRegionId) ||
                (regionsById.TryGetValue(group.CurrentRegionId, out var region) &&
                 region.NeighborIds.Contains(focusGroup.CurrentRegionId, StringComparer.Ordinal)))
            .OrderBy(group => group.Name, StringComparer.Ordinal)
            .Select(group => BuildSummary(group, focusGroup, regionsById, discoveryCatalog, advancementCatalog))
            .ToArray();

        return candidates;
    }

    private static KnownPolitySummary BuildSummary(
        PopulationGroup group,
        PopulationGroup? focusGroup,
        IReadOnlyDictionary<string, Region> regionsById,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var currentRegionKnown = focusGroup is null || focusGroup.KnownRegionIds.Contains(group.CurrentRegionId);
        var originRegionKnown = focusGroup is null || focusGroup.KnownRegionIds.Contains(group.OriginRegionId);
        var currentRegionName = currentRegionKnown && regionsById.TryGetValue(group.CurrentRegionId, out var currentRegion)
            ? currentRegion.Name
            : "Not yet known";
        var coreRegionName = originRegionKnown && regionsById.TryGetValue(group.OriginRegionId, out var originRegion)
            ? originRegion.Name
            : currentRegionName;

        var isNearby = focusGroup is not null &&
            (string.Equals(group.CurrentRegionId, focusGroup.CurrentRegionId, StringComparison.Ordinal) ||
             (regionsById.TryGetValue(group.CurrentRegionId, out var current) &&
              current.NeighborIds.Contains(focusGroup.CurrentRegionId, StringComparer.Ordinal)));

        var relationship = focusGroup is null
            ? "Unknown"
            : ResolveRelationship(group, focusGroup, isNearby);

        var visiblePressureSummary = isNearby
            ? BuildPressureSummary(group)
            : "Pressures not clearly visible";

        var traits = isNearby
            ? BuildTraits(group, discoveryCatalog, advancementCatalog)
            : ["No notable traits observed"];

        var risks = isNearby
            ? BuildRisks(group)
            : ["No clear risks observed"];

        var notes = BuildNotes(group, focusGroup, currentRegionName, isNearby);

        return new KnownPolitySummary(
            group.Id,
            group.Name,
            PolityPresentation.DescribeGovernmentForm(group.GovernmentForm),
            coreRegionName,
            currentRegionName,
            KnowledgePresentation.ApproximatePopulation(group.Population, exactAllowed: isNearby),
            relationship,
            isNearby ? "Nearby" : "Distant",
            visiblePressureSummary,
            traits,
            risks,
            notes,
            BuildKnownLaws(group));
    }

    private static string ResolveRelationship(PopulationGroup group, PopulationGroup focusGroup, bool isNearby)
    {
        if (string.Equals(group.CurrentRegionId, focusGroup.CurrentRegionId, StringComparison.Ordinal))
        {
            return "Competing nearby";
        }

        if (string.Equals(group.OriginRegionId, focusGroup.OriginRegionId, StringComparison.Ordinal))
        {
            return "Shared homeland";
        }

        if (isNearby)
        {
            return "Cautious";
        }

        return "Known contact";
    }

    private static string BuildPressureSummary(PopulationGroup group)
    {
        var notable = new List<string>();

        if (group.Pressures.FoodPressure >= 60)
        {
            notable.Add("food stress");
        }

        if (group.Pressures.WaterPressure >= 60)
        {
            notable.Add("water strain");
        }

        if (group.Pressures.OvercrowdingPressure >= 60)
        {
            notable.Add("crowding");
        }

        if (group.Pressures.MigrationPressure >= 60)
        {
            notable.Add("migration pressure");
        }

        if (group.Pressures.ThreatPressure >= 60)
        {
            notable.Add("danger");
        }

        return notable.Count > 0
            ? $"Visible signs of {string.Join(", ", notable)}"
            : "No obvious distress is visible";
    }

    private static IReadOnlyList<string> BuildTraits(
        PopulationGroup group,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        _ = discoveryCatalog;
        _ = advancementCatalog;
        var traits = new List<string>();

        if (group.SubsistenceMode == Species.Domain.Enums.SubsistenceMode.Gatherer)
        {
            traits.Add("Often seen foraging");
        }

        if (group.SubsistenceMode == Species.Domain.Enums.SubsistenceMode.Hunter)
        {
            traits.Add("Often seen hunting");
        }

        if (group.Pressures.MigrationPressure >= 60)
        {
            traits.Add("Frequently on the move");
        }

        if (group.StoredFood > group.Population)
        {
            traits.Add("Carries visible provisions");
        }

        if (group.Pressures.ThreatPressure < 40)
        {
            traits.Add("Moves with some confidence");
        }

        return traits.Count > 0 ? traits.Take(3).ToArray() : ["No notable traits observed"];
    }

    private static IReadOnlyList<string> BuildRisks(PopulationGroup group)
    {
        var risks = new List<string>();

        if (group.Pressures.OvercrowdingPressure >= 60)
        {
            risks.Add("Crowded conditions");
        }

        if (group.Pressures.WaterPressure >= 60)
        {
            risks.Add("Limited water sources");
        }

        if (group.Pressures.FoodPressure >= 60)
        {
            risks.Add("Food pressure is visible");
        }

        if (group.Pressures.ThreatPressure >= 60)
        {
            risks.Add("Threat pressure is high");
        }

        return risks.Count > 0 ? risks.Take(3).ToArray() : ["No clear risks observed"];
    }

    private static IReadOnlyList<string> BuildNotes(
        PopulationGroup group,
        PopulationGroup? focusGroup,
        string currentRegionName,
        bool isNearby)
    {
        var notes = new List<string> { $"Current region: {currentRegionName}" };

        if (isNearby)
        {
            notes.Add("This polity is operating near the player polity");
        }

        if (focusGroup is not null && string.Equals(group.CurrentRegionId, focusGroup.CurrentRegionId, StringComparison.Ordinal))
        {
            notes.Add("Competes for the same local space");
        }

        if (focusGroup is not null && string.Equals(group.OriginRegionId, focusGroup.OriginRegionId, StringComparison.Ordinal))
        {
            notes.Add("Shares the same homeland");
        }

        return notes;
    }

    private static IReadOnlyList<string> BuildKnownLaws(PopulationGroup group)
    {
        var activeEnacted = group.EnactedLaws
            .Where(law => law.IsActive)
            .OrderByDescending(law => law.EnactedOnYear)
            .ThenByDescending(law => law.EnactedOnMonth)
            .Select(law => law.Title)
            .ToArray();

        return activeEnacted.Length > 0
            ? activeEnacted.Take(2).ToArray()
            : ["No known laws"];
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

public sealed record KnownPolitiesScreenData(
    string CurrentDate,
    IReadOnlyList<KnownPolitySummary> Polities,
    KnownPolitySummary? SelectedPolity,
    int SelectedIndex);

public sealed record KnownPolitySummary(
    string Id,
    string Name,
    string GovernmentForm,
    string CoreRegion,
    string CurrentRegion,
    string Population,
    string Relationship,
    string Proximity,
    string PressureSummary,
    IReadOnlyList<string> Traits,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> KnownLaws);
