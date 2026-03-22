using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Domain.Simulation;
using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;

namespace Species.Client.ViewModelFactories;

public static class KnownPolitiesViewModelFactory
{
    private static readonly PolityConditionEvaluator PolityConditionEvaluator = new();

    public static KnownPolitiesViewModel Build(
        World world,
        string focalPolityId,
        int selectedPolityIndex,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var focusPolity = PlayerFocus.Resolve(world, focalPolityId);
        var focusContext = PlayerFocus.ResolveContext(world, focalPolityId);
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var knownPolities = GetKnownPolities(world, focusPolity, focusContext, regionsById, discoveryCatalog, advancementCatalog);
        var clampedIndex = knownPolities.Count == 0
            ? 0
            : Math.Clamp(selectedPolityIndex, 0, knownPolities.Count - 1);

        return new KnownPolitiesViewModel(
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            knownPolities,
            knownPolities.Count == 0 ? null : knownPolities[clampedIndex],
            clampedIndex);
    }

    private static IReadOnlyList<KnownPolitySummary> GetKnownPolities(
        World world,
        Polity? focusPolity,
        PolityContext? focusContext,
        IReadOnlyDictionary<string, Region> regionsById,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var knownRegionIds = focusContext?.KnownRegionIds ?? new HashSet<string>(StringComparer.Ordinal);

        return world.Polities
            .Where(polity => focusPolity is null || !string.Equals(polity.Id, focusPolity.Id, StringComparison.Ordinal))
            .Select(polity => new { Polity = polity, Context = PolityData.BuildContext(world, polity) })
            .Where(item => item.Context?.LeadGroup is not null)
            .Where(item =>
                focusContext is null ||
                knownRegionIds.Contains(item.Context!.CurrentRegionId) ||
                knownRegionIds.Contains(item.Context!.OriginRegionId) ||
                (regionsById.TryGetValue(item.Context!.CurrentRegionId, out var region) &&
                 region.NeighborIds.Contains(focusContext.CurrentRegionId, StringComparer.Ordinal)))
            .OrderBy(item => item.Polity.Name, StringComparer.Ordinal)
            .Select(item => BuildSummary(world, item.Polity, item.Context!, focusPolity, focusContext, regionsById, discoveryCatalog, advancementCatalog))
            .ToArray();
    }

    private static KnownPolitySummary BuildSummary(
        World world,
        Polity polity,
        PolityContext context,
        Polity? focusPolity,
        PolityContext? focusContext,
        IReadOnlyDictionary<string, Region> regionsById,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        _ = discoveryCatalog;
        _ = advancementCatalog;
        var snapshot = PolityConditionEvaluator.Evaluate(world, polity);

        var currentRegionKnown = focusContext is null || focusContext.KnownRegionIds.Contains(context.CurrentRegionId);
        var coreRegionKnown = focusContext is null || focusContext.KnownRegionIds.Contains(context.CoreRegionId);
        var currentRegionName = currentRegionKnown && regionsById.TryGetValue(context.CurrentRegionId, out var currentRegion)
            ? currentRegion.Name
            : "Not yet known";
        var coreRegionName = coreRegionKnown && regionsById.TryGetValue(context.CoreRegionId, out var originRegion)
            ? originRegion.Name
            : currentRegionName;

        var isNearby = focusContext is not null &&
            (string.Equals(context.CurrentRegionId, focusContext.CurrentRegionId, StringComparison.Ordinal) ||
             (regionsById.TryGetValue(context.CurrentRegionId, out var current) &&
              current.NeighborIds.Contains(focusContext.CurrentRegionId, StringComparer.Ordinal)));
        var relation = focusPolity?.InterPolityRelations
            .FirstOrDefault(item => string.Equals(item.OtherPolityId, polity.Id, StringComparison.Ordinal));

        return new KnownPolitySummary(
            polity.Id,
            polity.Name,
            $"{PolityPresentation.DescribeGovernmentForm(snapshot.GovernmentForm)} / {PolityPresentation.DescribePoliticalScaleForm(snapshot.ScaleForm)}",
            coreRegionName,
            currentRegionName,
            KnowledgePresentation.ApproximatePopulation(context.TotalPopulation, exactAllowed: isNearby),
            ResolveRelationship(context, focusContext, isNearby, relation),
            isNearby ? "Nearby" : "Distant",
            snapshot.Summary,
            BuildTraits(context, snapshot),
            BuildRisks(snapshot),
            BuildNotes(context, snapshot, focusPolity, focusContext, currentRegionName, coreRegionName, isNearby, relation),
            BuildKnownLaws(polity));
    }

