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
        var updatedGroups = new List<PopulationGroup>(world.PopulationGroups.Count);
        var changes = new List<AdvancementChange>(world.PopulationGroups.Count);

        foreach (var group in world.PopulationGroups.OrderBy(group => group.Id, StringComparer.Ordinal))
        {
            var updatedGroup = CloneGroup(group);
            var evidence = updatedGroup.AdvancementEvidence;
            survivalByGroupId.TryGetValue(group.Id, out var survivalChange);
            migrationByGroupId.TryGetValue(group.Id, out var migrationChange);

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

            if (updatedGroup.StoredFood > 0)
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

            var unlockedThisMonth = new List<AdvancementDefinition>();
            var checks = new List<string>();

            EvaluateAdvancement(
                updatedGroup,
                advancementCatalog,
                AdvancementCatalog.ImprovedGatheringId,
                updatedGroup.KnownDiscoveryIds.Contains(localFloraDiscoveryId),
                evidence.SuccessfulGatheringWithKnowledgeMonths,
                AdvancementConstants.ImprovedGatheringMonthsRequired,
                "local flora knowledge",
                unlockedThisMonth,
                checks);

            EvaluateAdvancement(
                updatedGroup,
                advancementCatalog,
                AdvancementCatalog.ImprovedHuntingId,
                updatedGroup.KnownDiscoveryIds.Contains(localFaunaDiscoveryId),
                evidence.SuccessfulHuntingWithKnowledgeMonths,
                AdvancementConstants.ImprovedHuntingMonthsRequired,
                "local fauna knowledge",
                unlockedThisMonth,
                checks);

            EvaluateAdvancement(
                updatedGroup,
                advancementCatalog,
                AdvancementCatalog.FoodStorageId,
                true,
                evidence.SurplusStoredFoodMonths,
                AdvancementConstants.FoodStorageSurplusMonthsRequired,
                "repeated food surplus",
                unlockedThisMonth,
                checks);

            EvaluateAdvancement(
                updatedGroup,
                advancementCatalog,
                AdvancementCatalog.OrganizedTravelId,
                evidence.KnownRouteTravelMonths > 0,
                evidence.KnownRouteTravelMonths,
                AdvancementConstants.OrganizedTravelKnownRouteMonthsRequired,
                "known route use",
                unlockedThisMonth,
                checks);

            EvaluateAdvancement(
                updatedGroup,
                advancementCatalog,
                AdvancementCatalog.LocalResourceUseId,
                updatedGroup.KnownDiscoveryIds.Contains(localRegionDiscoveryId),
                evidence.SuccessfulResidenceWithRegionKnowledgeMonths,
                AdvancementConstants.LocalResourceUseMonthsRequired,
                "local region knowledge",
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
                    : string.Join(" ", unlockedThisMonth.Select(definition => definition.PracticalEffectSummary))
            });
        }

        return new AdvancementResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle),
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
            discoveryCatalog.GetLocalRegionConditionsDiscoveryId(regionId)
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

    private static string BuildEvidenceSummary(AdvancementEvidenceState evidence)
    {
        return $"GatherUse={evidence.SuccessfulGatheringWithKnowledgeMonths} | HuntUse={evidence.SuccessfulHuntingWithKnowledgeMonths} | StorageUse={evidence.SurplusStoredFoodMonths} | RouteUse={evidence.KnownRouteTravelMonths} | LocalUse={evidence.SuccessfulResidenceWithRegionKnowledgeMonths}";
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
            GovernmentForm = group.GovernmentForm,
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
            AdvancementEvidence = group.AdvancementEvidence.Clone(),
            ActiveLawProposal = group.ActiveLawProposal?.Clone(),
            LawProposalHistory = group.LawProposalHistory.Select(proposal => proposal.Clone()).ToList(),
            EnactedLaws = group.EnactedLaws.Select(law => law.Clone()).ToList(),
            PoliticalBlocs = group.PoliticalBlocs.Select(bloc => bloc.Clone()).ToList()
        };
    }
}
