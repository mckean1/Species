using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class AdvancementSystem
{
    public AdvancementResult Run(
        World world,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog,
        IReadOnlyList<GroupSurvivalChange> survivalChanges,
        IReadOnlyList<MigrationChange> migrationChanges)
    {
        var survivalByGroupId = survivalChanges.ToDictionary(change => change.GroupId, StringComparer.Ordinal);
        var migrationByGroupId = migrationChanges.ToDictionary(change => change.GroupId, StringComparer.Ordinal);
        var polityContextById = world.Polities
            .Select(polity => PolityData.BuildContext(world, polity))
            .Where(context => context is not null)
            .ToDictionary(context => context!.Polity.Id, context => context!, StringComparer.Ordinal);
        var updatedGroups = new List<PopulationGroup>(world.PopulationGroups.Count);
        var changes = new List<AdvancementChange>(world.PopulationGroups.Count);

        foreach (var group in world.PopulationGroups.OrderBy(group => group.Id, StringComparer.Ordinal))
        {
            var updatedGroup = CloneGroup(group);
            var evidence = updatedGroup.AdvancementEvidence;
            survivalByGroupId.TryGetValue(group.Id, out var survivalChange);
            migrationByGroupId.TryGetValue(group.Id, out var migrationChange);
            polityContextById.TryGetValue(group.PolityId, out var polityContext);

            var livedRegionId = survivalChange?.CurrentRegionId ?? updatedGroup.CurrentRegionId;
            var localFloraDiscoveryId = discoveryCatalog.GetLocalFloraDiscoveryId(livedRegionId);
            var localFaunaDiscoveryId = discoveryCatalog.GetLocalFaunaDiscoveryId(livedRegionId);
            var localRegionDiscoveryId = discoveryCatalog.GetLocalRegionConditionsDiscoveryId(livedRegionId);

            if (((survivalChange?.PrimaryAction == "Gather" && survivalChange.PrimaryFoodGained > 0) ||
                 (survivalChange?.FallbackAction == "Gather" && survivalChange.FallbackFoodGained > 0)) &&
                updatedGroup.KnownDiscoveryIds.Contains(localFloraDiscoveryId))
            {
                evidence.SuccessfulGatheringWithKnowledgeMonths++;
            }

            if (((survivalChange?.PrimaryAction == "Hunt" && survivalChange.PrimaryFoodGained > 0) ||
                 (survivalChange?.FallbackAction == "Hunt" && survivalChange.FallbackFoodGained > 0)) &&
                updatedGroup.KnownDiscoveryIds.Contains(localFaunaDiscoveryId))
            {
                evidence.SuccessfulHuntingWithKnowledgeMonths++;
            }

            if (updatedGroup.StoredFood > 0 && polityContext is not null && polityContext.MaterialProduction.StorageSupport >= 20)
            {
                evidence.SurplusStoredFoodMonths++;
            }

            if (migrationChange is { Moved: true } &&
                updatedGroup.KnownDiscoveryIds.Contains(discoveryCatalog.GetRouteDiscoveryId(migrationChange.CurrentRegionId, migrationChange.NewRegionId)))
            {
                evidence.KnownRouteTravelMonths++;
            }

            if (survivalChange is { Outcome: "Survived", Shortage: 0 } &&
                updatedGroup.KnownDiscoveryIds.Contains(localRegionDiscoveryId))
            {
                evidence.SuccessfulResidenceWithRegionKnowledgeMonths++;
            }

            if (polityContext is not null &&
                polityContext.MaterialProduction.ToolSupport >= 20 &&
                polityContext.MaterialProduction.SurplusScore >= 20)
            {
                evidence.MaterialPracticeMonths++;
            }

            if (updatedGroup.StoredFood > 0 &&
                (survivalChange?.Shortage > 0 || (polityContext is not null && polityContext.MaterialProduction.StorageSupport < 35)))
            {
                evidence.StoragePressureMonths++;
            }

            if (polityContext is not null &&
                polityContext.PrimarySettlement is not null &&
                polityContext.MaterialProduction.ShelterSupport >= 25 &&
                (updatedGroup.Pressures.Threat.EffectiveValue >= 45 || polityContext.MaterialShortageMonths > 0))
            {
                evidence.ShelterReadinessMonths++;
            }

            if (polityContext is not null &&
                polityContext.AnchoringKind is not Species.Domain.Enums.PolityAnchoringKind.Mobile &&
                polityContext.Pressures.Migration.EffectiveValue < 60 &&
                polityContext.MaterialProduction.DeficitScore < 60)
            {
                evidence.StabilityMonths++;
            }

            if (updatedGroup.DiscoveryEvidence.SharedExposureMonthsByDiscoveryId.Count > 0)
            {
                evidence.ContactLearningMonths++;
            }

            var unlockedThisMonth = new List<AdvancementDefinition>();
            var checks = new List<string>();

            EvaluateAdvancement(
                updatedGroup,
                advancementCatalog,
                AdvancementCatalog.ImprovedGatheringId,
                updatedGroup.KnownDiscoveryIds.Contains(localFloraDiscoveryId) &&
                updatedGroup.DiscoveryEvidence.RecurringFoodPressureMonths > 0,
                evidence.SuccessfulGatheringWithKnowledgeMonths,
                AdvancementConstants.ImprovedGatheringMonthsRequired + 1,
                "flora knowledge plus sustained need",
                unlockedThisMonth,
                checks);

            EvaluateAdvancement(
                updatedGroup,
                advancementCatalog,
                AdvancementCatalog.ImprovedHuntingId,
                updatedGroup.KnownDiscoveryIds.Contains(localFaunaDiscoveryId) &&
                updatedGroup.KnownDiscoveryIds.Contains(DiscoveryCatalog.SeasonalTrackingId) &&
                (updatedGroup.DiscoveryEvidence.RecurringFoodPressureMonths > 0 || updatedGroup.DiscoveryEvidence.RecurringThreatPressureMonths > 0),
                evidence.SuccessfulHuntingWithKnowledgeMonths,
                AdvancementConstants.ImprovedHuntingMonthsRequired + 1,
                "fauna knowledge, tracking, and sustained need",
                unlockedThisMonth,
                checks);

            EvaluateAdvancement(
                updatedGroup,
                advancementCatalog,
                AdvancementCatalog.FoodStorageId,
                updatedGroup.KnownDiscoveryIds.Contains(DiscoveryCatalog.PreservationCluesId) &&
                (updatedGroup.KnownDiscoveryIds.Contains(DiscoveryCatalog.ClayShapingId) || (polityContext?.MaterialProduction.StorageSupport ?? 0) >= 20) &&
                polityContext is not null &&
                polityContext.AnchoringKind is not Species.Domain.Enums.PolityAnchoringKind.Mobile,
                Math.Min(evidence.SurplusStoredFoodMonths, evidence.StoragePressureMonths),
                AdvancementConstants.FoodStorageSurplusMonthsRequired,
                "preservation knowledge plus durable storage conditions",
                unlockedThisMonth,
                checks);

            EvaluateAdvancement(
                updatedGroup,
                advancementCatalog,
                AdvancementCatalog.OrganizedTravelId,
                evidence.KnownRouteTravelMonths > 0 &&
                evidence.StabilityMonths > 0,
                evidence.KnownRouteTravelMonths,
                AdvancementConstants.OrganizedTravelKnownRouteMonthsRequired,
                "known route use plus continuity",
                unlockedThisMonth,
                checks);

            EvaluateAdvancement(
                updatedGroup,
                advancementCatalog,
                AdvancementCatalog.LocalResourceUseId,
                updatedGroup.KnownDiscoveryIds.Contains(localRegionDiscoveryId) &&
                polityContext is not null &&
                polityContext.MaterialProduction.ToolSupport >= 20 &&
                evidence.StabilityMonths > 0,
                Math.Min(evidence.MaterialPracticeMonths, evidence.SuccessfulResidenceWithRegionKnowledgeMonths),
                AdvancementConstants.LocalResourceUseMonthsRequired,
                "local region knowledge plus material practice",
                unlockedThisMonth,
                checks);

            EvaluateAdvancement(
                updatedGroup,
                advancementCatalog,
                AdvancementCatalog.StrongerShelterId,
                updatedGroup.KnownDiscoveryIds.Contains(DiscoveryCatalog.ShelterMethodsId) &&
                polityContext is not null &&
                polityContext.PrimarySettlement is not null &&
                polityContext.MaterialProduction.ShelterSupport >= 25,
                evidence.ShelterReadinessMonths,
                AdvancementConstants.StrongerShelterMonthsRequired,
                "shelter-method knowledge plus settled material readiness",
                unlockedThisMonth,
                checks);

            updatedGroups.Add(updatedGroup);
            changes.Add(new AdvancementChange
            {
                GroupId = updatedGroup.Id,
                GroupName = updatedGroup.Name,
                RelevantDiscoveriesSummary = BuildRelevantDiscoveriesSummary(updatedGroup, discoveryCatalog, livedRegionId),
                LearnedAdvancementsSummary = BuildLearnedAdvancementsSummary(updatedGroup, advancementCatalog),
                EvidenceSummary = BuildEvidenceSummary(updatedGroup.AdvancementEvidence),
                CheckSummary = checks.Count == 0 ? "none" : string.Join(" | ", checks),
                UnlockedAdvancementsSummary = unlockedThisMonth.Count == 0
                    ? "none"
                    : string.Join(", ", unlockedThisMonth.Select(definition => definition.Name)),
                PracticalEffectSummary = unlockedThisMonth.Count == 0
                    ? "No new advancement effect this month."
                    : string.Join(" ", unlockedThisMonth.Select(definition => definition.PracticalEffectSummary)),
                ChronicleLinesSummary = unlockedThisMonth.Count == 0
                    ? "none"
                    : string.Join(";;", unlockedThisMonth.Select(definition => BuildChronicleLine(updatedGroup.Name, definition)))
            });
        }

        return new AdvancementResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, world.Polities, world.FocalPolityId),
            changes);
    }

    private static void EvaluateAdvancement(
        PopulationGroup group,
        AdvancementCatalog advancementCatalog,
        string advancementId,
        bool prerequisiteMet,
        int evidenceCount,
        int requiredCount,
        string prerequisiteLabel,
        ICollection<AdvancementDefinition> unlockedThisMonth,
        ICollection<string> checks)
    {
        var definition = advancementCatalog.GetById(advancementId);
        if (definition is null)
        {
            return;
        }

        if (group.LearnedAdvancementIds.Contains(advancementId))
        {
            checks.Add($"{definition.Name}: already learned.");
            return;
        }

        if (!prerequisiteMet)
        {
            checks.Add($"{definition.Name}: waiting on {prerequisiteLabel}.");
            return;
        }

        if (evidenceCount >= requiredCount)
        {
            group.LearnedAdvancementIds.Add(advancementId);
            unlockedThisMonth.Add(definition);
            checks.Add($"{definition.Name}: learned at {evidenceCount}/{requiredCount}.");
            return;
        }

        checks.Add($"{definition.Name}: {evidenceCount}/{requiredCount} after {prerequisiteLabel}.");
    }

    private static string BuildRelevantDiscoveriesSummary(PopulationGroup group, DiscoveryCatalog discoveryCatalog, string regionId)
    {
        var relevantIds = new[]
        {
            discoveryCatalog.GetLocalFloraDiscoveryId(regionId),
            discoveryCatalog.GetLocalFaunaDiscoveryId(regionId),
            discoveryCatalog.GetLocalWaterSourcesDiscoveryId(regionId),
            discoveryCatalog.GetLocalRegionConditionsDiscoveryId(regionId),
            DiscoveryCatalog.ClayShapingId,
            DiscoveryCatalog.SeasonalTrackingId,
            DiscoveryCatalog.PreservationCluesId,
            DiscoveryCatalog.ShelterMethodsId
        };

        var names = relevantIds
            .Where(group.KnownDiscoveryIds.Contains)
            .Select(id => discoveryCatalog.GetById(id)?.Name ?? id)
            .ToArray();

        return names.Length == 0 ? "none" : string.Join(", ", names);
    }

    private static string BuildLearnedAdvancementsSummary(PopulationGroup group, AdvancementCatalog advancementCatalog)
    {
        if (group.LearnedAdvancementIds.Count == 0)
        {
            return "none";
        }

        return string.Join(
            ", ",
            group.LearnedAdvancementIds
                .OrderBy(id => id, StringComparer.Ordinal)
                .Select(id => advancementCatalog.GetById(id)?.Name ?? id));
    }

    private static string BuildChronicleLine(string groupName, AdvancementDefinition definition)
    {
        return definition.Id switch
        {
            var id when id == AdvancementCatalog.FoodStorageId => $"{groupName} adopted improved storage after repeated spoilage pressure.",
            var id when id == AdvancementCatalog.StrongerShelterId => $"{groupName} adopted stronger shelter from settled material practice.",
            var id when id == AdvancementCatalog.ImprovedHuntingId => $"{groupName} improved hunting practice through repeated tracking.",
            var id when id == AdvancementCatalog.ImprovedGatheringId => $"{groupName} improved gathering practice through repeated local use.",
            _ => $"{groupName} learned {definition.Name.ToLowerInvariant()}."
        };
    }

    private static string BuildEvidenceSummary(AdvancementEvidenceState evidence)
    {
        return $"GatherUse={evidence.SuccessfulGatheringWithKnowledgeMonths} | HuntUse={evidence.SuccessfulHuntingWithKnowledgeMonths} | StorageUse={evidence.SurplusStoredFoodMonths}/{evidence.StoragePressureMonths} | RouteUse={evidence.KnownRouteTravelMonths} | LocalUse={evidence.SuccessfulResidenceWithRegionKnowledgeMonths}/{evidence.MaterialPracticeMonths} | Shelter={evidence.ShelterReadinessMonths} | Stability={evidence.StabilityMonths}";
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
