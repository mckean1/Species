using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class DiscoverySystem
{
    public DiscoveryResult Run(
        World world,
        DiscoveryCatalog discoveryCatalog,
        IReadOnlyList<GroupSurvivalChange> survivalChanges,
        IReadOnlyList<MigrationChange> migrationChanges)
    {
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var survivalByGroupId = survivalChanges.ToDictionary(change => change.GroupId, StringComparer.Ordinal);
        var migrationByGroupId = migrationChanges.ToDictionary(change => change.GroupId, StringComparer.Ordinal);
        var updatedGroups = new List<PopulationGroup>(world.PopulationGroups.Count);
        var changes = new List<DiscoveryChange>(world.PopulationGroups.Count);

        foreach (var group in world.PopulationGroups.OrderBy(group => group.Id, StringComparer.Ordinal))
        {
            var updatedGroup = CloneGroup(group);
            var evidence = updatedGroup.DiscoveryEvidence;
            survivalByGroupId.TryGetValue(group.Id, out var survivalChange);
            migrationByGroupId.TryGetValue(group.Id, out var migrationChange);

            var livedRegionId = survivalChange?.CurrentRegionId ?? group.CurrentRegionId;
            if (regionsById.TryGetValue(livedRegionId, out var livedRegion))
            {
                Increment(evidence.MonthsSpentByRegionId, livedRegionId);
                Increment(evidence.WaterExposureMonthsByRegionId, livedRegionId);

                if ((survivalChange?.PrimaryAction == "Gather" && survivalChange.PrimaryFoodGained > 0) ||
                    (survivalChange?.FallbackAction == "Gather" && survivalChange.FallbackFoodGained > 0))
                {
                    Increment(evidence.SuccessfulGatheringMonthsByRegionId, livedRegionId);
                }

                if ((survivalChange?.PrimaryAction == "Hunt" && survivalChange.PrimaryFoodGained > 0) ||
                    (survivalChange?.FallbackAction == "Hunt" && survivalChange.FallbackFoodGained > 0))
                {
                    Increment(evidence.SuccessfulHuntingMonthsByRegionId, livedRegionId);
                }
            }

            if (migrationChange is { Moved: true })
            {
                var routeKey = discoveryCatalog.GetRouteKey(migrationChange.CurrentRegionId, migrationChange.NewRegionId);
                Increment(evidence.RouteTraversalCountsByRouteId, routeKey);
            }

            var unlockedThisMonth = new List<DiscoveryDefinition>();
            var checks = new List<string>();

            EvaluateRegionDiscovery(
                updatedGroup,
                discoveryCatalog,
                discoveryCatalog.GetLocalFloraDiscoveryId(livedRegionId),
                evidence.SuccessfulGatheringMonthsByRegionId.GetValueOrDefault(livedRegionId),
                DiscoveryConstants.LocalFloraGatheringMonthsRequired,
                unlockedThisMonth,
                checks);

            EvaluateRegionDiscovery(
                updatedGroup,
                discoveryCatalog,
                discoveryCatalog.GetLocalFaunaDiscoveryId(livedRegionId),
                evidence.SuccessfulHuntingMonthsByRegionId.GetValueOrDefault(livedRegionId),
                DiscoveryConstants.LocalFaunaHuntingMonthsRequired,
                unlockedThisMonth,
                checks);

            EvaluateRegionDiscovery(
                updatedGroup,
                discoveryCatalog,
                discoveryCatalog.GetLocalRegionConditionsDiscoveryId(livedRegionId),
                evidence.MonthsSpentByRegionId.GetValueOrDefault(livedRegionId),
                DiscoveryConstants.LocalRegionResidenceMonthsRequired,
                unlockedThisMonth,
                checks);

            EvaluateRegionDiscovery(
                updatedGroup,
                discoveryCatalog,
                discoveryCatalog.GetLocalWaterSourcesDiscoveryId(livedRegionId),
                evidence.WaterExposureMonthsByRegionId.GetValueOrDefault(livedRegionId),
                DiscoveryConstants.LocalWaterExposureMonthsRequired,
                unlockedThisMonth,
                checks);

            if (migrationChange is { Moved: true })
            {
                var routeDiscoveryId = discoveryCatalog.GetRouteDiscoveryId(migrationChange.CurrentRegionId, migrationChange.NewRegionId);
                var routeKey = discoveryCatalog.GetRouteKey(migrationChange.CurrentRegionId, migrationChange.NewRegionId);
                EvaluateRegionDiscovery(
                    updatedGroup,
                    discoveryCatalog,
                    routeDiscoveryId,
                    evidence.RouteTraversalCountsByRouteId.GetValueOrDefault(routeKey),
                    DiscoveryConstants.RouteTraversalCountRequired,
                    unlockedThisMonth,
                    checks);
            }

            updatedGroups.Add(updatedGroup);
            changes.Add(new DiscoveryChange
            {
                GroupId = updatedGroup.Id,
                GroupName = updatedGroup.Name,
                KnownDiscoveriesSummary = BuildKnownDiscoveriesSummary(updatedGroup, discoveryCatalog),
                EvidenceSummary = BuildEvidenceSummary(updatedGroup.DiscoveryEvidence),
                CheckSummary = checks.Count == 0 ? "none" : string.Join(" | ", checks),
                UnlockedDiscoveriesSummary = unlockedThisMonth.Count == 0
                    ? "none"
                    : string.Join(", ", unlockedThisMonth.Select(definition => definition.Name)),
                DecisionEffectSummary = unlockedThisMonth.Count == 0
                    ? "No new discovery effect this month."
                    : string.Join(" ", unlockedThisMonth.Select(definition => definition.DecisionEffectSummary))
            });
        }

        return new DiscoveryResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle),
            changes);
    }

    private static void EvaluateRegionDiscovery(
        PopulationGroup group,
        DiscoveryCatalog discoveryCatalog,
        string discoveryId,
        int evidenceCount,
        int requiredCount,
        ICollection<DiscoveryDefinition> unlockedThisMonth,
        ICollection<string> checks)
    {
        var definition = discoveryCatalog.GetById(discoveryId);
        if (definition is null)
        {
            return;
        }

        if (group.KnownDiscoveryIds.Contains(discoveryId))
        {
            checks.Add($"{definition.Name}: already known.");
            return;
        }

        if (evidenceCount >= requiredCount)
        {
            group.KnownDiscoveryIds.Add(discoveryId);
            unlockedThisMonth.Add(definition);
            checks.Add($"{definition.Name}: unlocked at {evidenceCount}/{requiredCount}.");
            return;
        }

        checks.Add($"{definition.Name}: {evidenceCount}/{requiredCount}.");
    }

    private static void Increment(IDictionary<string, int> counters, string key)
    {
        counters[key] = counters.TryGetValue(key, out var currentValue)
            ? currentValue + 1
            : 1;
    }

    private static string BuildKnownDiscoveriesSummary(PopulationGroup group, DiscoveryCatalog discoveryCatalog)
    {
        if (group.KnownDiscoveryIds.Count == 0)
        {
            return "none";
        }

        return string.Join(
            ", ",
            group.KnownDiscoveryIds
                .OrderBy(id => id, StringComparer.Ordinal)
                .Select(id => discoveryCatalog.GetById(id)?.Name ?? id));
    }

    private static string BuildEvidenceSummary(DiscoveryEvidenceState evidence)
    {
        return $"Gather=[{SummarizeCounters(evidence.SuccessfulGatheringMonthsByRegionId)}] | Hunt=[{SummarizeCounters(evidence.SuccessfulHuntingMonthsByRegionId)}] | Residence=[{SummarizeCounters(evidence.MonthsSpentByRegionId)}] | Water=[{SummarizeCounters(evidence.WaterExposureMonthsByRegionId)}] | Routes=[{SummarizeCounters(evidence.RouteTraversalCountsByRouteId)}]";
    }

    private static string SummarizeCounters(IReadOnlyDictionary<string, int> counters)
    {
        if (counters.Count == 0)
        {
            return "none";
        }

        return string.Join(", ", counters.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => $"{entry.Key}:{entry.Value}"));
    }

    private static PopulationGroup CloneGroup(PopulationGroup group)
    {
        return new PopulationGroup
        {
            Id = group.Id,
            Name = group.Name,
            SpeciesId = group.SpeciesId,
            CurrentRegionId = group.CurrentRegionId,
            OriginRegionId = group.OriginRegionId,
            Population = group.Population,
            StoredFood = group.StoredFood,
            SubsistenceMode = group.SubsistenceMode,
            Pressures = new PressureState
            {
                FoodPressure = group.Pressures.FoodPressure,
                WaterPressure = group.Pressures.WaterPressure,
                ThreatPressure = group.Pressures.ThreatPressure,
                OvercrowdingPressure = group.Pressures.OvercrowdingPressure,
                MigrationPressure = group.Pressures.MigrationPressure
            },
            LastRegionId = group.LastRegionId,
            MonthsSinceLastMove = group.MonthsSinceLastMove,
            KnownRegionIds = new HashSet<string>(group.KnownRegionIds, StringComparer.Ordinal),
            KnownDiscoveryIds = new HashSet<string>(group.KnownDiscoveryIds, StringComparer.Ordinal),
            DiscoveryEvidence = group.DiscoveryEvidence.Clone(),
            LearnedAdvancementIds = new HashSet<string>(group.LearnedAdvancementIds, StringComparer.Ordinal),
            AdvancementEvidence = group.AdvancementEvidence.Clone()
        };
    }
}