    private static string ResolveRelationship(PolityContext context, PolityContext? focusContext, bool isNearby, InterPolityRelation? relation)
    {
        if (relation is not null)
        {
            return relation.Stance switch
            {
                Species.Domain.Enums.InterPolityStance.Cooperative => "Cooperative",
                Species.Domain.Enums.InterPolityStance.Wary => "Wary",
                Species.Domain.Enums.InterPolityStance.Rival => "Rival",
                Species.Domain.Enums.InterPolityStance.Hostile => "Hostile",
                Species.Domain.Enums.InterPolityStance.RaidingConflict => "Raiding conflict",
                Species.Domain.Enums.InterPolityStance.OpenConflict => "Open conflict",
                Species.Domain.Enums.InterPolityStance.UneasyPeace => "Uneasy peace",
                Species.Domain.Enums.InterPolityStance.Neutral => "Neutral contact",
                _ => "Unknown"
            };
        }

        if (focusContext is null)
        {
            return "Unknown";
        }

        if (string.Equals(context.CurrentRegionId, focusContext.CurrentRegionId, StringComparison.Ordinal))
        {
            return "Competing nearby";
        }

        if (string.Equals(context.OriginRegionId, focusContext.OriginRegionId, StringComparison.Ordinal))
        {
            return "Shared homeland";
        }

        return isNearby ? "Cautious" : "Known contact";
    }

    private static string BuildPressureSummary(PolityContext context, InterPolityRelation? relation)
    {
        var notable = new List<string>();

        if (context.Pressures.Food.DisplayValue >= 60)
        {
            notable.Add($"food stress ({context.Pressures.Food.SeverityLabel.ToLowerInvariant()})");
        }

        if (context.Pressures.Water.DisplayValue >= 60)
        {
            notable.Add($"water strain ({context.Pressures.Water.SeverityLabel.ToLowerInvariant()})");
        }

        if (context.Pressures.Overcrowding.DisplayValue >= 60)
        {
            notable.Add("crowding");
        }

        if (context.Pressures.Migration.DisplayValue >= 60)
        {
            notable.Add("migration pressure");
        }

        if (context.Pressures.Threat.DisplayValue >= 60)
        {
            notable.Add("danger");
        }

        if (relation?.RaidPressure >= 35)
        {
            notable.Add("raid damage");
        }

        if (relation?.FrontierFriction >= 35)
        {
            notable.Add("frontier friction");
        }

        return notable.Count > 0
            ? $"Visible signs of {string.Join(", ", notable)}"
            : "No obvious distress is visible";
    }

