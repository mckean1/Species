using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
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
        var politiesById = world.Polities.ToDictionary(polity => polity.Id, StringComparer.Ordinal);
        var survivalByGroupId = survivalChanges.ToDictionary(change => change.GroupId, StringComparer.Ordinal);
        var migrationByGroupId = migrationChanges.ToDictionary(change => change.GroupId, StringComparer.Ordinal);
        var knownDiscoveriesByPolityId = world.PopulationGroups
            .GroupBy(group => group.PolityId, StringComparer.Ordinal)
            .ToDictionary(
                grouping => grouping.Key,
                grouping => grouping.SelectMany(group => group.KnownDiscoveryIds).ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);
        var contactMap = BuildPolityContactMap(world, regionsById);
        var updatedGroups = new List<PopulationGroup>(world.PopulationGroups.Count);
        var changes = new List<DiscoveryChange>(world.PopulationGroups.Count);

        foreach (var group in world.PopulationGroups.OrderBy(group => group.Id, StringComparer.Ordinal))
        {
            var updatedGroup = CloneGroup(group);
            var evidence = updatedGroup.DiscoveryEvidence;
            survivalByGroupId.TryGetValue(group.Id, out var survivalChange);
            migrationByGroupId.TryGetValue(group.Id, out var migrationChange);
            politiesById.TryGetValue(group.PolityId, out var polity);

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

                if (group.Pressures.Food.EffectiveValue >= 55 || survivalChange?.Shortage > 0)
                {
                    evidence.RecurringFoodPressureMonths++;
                }

                if (group.Pressures.Threat.EffectiveValue >= 55)
                {
                    evidence.RecurringThreatPressureMonths++;
                }

                if (polity is not null && polity.MaterialShortageMonths > 0)
                {
                    evidence.RecurringMaterialShortageMonths++;
                }

                if (polity is not null &&
                    (polity.AnchoringKind is PolityAnchoringKind.SemiRooted or PolityAnchoringKind.Anchored ||
                     polity.Settlements.Any(settlement => settlement.IsActive && string.Equals(settlement.RegionId, livedRegionId, StringComparison.Ordinal))))
                {
                    evidence.SettlementContinuityMonths++;
                }

                if (polity is not null &&
                    ((survivalChange?.PrimaryAction == "Hunt" && survivalChange.PrimaryFoodGained > 0) ||
                     (survivalChange?.FallbackAction == "Hunt" && survivalChange.FallbackFoodGained > 0)) &&
                    polity.RegionalPresences.Any(presence =>
                        string.Equals(presence.RegionId, livedRegionId, StringComparison.Ordinal) &&
                        presence.Kind is PolityPresenceKind.Seasonal or PolityPresenceKind.Habitation or PolityPresenceKind.Core))
                {
                    evidence.SeasonalObservationMonths++;
                }

                AccumulateMaterialExposure(evidence, livedRegion.MaterialProfile);
                if (polity is not null)
                {
                    AccumulateMaterialUse(evidence, polity, livedRegionId);
                }
            }

            if (migrationChange is { Moved: true })
            {
                var routeKey = discoveryCatalog.GetRouteKey(migrationChange.CurrentRegionId, migrationChange.NewRegionId);
                Increment(evidence.RouteTraversalCountsByRouteId, routeKey);
            }

            foreach (var contactPolityId in contactMap.GetValueOrDefault(group.PolityId) ?? [])
            {
                Increment(evidence.ContactMonthsByPolityId, contactPolityId);
            }

            if (polity is not null && knownDiscoveriesByPolityId.TryGetValue(polity.Id, out var internalDiscoveries))
            {
                foreach (var discoveryId in internalDiscoveries)
                {
                    var definition = discoveryCatalog.GetById(discoveryId);
                    if (definition is null || !definition.InternalSpreadAllowed || updatedGroup.KnownDiscoveryIds.Contains(discoveryId))
                    {
                        continue;
                    }

                    Increment(evidence.SharedExposureMonthsByDiscoveryId, discoveryId);
                }
            }

            foreach (var contactPolityId in contactMap.GetValueOrDefault(group.PolityId) ?? [])
            {
                if (!knownDiscoveriesByPolityId.TryGetValue(contactPolityId, out var contactDiscoveries))
                {
                    continue;
                }

                foreach (var discoveryId in contactDiscoveries)
                {
                    var definition = discoveryCatalog.GetById(discoveryId);
                    if (definition is null || !definition.ContactSpreadAllowed || updatedGroup.KnownDiscoveryIds.Contains(discoveryId))
                    {
                        continue;
                    }

                    Increment(evidence.SharedExposureMonthsByDiscoveryId, discoveryId);
                }
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
                checks,
                extraGate: evidence.MonthsSpentByRegionId.GetValueOrDefault(livedRegionId) > 0,
                waitingReason: "needs repeat gathering and residence");

            EvaluateRegionDiscovery(
                updatedGroup,
                discoveryCatalog,
                discoveryCatalog.GetLocalFaunaDiscoveryId(livedRegionId),
                evidence.SuccessfulHuntingMonthsByRegionId.GetValueOrDefault(livedRegionId),
                DiscoveryConstants.LocalFaunaHuntingMonthsRequired,
                unlockedThisMonth,
                checks,
                extraGate: evidence.RecurringThreatPressureMonths > 0 || evidence.SuccessfulHuntingMonthsByRegionId.GetValueOrDefault(livedRegionId) > 1,
                waitingReason: "needs repeat hunting exposure");

            EvaluateRegionDiscovery(
                updatedGroup,
                discoveryCatalog,
                discoveryCatalog.GetLocalRegionConditionsDiscoveryId(livedRegionId),
                evidence.MonthsSpentByRegionId.GetValueOrDefault(livedRegionId),
                DiscoveryConstants.LocalRegionResidenceMonthsRequired,
                unlockedThisMonth,
                checks,
                extraGate: evidence.RecurringFoodPressureMonths > 0 || evidence.SettlementContinuityMonths > 0,
                waitingReason: "needs residence under practical conditions");

            EvaluateRegionDiscovery(
                updatedGroup,
                discoveryCatalog,
                discoveryCatalog.GetLocalWaterSourcesDiscoveryId(livedRegionId),
                evidence.WaterExposureMonthsByRegionId.GetValueOrDefault(livedRegionId),
                DiscoveryConstants.LocalWaterExposureMonthsRequired,
                unlockedThisMonth,
                checks,
                extraGate: true,
                waitingReason: "needs repeat water exposure");

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
                    checks,
                    extraGate: true,
                    waitingReason: "needs repeat traversal");
            }

            EvaluateRegionDiscovery(
                updatedGroup,
                discoveryCatalog,
                DiscoveryCatalog.ClayShapingId,
                evidence.MaterialExposureMonthsByResourceId.GetValueOrDefault(MaterialResource.Clay.ToString()) +
                evidence.MaterialUseMonthsByResourceId.GetValueOrDefault(MaterialResource.Clay.ToString()),
                DiscoveryConstants.ClayShapingExposureMonthsRequired,
                unlockedThisMonth,
                checks,
                extraGate: evidence.SettlementContinuityMonths > 0,
                waitingReason: "needs clay use in a durable camp or settlement");

            EvaluateRegionDiscovery(
                updatedGroup,
                discoveryCatalog,
                DiscoveryCatalog.SeasonalTrackingId,
                evidence.SeasonalObservationMonths,
                DiscoveryConstants.SeasonalTrackingMonthsRequired,
                unlockedThisMonth,
                checks,
                extraGate: SumCounters(evidence.RouteTraversalCountsByRouteId) > 0 || CountPositiveRegions(evidence.SuccessfulHuntingMonthsByRegionId) > 1,
                waitingReason: "needs repeated return and tracking patterns");

            EvaluateRegionDiscovery(
                updatedGroup,
                discoveryCatalog,
                DiscoveryCatalog.PreservationCluesId,
                evidence.RecurringFoodPressureMonths + group.AdvancementEvidence.SurplusStoredFoodMonths,
                DiscoveryConstants.PreservationCluesMonthsRequired,
                unlockedThisMonth,
                checks,
                extraGate: group.StoredFood > 0 || evidence.MaterialUseMonthsByResourceId.GetValueOrDefault(MaterialResource.Clay.ToString()) > 0,
                waitingReason: "needs storage strain and repeated surplus/shortage");

            EvaluateRegionDiscovery(
                updatedGroup,
                discoveryCatalog,
                DiscoveryCatalog.ShelterMethodsId,
                Math.Max(
                    evidence.RecurringMaterialShortageMonths + evidence.SettlementContinuityMonths,
                    evidence.SharedExposureMonthsByDiscoveryId.GetValueOrDefault(DiscoveryCatalog.ShelterMethodsId)),
                DiscoveryConstants.ShelterMethodsMonthsRequired,
                unlockedThisMonth,
                checks,
                extraGate:
                    evidence.MaterialExposureMonthsByResourceId.GetValueOrDefault(MaterialResource.Timber.ToString()) > 0 ||
                    evidence.MaterialExposureMonthsByResourceId.GetValueOrDefault(MaterialResource.Stone.ToString()) > 0 ||
                    evidence.SharedExposureMonthsByDiscoveryId.GetValueOrDefault(DiscoveryCatalog.ShelterMethodsId) > 0,
                waitingReason: "needs material strain, settled practice, or outside contact");

            TrySpreadDiscovery(updatedGroup, discoveryCatalog, unlockedThisMonth, checks);

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
                    : string.Join(" ", unlockedThisMonth.Select(definition => definition.DecisionEffectSummary)),
                ChronicleLinesSummary = unlockedThisMonth.Count == 0
                    ? "none"
                    : string.Join(";;", unlockedThisMonth.Select(definition => BuildChronicleLine(updatedGroup.Name, definition)))
            });
        }

        return new DiscoveryResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, world.Polities, world.FocalPolityId),
            changes);
    }

    private static Dictionary<string, HashSet<string>> BuildPolityContactMap(
        World world,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        var regionIdsByPolityId = world.PopulationGroups
            .GroupBy(group => group.PolityId, StringComparer.Ordinal)
            .ToDictionary(
                grouping => grouping.Key,
                grouping => grouping.Select(group => group.CurrentRegionId).ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);
        var contactMap = world.Polities.ToDictionary(polity => polity.Id, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        var polityIds = world.Polities.Select(polity => polity.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();

        for (var index = 0; index < polityIds.Length; index++)
        {
            var leftId = polityIds[index];
            for (var otherIndex = index + 1; otherIndex < polityIds.Length; otherIndex++)
            {
                var rightId = polityIds[otherIndex];
                var leftRegions = regionIdsByPolityId.GetValueOrDefault(leftId) ?? [];
                var rightRegions = regionIdsByPolityId.GetValueOrDefault(rightId) ?? [];
                var overlaps = leftRegions.Overlaps(rightRegions);
                var adjacent = leftRegions.Any(regionId =>
                    regionsById.TryGetValue(regionId, out var region) &&
                    region.NeighborIds.Any(rightRegions.Contains));

                if (!overlaps && !adjacent)
                {
                    continue;
                }

                contactMap[leftId].Add(rightId);
                contactMap[rightId].Add(leftId);
            }
        }

        return contactMap;
    }

    private static void AccumulateMaterialExposure(DiscoveryEvidenceState evidence, RegionMaterialProfile profile)
    {
        foreach (var entry in profile.Opportunities.AsDictionary().Where(entry => entry.Value > 0))
        {
            Increment(evidence.MaterialExposureMonthsByResourceId, entry.Key.ToString());
        }
    }

    private static void AccumulateMaterialUse(DiscoveryEvidenceState evidence, Polity polity, string regionId)
    {
        var totalStores = polity.MaterialStores.Clone();
        foreach (var settlement in polity.Settlements.Where(settlement => settlement.IsActive && string.Equals(settlement.RegionId, regionId, StringComparison.Ordinal)))
        {
            totalStores.Timber += settlement.MaterialStores.Timber;
            totalStores.Stone += settlement.MaterialStores.Stone;
            totalStores.Fiber += settlement.MaterialStores.Fiber;
            totalStores.Clay += settlement.MaterialStores.Clay;
            totalStores.Hides += settlement.MaterialStores.Hides;
        }

        foreach (var entry in totalStores.AsDictionary().Where(entry => entry.Value > 0))
        {
            Increment(evidence.MaterialUseMonthsByResourceId, entry.Key.ToString());
        }
    }

    private static void TrySpreadDiscovery(
        PopulationGroup group,
        DiscoveryCatalog discoveryCatalog,
        ICollection<DiscoveryDefinition> unlockedThisMonth,
        ICollection<string> checks)
    {
        foreach (var spreadEvidence in group.DiscoveryEvidence.SharedExposureMonthsByDiscoveryId.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (group.KnownDiscoveryIds.Contains(spreadEvidence.Key))
            {
                continue;
            }

            var definition = discoveryCatalog.GetById(spreadEvidence.Key);
            if (definition is null)
            {
                continue;
            }

            var required = definition.ContactSpreadAllowed
                ? DiscoveryConstants.ContactKnowledgeSpreadMonthsRequired
                : DiscoveryConstants.InternalKnowledgeSpreadMonthsRequired;
            if (spreadEvidence.Value < required)
            {
                checks.Add($"{definition.Name}: {spreadEvidence.Value}/{required} spread exposure.");
                continue;
            }

            group.KnownDiscoveryIds.Add(spreadEvidence.Key);
            unlockedThisMonth.Add(definition);
            checks.Add($"{definition.Name}: learned through continued contact and continuity.");
        }
    }

    private static void EvaluateRegionDiscovery(
        PopulationGroup group,
        DiscoveryCatalog discoveryCatalog,
        string discoveryId,
        int evidenceCount,
        int requiredCount,
        ICollection<DiscoveryDefinition> unlockedThisMonth,
        ICollection<string> checks,
        bool extraGate,
        string waitingReason)
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

        if (!extraGate)
        {
            checks.Add($"{definition.Name}: waiting, {waitingReason}.");
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

    private static int CountPositiveRegions(IReadOnlyDictionary<string, int> counters)
    {
        return counters.Count(entry => entry.Value > 0);
    }

    private static int SumCounters(IReadOnlyDictionary<string, int> counters)
    {
        return counters.Values.Sum();
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

    private static string BuildChronicleLine(string groupName, DiscoveryDefinition definition)
    {
        return definition.Id switch
        {
            var id when id == DiscoveryCatalog.ClayShapingId => $"{groupName} learned clay shaping from repeated local use.",
            var id when id == DiscoveryCatalog.SeasonalTrackingId => $"{groupName} learned seasonal tracking from repeated return and pursuit.",
            var id when id == DiscoveryCatalog.PreservationCluesId => $"{groupName} learned preservation clues after repeated storage strain.",
            var id when id == DiscoveryCatalog.ShelterMethodsId => $"{groupName} learned shelter methods through material strain and contact.",
            _ => $"{groupName} discovered {definition.Name.ToLowerInvariant()}."
        };
    }

    private static string BuildEvidenceSummary(DiscoveryEvidenceState evidence)
    {
        return $"Gather=[{SummarizeCounters(evidence.SuccessfulGatheringMonthsByRegionId)}] | Hunt=[{SummarizeCounters(evidence.SuccessfulHuntingMonthsByRegionId)}] | Residence=[{SummarizeCounters(evidence.MonthsSpentByRegionId)}] | Water=[{SummarizeCounters(evidence.WaterExposureMonthsByRegionId)}] | Routes=[{SummarizeCounters(evidence.RouteTraversalCountsByRouteId)}] | Materials=[{SummarizeCounters(evidence.MaterialExposureMonthsByResourceId)}] | Contact=[{SummarizeCounters(evidence.ContactMonthsByPolityId)}] | Pressure=F{evidence.RecurringFoodPressureMonths}/T{evidence.RecurringThreatPressureMonths}/M{evidence.RecurringMaterialShortageMonths}";
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
            PolityId = group.PolityId,
            CurrentRegionId = group.CurrentRegionId,
            OriginRegionId = group.OriginRegionId,
            Population = group.Population,
            StoredFood = group.StoredFood,
            SubsistenceMode = group.SubsistenceMode,
            Pressures = group.Pressures.Clone(),
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
