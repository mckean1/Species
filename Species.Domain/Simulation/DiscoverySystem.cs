using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Knowledge;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class DiscoverySystem
{
    public DiscoveryResult Run(
        World world,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        IReadOnlyList<GroupSurvivalChange> survivalChanges,
        IReadOnlyList<MigrationChange> migrationChanges)
    {
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var updatedPolities = world.Polities
            .Select(polity => polity.Clone())
            .ToArray();
        var politiesById = updatedPolities.ToDictionary(polity => polity.Id, StringComparer.Ordinal);
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

        // Boundary:
        // - The main group loop below owns broader non-species discoveries such as routes, methods, and materials.
        // - Species-specific flora/fauna knowing and use progression is handled separately in UpdateSpeciesAwareness.
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
            var discoveryBudget = ProgressionPacingConstants.DiscoveryMonthlyBudget;

            EvaluateRegionDiscovery(
                updatedGroup,
                discoveryCatalog,
                discoveryCatalog.GetLocalFloraDiscoveryId(livedRegionId),
                evidence.SuccessfulGatheringMonthsByRegionId.GetValueOrDefault(livedRegionId),
                DiscoveryConstants.LocalFloraGatheringMonthsRequired,
                unlockedThisMonth,
                checks,
                ref discoveryBudget,
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
                ref discoveryBudget,
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
                ref discoveryBudget,
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
                ref discoveryBudget,
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
                    ref discoveryBudget,
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
                ref discoveryBudget,
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
                ref discoveryBudget,
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
                ref discoveryBudget,
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
                ref discoveryBudget,
                extraGate:
                    evidence.MaterialExposureMonthsByResourceId.GetValueOrDefault(MaterialResource.Timber.ToString()) > 0 ||
                    evidence.MaterialExposureMonthsByResourceId.GetValueOrDefault(MaterialResource.Stone.ToString()) > 0 ||
                    evidence.SharedExposureMonthsByDiscoveryId.GetValueOrDefault(DiscoveryCatalog.ShelterMethodsId) > 0,
                waitingReason: "needs material strain, settled practice, or outside contact");

            TrySpreadDiscovery(updatedGroup, discoveryCatalog, unlockedThisMonth, checks, ref discoveryBudget);

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

        UpdateSpeciesAwareness(updatedPolities, updatedGroups, regionsById, survivalByGroupId, floraCatalog, faunaCatalog);

        return new DiscoveryResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, updatedPolities, world.FocalPolityId),
            changes);
    }

    private static void UpdateSpeciesAwareness(
        IReadOnlyList<Polity> polities,
        IReadOnlyList<PopulationGroup> updatedGroups,
        IReadOnlyDictionary<string, Region> regionsById,
        IReadOnlyDictionary<string, GroupSurvivalChange> survivalByGroupId,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        // Species awareness is intentionally separate from broader discovery progression.
        // It owns only the Encounter -> Discovery -> Knowledge ladder for actual flora/fauna species.
        var groupsByPolityId = updatedGroups
            .GroupBy(group => group.PolityId, StringComparer.Ordinal)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray(), StringComparer.Ordinal);

        foreach (var polity in polities)
        {
            if (!groupsByPolityId.TryGetValue(polity.Id, out var memberGroups) || memberGroups.Length == 0)
            {
                continue;
            }

            var awarenessByKey = polity.SpeciesAwareness
                .ToDictionary(state => BuildSpeciesKey(state.SpeciesClass, state.SpeciesId), StringComparer.Ordinal);
            var exposureByKey = BuildSpeciesExposureMap(polity, memberGroups, regionsById, survivalByGroupId, floraCatalog, faunaCatalog);
            var allKeys = awarenessByKey.Keys
                .Union(exposureByKey.Keys, StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToArray();

            foreach (var key in allKeys)
            {
                exposureByKey.TryGetValue(key, out var exposure);
                if (!awarenessByKey.TryGetValue(key, out var state))
                {
                    if (exposure is null)
                    {
                        continue;
                    }

                    state = new PolitySpeciesAwarenessState
                    {
                        SpeciesId = exposure.SpeciesId,
                        SpeciesClass = exposure.SpeciesClass
                    };
                    awarenessByKey.Add(key, state);
                }

                ApplyMonthlyProgress(state, exposure);
            }

            polity.SpeciesAwareness.Clear();
            polity.SpeciesAwareness.AddRange(awarenessByKey.Values
                .Where(state => state.CurrentLevel != KnowledgeLevel.Unknown ||
                                state.EncounterProgress > 0.0f ||
                                state.DiscoveryProgress > 0.0f ||
                                state.KnowledgeProgress > 0.0f)
                .OrderBy(state => state.SpeciesClass)
                .ThenBy(state => state.SpeciesId, StringComparer.Ordinal));
        }
    }

    private static Dictionary<string, SpeciesAwarenessExposure> BuildSpeciesExposureMap(
        Polity polity,
        IReadOnlyList<PopulationGroup> memberGroups,
        IReadOnlyDictionary<string, Region> regionsById,
        IReadOnlyDictionary<string, GroupSurvivalChange> survivalByGroupId,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var exposureByKey = new Dictionary<string, SpeciesAwarenessExposure>(StringComparer.Ordinal);
        var totalPopulation = Math.Max(1, memberGroups.Sum(group => Math.Max(1, group.Population)));
        var gatherOrientation = ResolveSubsistenceOrientation(memberGroups, survivalByGroupId, "Gather");
        var huntOrientation = ResolveSubsistenceOrientation(memberGroups, survivalByGroupId, "Hunt");
        var regionWeights = memberGroups
            .GroupBy(group => group.CurrentRegionId, StringComparer.Ordinal)
            .ToDictionary(
                grouping => grouping.Key,
                grouping => Math.Clamp(grouping.Sum(group => Math.Max(1, group.Population)) / (float)totalPopulation * 1.10f + 0.08f, 0.08f, 1.00f),
                StringComparer.Ordinal);

        foreach (var settlement in polity.Settlements.Where(settlement => settlement.IsActive))
        {
            regionWeights[settlement.RegionId] = Math.Max(regionWeights.GetValueOrDefault(settlement.RegionId), settlement.IsPrimary ? 0.45f : 0.28f);
        }

        foreach (var regionWeight in regionWeights.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (!regionsById.TryGetValue(regionWeight.Key, out var region))
            {
                continue;
            }

            foreach (var flora in region.Ecosystem.FloraPopulations.Where(entry => entry.Value > 0))
            {
                var definition = floraCatalog.GetById(flora.Key);
                if (definition is null)
                {
                    continue;
                }

                AccumulatePresenceExposure(
                    exposureByKey,
                    SpeciesClass.Flora,
                    definition.Id,
                    flora.Value,
                    definition.Conspicuousness,
                    regionWeight.Value,
                    gatherOrientation);
            }

            foreach (var fauna in region.Ecosystem.FaunaPopulations.Where(entry => entry.Value > 0))
            {
                var definition = faunaCatalog.GetById(fauna.Key);
                if (definition is null)
                {
                    continue;
                }

                AccumulatePresenceExposure(
                    exposureByKey,
                    SpeciesClass.Fauna,
                    definition.Id,
                    fauna.Value,
                    definition.Conspicuousness,
                    regionWeight.Value,
                    huntOrientation);
            }
        }

        foreach (var group in memberGroups)
        {
            if (!survivalByGroupId.TryGetValue(group.Id, out var survivalChange))
            {
                continue;
            }

            AccumulateInteractionExposure(exposureByKey, survivalChange.PrimaryAction, survivalChange.PrimaryConsumedSourceUnits);
            AccumulateInteractionExposure(exposureByKey, survivalChange.FallbackAction, survivalChange.FallbackConsumedSourceUnits);
        }

        return exposureByKey;
    }

    private static void AccumulatePresenceExposure(
        IDictionary<string, SpeciesAwarenessExposure> exposureByKey,
        SpeciesClass speciesClass,
        string speciesId,
        int population,
        float conspicuousness,
        float regionWeight,
        float behaviorAlignment)
    {
        var key = BuildSpeciesKey(speciesClass, speciesId);
        if (!exposureByKey.TryGetValue(key, out var exposure))
        {
            exposure = new SpeciesAwarenessExposure(speciesId, speciesClass);
            exposureByKey.Add(key, exposure);
        }

        var abundanceFactor = ResolveAbundanceFactor(population);
        exposure.ContactScore += abundanceFactor * regionWeight * Math.Clamp(conspicuousness, 0.15f, 1.00f);
        exposure.OverlapScore += regionWeight;
        exposure.BehaviorAlignment += behaviorAlignment * regionWeight;
        exposure.PresenceContacts++;
    }

    private static void AccumulateInteractionExposure(
        IDictionary<string, SpeciesAwarenessExposure> exposureByKey,
        string action,
        IReadOnlyDictionary<string, int> consumedSourceUnits)
    {
        var speciesClass = action switch
        {
            "Gather" => SpeciesClass.Flora,
            "Hunt" => SpeciesClass.Fauna,
            _ => (SpeciesClass?)null
        };

        if (speciesClass is null)
        {
            return;
        }

        foreach (var consumed in consumedSourceUnits.Where(entry => entry.Value > 0))
        {
            var key = BuildSpeciesKey(speciesClass.Value, consumed.Key);
            if (!exposureByKey.TryGetValue(key, out var exposure))
            {
                exposure = new SpeciesAwarenessExposure(consumed.Key, speciesClass.Value);
                exposureByKey.Add(key, exposure);
            }

            exposure.SuccessfulInteractions++;
            exposure.SuccessfulUseIntensity += Math.Clamp(consumed.Value / 18.0f, 0.08f, 0.85f);
        }
    }

    private static float ResolveSubsistenceOrientation(
        IReadOnlyList<PopulationGroup> memberGroups,
        IReadOnlyDictionary<string, GroupSurvivalChange> survivalByGroupId,
        string action)
    {
        if (memberGroups.Count == 0)
        {
            return 0.50f;
        }

        var matches = memberGroups.Count(group =>
            survivalByGroupId.TryGetValue(group.Id, out var survivalChange) &&
            (string.Equals(survivalChange.PrimaryAction, action, StringComparison.Ordinal) ||
             string.Equals(survivalChange.FallbackAction, action, StringComparison.Ordinal)));

        return Math.Clamp(0.35f + matches / (float)memberGroups.Count * 0.65f, 0.35f, 1.00f);
    }

    private static float ResolveAbundanceFactor(int population)
    {
        if (population <= 0)
        {
            return 0.0f;
        }

        return Math.Clamp((MathF.Log10(population + 1.0f) - 0.25f) / 2.85f, 0.02f, 1.00f);
    }

    private static void ApplyMonthlyProgress(PolitySpeciesAwarenessState state, SpeciesAwarenessExposure? exposure)
    {
        var contactScore = exposure?.ContactScore ?? 0.0f;
        var overlapScore = exposure?.OverlapScore ?? 0.0f;
        var behaviorAlignment = exposure?.BehaviorAlignment ?? 0.0f;
        var presenceContacts = exposure?.PresenceContacts ?? 0;
        var successfulInteractions = exposure?.SuccessfulInteractions ?? 0;
        var successfulUseIntensity = exposure?.SuccessfulUseIntensity ?? 0.0f;

        switch (state.CurrentLevel)
        {
            case KnowledgeLevel.Unknown:
                if (contactScore < SpeciesAwarenessConstants.MinimumContactScoreForEncounter)
                {
                    state.EncounterProgress = Math.Max(0.0f, state.EncounterProgress - SpeciesAwarenessConstants.EncounterMonthlyDecay);
                    return;
                }

                state.EncounterProgress = Math.Min(
                    SpeciesAwarenessConstants.StageThreshold,
                    state.EncounterProgress + Math.Min(
                        SpeciesAwarenessConstants.EncounterMonthlyGainCap,
                        0.6f + contactScore * 5.2f + overlapScore * 1.4f + behaviorAlignment * 1.0f));
                return;

            case KnowledgeLevel.Encounter:
                if (contactScore < SpeciesAwarenessConstants.MinimumContactScoreForEncounter ||
                    presenceContacts < SpeciesAwarenessConstants.MinimumPresenceContactsForDiscovery)
                {
                    state.DiscoveryProgress = Math.Max(0.0f, state.DiscoveryProgress - SpeciesAwarenessConstants.DiscoveryMonthlyDecay);
                    return;
                }

                state.DiscoveryProgress = Math.Min(
                    SpeciesAwarenessConstants.StageThreshold,
                    state.DiscoveryProgress + Math.Min(
                        SpeciesAwarenessConstants.DiscoveryMonthlyGainCap,
                        0.5f + contactScore * 3.2f + overlapScore * 1.2f + behaviorAlignment * 1.6f + successfulInteractions * 1.4f + successfulUseIntensity * 1.2f));
                return;

            case KnowledgeLevel.Discovery:
                if ((contactScore < SpeciesAwarenessConstants.MinimumContactScoreForEncounter && successfulInteractions <= 0) ||
                    successfulInteractions < SpeciesAwarenessConstants.MinimumSuccessfulInteractionsForKnowledge)
                {
                    state.KnowledgeProgress = Math.Max(0.0f, state.KnowledgeProgress - SpeciesAwarenessConstants.KnowledgeMonthlyDecay);
                    return;
                }

                state.KnowledgeProgress = Math.Min(
                    SpeciesAwarenessConstants.StageThreshold,
                    state.KnowledgeProgress + Math.Min(
                        SpeciesAwarenessConstants.KnowledgeMonthlyGainCap,
                        0.4f + contactScore * 1.6f + behaviorAlignment * 1.4f + successfulInteractions * 2.0f + successfulUseIntensity * 2.8f));
                return;

            case KnowledgeLevel.Knowledge:
                return;
        }
    }

    private static string BuildSpeciesKey(SpeciesClass speciesClass, string speciesId)
    {
        return $"{speciesClass}:{speciesId}";
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
        ICollection<string> checks,
        ref float discoveryBudget)
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
            var progress = group.DiscoveryEvidence.DiscoveryProgressByDiscoveryId.GetValueOrDefault(spreadEvidence.Key);
            if (spreadEvidence.Value < required && progress <= 0.0f)
            {
                checks.Add($"{definition.Name}: {spreadEvidence.Value}/{required} spread exposure.");
                continue;
            }

            var gain = ResolveDiscoveryGain(spreadEvidence.Value, required, contactSpread: true);
            progress = ApplyProgress(group.DiscoveryEvidence.DiscoveryProgressByDiscoveryId, spreadEvidence.Key, progress, gain, ref discoveryBudget, ProgressionPacingConstants.DiscoveryMonthlyDecay);

            if (progress >= ProgressionPacingConstants.StageThreshold)
            {
                group.KnownDiscoveryIds.Add(spreadEvidence.Key);
                unlockedThisMonth.Add(definition);
                checks.Add($"{definition.Name}: learned through continued contact and continuity.");
                continue;
            }

            checks.Add($"{definition.Name}: spread progress {progress:0}/{ProgressionPacingConstants.StageThreshold:0} from {spreadEvidence.Value}/{required} exposure.");
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
        ref float discoveryBudget,
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
            ApplyProgress(group.DiscoveryEvidence.DiscoveryProgressByDiscoveryId, discoveryId, group.DiscoveryEvidence.DiscoveryProgressByDiscoveryId.GetValueOrDefault(discoveryId), 0.0f, ref discoveryBudget, ProgressionPacingConstants.DiscoveryMonthlyDecay);
            checks.Add($"{definition.Name}: waiting, {waitingReason}.");
            return;
        }

        var progress = group.DiscoveryEvidence.DiscoveryProgressByDiscoveryId.GetValueOrDefault(discoveryId);
        var gain = ResolveDiscoveryGain(evidenceCount, requiredCount, contactSpread: false);
        progress = ApplyProgress(group.DiscoveryEvidence.DiscoveryProgressByDiscoveryId, discoveryId, progress, gain, ref discoveryBudget, ProgressionPacingConstants.DiscoveryMonthlyDecay);

        if (progress >= ProgressionPacingConstants.StageThreshold)
        {
            group.KnownDiscoveryIds.Add(discoveryId);
            unlockedThisMonth.Add(definition);
            checks.Add($"{definition.Name}: unlocked after paced progress at {evidenceCount}/{requiredCount}.");
            return;
        }

        checks.Add($"{definition.Name}: progress {progress:0}/{ProgressionPacingConstants.StageThreshold:0} from {evidenceCount}/{requiredCount}.");
    }

    private static float ResolveDiscoveryGain(int evidenceCount, int requiredCount, bool contactSpread)
    {
        if (evidenceCount <= 0 || requiredCount <= 0)
        {
            return 0.0f;
        }

        var evidenceRatio = Math.Clamp(evidenceCount / (float)requiredCount, 0.0f, 2.0f);
        var baseGain = contactSpread ? 1.5f : 2.5f;
        return Math.Min(
            ProgressionPacingConstants.DiscoveryMonthlyGainCap,
            baseGain + evidenceRatio * (contactSpread ? 7.0f : 9.0f));
    }

    private static float ApplyProgress(
        IDictionary<string, float> progressById,
        string id,
        float currentProgress,
        float requestedGain,
        ref float monthlyBudget,
        float monthlyDecay)
    {
        var progress = requestedGain <= 0.0f
            ? Math.Max(0.0f, currentProgress - monthlyDecay)
            : Math.Min(
                ProgressionPacingConstants.StageThreshold,
                currentProgress + Math.Min(requestedGain, Math.Max(0.0f, monthlyBudget)));

        if (requestedGain > 0.0f)
        {
            monthlyBudget = Math.Max(0.0f, monthlyBudget - Math.Min(requestedGain, Math.Max(0.0f, monthlyBudget)));
        }

        if (progress <= 0.0f)
        {
            progressById.Remove(id);
            return 0.0f;
        }

        progressById[id] = progress;
        return progress;
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
        return $"Gather=[{SummarizeCounters(evidence.SuccessfulGatheringMonthsByRegionId)}] | Hunt=[{SummarizeCounters(evidence.SuccessfulHuntingMonthsByRegionId)}] | Residence=[{SummarizeCounters(evidence.MonthsSpentByRegionId)}] | Water=[{SummarizeCounters(evidence.WaterExposureMonthsByRegionId)}] | Routes=[{SummarizeCounters(evidence.RouteTraversalCountsByRouteId)}] | Materials=[{SummarizeCounters(evidence.MaterialExposureMonthsByResourceId)}] | Contact=[{SummarizeCounters(evidence.ContactMonthsByPolityId)}] | Progress=[{SummarizeProgress(evidence.DiscoveryProgressByDiscoveryId)}] | Pressure=F{evidence.RecurringFoodPressureMonths}/T{evidence.RecurringThreatPressureMonths}/M{evidence.RecurringMaterialShortageMonths}";
    }

    private static string SummarizeCounters(IReadOnlyDictionary<string, int> counters)
    {
        if (counters.Count == 0)
        {
            return "none";
        }

        return string.Join(", ", counters.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => $"{entry.Key}:{entry.Value}"));
    }

    private static string SummarizeProgress(IReadOnlyDictionary<string, float> counters)
    {
        if (counters.Count == 0)
        {
            return "none";
        }

        return string.Join(", ", counters.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => $"{entry.Key}:{entry.Value:0}"));
    }

    private static PopulationGroup CloneGroup(PopulationGroup group)
    {
        return new PopulationGroup
        {
            Id = group.Id,
            Name = group.Name,
            SpeciesId = group.SpeciesId,
            SpeciesClass = group.SpeciesClass,
            PolityId = group.PolityId,
            CurrentRegionId = group.CurrentRegionId,
            OriginRegionId = group.OriginRegionId,
            Population = group.Population,
            StoredFood = group.StoredFood,
            HungerPressure = group.HungerPressure,
            ShortageMonths = group.ShortageMonths,
            FoodStressState = group.FoodStressState,
            SubsistencePreference = group.SubsistencePreference,
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

    private sealed class SpeciesAwarenessExposure
    {
        public SpeciesAwarenessExposure(string speciesId, SpeciesClass speciesClass)
        {
            SpeciesId = speciesId;
            SpeciesClass = speciesClass;
        }

        public string SpeciesId { get; }

        public SpeciesClass SpeciesClass { get; }

        public float ContactScore { get; set; }

        public float OverlapScore { get; set; }

        public float BehaviorAlignment { get; set; }

        public int PresenceContacts { get; set; }

        public int SuccessfulInteractions { get; set; }

        public float SuccessfulUseIntensity { get; set; }
    }
}