    private static IReadOnlyList<string> BuildTraits(PolityContext context, PolityConditionSnapshot snapshot)
    {
        var traits = new List<string>();
        var leadGroup = context.LeadGroup;
        if (leadGroup is null)
        {
            return ["No notable traits observed"];
        }

        if (leadGroup.SubsistenceMode == Species.Domain.Enums.SubsistenceMode.Gatherer)
        {
            traits.Add("Often seen foraging");
        }

        if (leadGroup.SubsistenceMode == Species.Domain.Enums.SubsistenceMode.Hunter)
        {
            traits.Add("Often seen hunting");
        }

        if (snapshot.MaterialSurvival.MigrationCondition >= PolityConditionSeverity.Critical)
        {
            traits.Add("Frequently on the move");
        }
        else if (snapshot.AnchoringKind == Species.Domain.Enums.PolityAnchoringKind.Anchored)
        {
            traits.Add("Clearly anchored to a core site");
        }

        if (context.TotalStoredFood > context.TotalPopulation)
        {
            traits.Add("Carries visible provisions");
        }

        if (snapshot.MaterialSurvival.ThreatCondition == PolityConditionSeverity.Stable)
        {
            traits.Add("Moves with some confidence");
        }

        return traits.Count > 0 ? traits.Take(3).ToArray() : ["No notable traits observed"];
    }

    private static IReadOnlyList<string> BuildRisks(PolityConditionSnapshot snapshot)
    {
        var risks = new List<string>();

        if (snapshot.MaterialSurvival.CrowdingCondition >= PolityConditionSeverity.Strained)
        {
            risks.Add("Crowded conditions");
        }

        if (snapshot.MaterialSurvival.WaterCondition >= PolityConditionSeverity.Strained)
        {
            risks.Add("Limited water sources");
        }

        if (snapshot.MaterialSurvival.FoodCondition >= PolityConditionSeverity.Strained)
        {
            risks.Add("Food pressure is visible");
        }

        if (snapshot.MaterialSurvival.ThreatCondition >= PolityConditionSeverity.Strained)
        {
            risks.Add("Threat pressure is high");
        }

        return risks.Count > 0 ? risks.Take(3).ToArray() : ["No clear risks observed"];
    }

    private static IReadOnlyList<string> BuildNotes(
        PolityContext context,
        PolityConditionSnapshot snapshot,
        Polity? focusPolity,
        PolityContext? focusContext,
        string currentRegionName,
        string coreRegionName,
        bool isNearby,
        InterPolityRelation? relation)
    {
        var notes = new List<string> { $"Current region: {currentRegionName}" };

        if (!string.Equals(coreRegionName, currentRegionName, StringComparison.Ordinal))
        {
            notes.Add($"Core region: {coreRegionName}");
        }

        notes.Add($"Anchoring: {PolityPresentation.DescribeAnchoringKind(snapshot.AnchoringKind)}");

        if (snapshot.SpatialStability.HasValidSeat && context.PrimarySettlement is not null)
        {
            notes.Add($"Primary site: {context.PrimarySettlement.Name}");
        }
        else
        {
            notes.Add("Primary site: none");
        }

        notes.Add($"State form: {PolityPresentation.DescribePoliticalScaleForm(snapshot.ScaleForm)}");
        notes.Add(snapshot.Summary);

        if (isNearby)
        {
            notes.Add("This polity is operating near the player polity");
        }

        if (focusContext is not null && string.Equals(context.CurrentRegionId, focusContext.CurrentRegionId, StringComparison.Ordinal))
        {
            notes.Add("Competes for the same local space");
        }

        if (focusContext is not null && string.Equals(context.HomeRegionId, focusContext.HomeRegionId, StringComparison.Ordinal))
        {
            notes.Add("Shares the same homeland");
        }

        if (relation is not null && !string.IsNullOrWhiteSpace(relation.RecentSummary))
        {
            notes.Add(relation.RecentSummary);
        }

        if (relation?.RaidsSuffered > 0 || relation?.RaidsInflicted > 0)
        {
            notes.Add($"Recent clashes: suffered {relation.RaidsSuffered}, inflicted {relation.RaidsInflicted}");
        }

        if (focusPolity is not null && string.Equals(context.Polity.Id, focusPolity.Id, StringComparison.Ordinal))
        {
            notes.Add("This is the player polity");
        }

        return notes;
    }

    private static IReadOnlyList<string> BuildKnownLaws(Polity polity)
    {
        var activeEnacted = polity.EnactedLaws
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
