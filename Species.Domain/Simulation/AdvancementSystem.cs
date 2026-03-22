using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class AdvancementSystem
{
    public AdvancementResult Run(
        World world,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        IReadOnlyList<GroupSurvivalChange> survivalChanges,
        IReadOnlyList<MigrationChange> migrationChanges)
    {
        var survivalByGroupId = survivalChanges.ToDictionary(change => change.GroupId, StringComparer.Ordinal);
        var migrationByGroupId = migrationChanges.ToDictionary(change => change.GroupId, StringComparer.Ordinal);
        var polityContextById = world.Polities
            .Select(polity => PolityData.BuildContext(world, polity))
            .Where(context => context is not null)
            .ToDictionary(context => context!.Polity.Id, context => context!, StringComparer.Ordinal);
        var politiesById = world.Polities.ToDictionary(polity => polity.Id, StringComparer.Ordinal);
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var updatedGroups = new List<PopulationGroup>(world.PopulationGroups.Count);
        var changes = new List<AdvancementChange>(world.PopulationGroups.Count);

        foreach (var group in world.PopulationGroups.OrderBy(group => group.Id, StringComparer.Ordinal))
        {
            var updatedGroup = CloneGroup(group);
            var evidence = updatedGroup.AdvancementEvidence;
            survivalByGroupId.TryGetValue(group.Id, out var survivalChange);
            migrationByGroupId.TryGetValue(group.Id, out var migrationChange);
            polityContextById.TryGetValue(group.PolityId, out var polityContext);
            politiesById.TryGetValue(group.PolityId, out var polity);
            var region = regionsById.GetValueOrDefault(survivalChange?.CurrentRegionId ?? updatedGroup.CurrentRegionId);

            UpdateMonthlyEvidence(updatedGroup, polity, polityContext, region, survivalChange, migrationChange, floraCatalog, faunaCatalog);

            var unlockedThisMonth = new List<AdvancementDefinition>();
            var checks = new List<string>();
            var advancementBudget = ProgressionPacingConstants.AdvancementMonthlyBudget;
            var adoptionBudget = ProgressionPacingConstants.AdoptionMonthlyBudget;

            foreach (var definition in advancementCatalog.Definitions.OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                var eligibility = FirstWaveAdvancementEvaluator.Evaluate(definition, updatedGroup, polity, polityContext, region, floraCatalog, faunaCatalog);
                EvaluateAdvancement(
                    updatedGroup,
                    definition,
                    eligibility,
                    ref advancementBudget,
                    ref adoptionBudget,
                    unlockedThisMonth,
                    checks);
            }

            updatedGroups.Add(updatedGroup);
            changes.Add(new AdvancementChange
            {
                GroupId = updatedGroup.Id,
                GroupName = updatedGroup.Name,
                RelevantDiscoveriesSummary = BuildRelevantDiscoveriesSummary(updatedGroup, advancementCatalog),
                LearnedAdvancementsSummary = BuildLearnedAdvancementsSummary(updatedGroup, advancementCatalog),
                EvidenceSummary = BuildEvidenceSummary(updatedGroup.AdvancementEvidence),
                CheckSummary = checks.Count == 0 ? "none" : string.Join(" | ", checks),
                UnlockedAdvancementsSummary = unlockedThisMonth.Count == 0
                    ? "none"
                    : string.Join(", ", unlockedThisMonth.Select(definition => definition.Name)),
                PracticalEffectSummary = unlockedThisMonth.Count == 0
                    ? "No new advancement effect this month."
                    : string.Join(" ", unlockedThisMonth.Select(definition => definition.PracticalEffectSummary)),
                UnlockedAdvancementNames = unlockedThisMonth
                    .Select(definition => definition.Name)
                    .ToArray()
            });
        }

        return new AdvancementResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, world.Polities, world.FocalPolityId),
            changes);
    }

    private static void UpdateMonthlyEvidence(
        PopulationGroup group,
        Polity? polity,
        PolityContext? polityContext,
        Region? region,
        GroupSurvivalChange? survivalChange,
        MigrationChange? migrationChange,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        if (region is null)
        {
            return;
        }

        if (HasDiscoveredFloraTag(polity, region, floraCatalog, FloraTag.Edible))
        {
            group.AdvancementEvidence.ForagingOpportunityMonths++;
        }

        if (HasDiscoveredFaunaTag(polity, region, faunaCatalog, FaunaTag.SmallPrey))
        {
            group.AdvancementEvidence.SmallPreyOpportunityMonths++;
        }

        if (HasDiscoveredFaunaTag(polity, region, faunaCatalog, FaunaTag.LargePrey))
        {
            group.AdvancementEvidence.LargePreyOpportunityMonths++;
        }

        if (region.WaterAvailability is WaterAvailability.Medium or WaterAvailability.High &&
            HasDiscoveredFaunaTag(polity, region, faunaCatalog, FaunaTag.AquaticFood))
        {
            group.AdvancementEvidence.AquaticOpportunityMonths++;
        }

        if (group.KnownDiscoveryIds.Contains(DiscoveryCatalog.ToolStoneId) &&
            region.MaterialProfile.Opportunities.Stone > 0)
        {
            group.AdvancementEvidence.StoneAccessMonths++;
        }

        if (HasDiscoveredFaunaTag(polity, region, faunaCatalog, FaunaTag.HideSource))
        {
            group.AdvancementEvidence.HideAccessMonths++;
        }

        if (HasDiscoveredFloraTag(polity, region, floraCatalog, FloraTag.FiberSource))
        {
            group.AdvancementEvidence.FiberAccessMonths++;
        }

        if (survivalChange is not null &&
            (survivalChange.TotalFoodAcquired > survivalChange.MonthlyFoodNeed ||
             survivalChange.StoredFoodAfter > survivalChange.StoredFoodBefore))
        {
            group.AdvancementEvidence.SurplusOpportunityMonths++;
        }

        if (survivalChange is not null &&
            ((survivalChange.StoredFoodAfter > 0 && survivalChange.Shortage > 0) ||
             survivalChange.StoredFoodBefore > 0 ||
             group.ShortageMonths > 0))
        {
            group.AdvancementEvidence.SpoilagePressureMonths++;
        }

        if (survivalChange is not null &&
            (survivalChange.Shortage > 0 ||
             survivalChange.FoodStressState is nameof(FoodStressState.HungerPressure) or nameof(FoodStressState.SevereShortage) or nameof(FoodStressState.Starvation)))
        {
            group.AdvancementEvidence.FoodPressureMonths++;
        }

        if ((polityContext?.MaterialProduction.DeficitScore ?? 0) >= 45 ||
            (polity?.MaterialShortageMonths ?? 0) > 0 ||
            group.Pressures.Threat.EffectiveValue >= 45)
        {
            group.AdvancementEvidence.MaterialNeedMonths++;
        }

        if (polityContext is not null &&
            polityContext.AnchoringKind is not PolityAnchoringKind.Mobile &&
            group.Pressures.Migration.EffectiveValue < 60 &&
            (migrationChange is null || !migrationChange.Moved))
        {
            group.AdvancementEvidence.StabilityMonths++;
        }

        if (polityContext is not null &&
            polityContext.AnchoringKind is PolityAnchoringKind.SemiRooted or PolityAnchoringKind.Anchored)
        {
            group.AdvancementEvidence.AnchoredContinuityMonths++;
        }

        if (group.Population >= 35 ||
            polityContext?.PrimarySettlement is not null ||
            polityContext?.AnchoringKind is PolityAnchoringKind.SemiRooted or PolityAnchoringKind.Anchored)
        {
            group.AdvancementEvidence.OrganizationalReadinessMonths++;
        }
    }

    private static void EvaluateAdvancement(
        PopulationGroup group,
        AdvancementDefinition definition,
        FirstWaveAdvancementEvaluator.AdvancementEligibility eligibility,
        ref float advancementBudget,
        ref float adoptionBudget,
        ICollection<AdvancementDefinition> unlockedThisMonth,
        ICollection<string> checks)
    {
        if (group.LearnedAdvancementIds.Contains(definition.Id))
        {
            checks.Add($"{definition.Name}: already learned.");
            return;
        }

        var capabilityProgress = group.AdvancementEvidence.AdvancementProgressById.GetValueOrDefault(definition.Id);
        var adoptionProgress = group.AdvancementEvidence.AdoptionProgressById.GetValueOrDefault(definition.Id);

        if (!eligibility.PrerequisitesMet)
        {
            capabilityProgress = ApplyProgress(group.AdvancementEvidence.AdvancementProgressById, definition.Id, capabilityProgress, 0.0f, ref advancementBudget, ProgressionPacingConstants.AdvancementMonthlyDecay);
            adoptionProgress = ApplyProgress(group.AdvancementEvidence.AdoptionProgressById, definition.Id, adoptionProgress, 0.0f, ref adoptionBudget, ProgressionPacingConstants.AdoptionMonthlyDecay);
            checks.Add($"{definition.Name}: {eligibility.StatusSummary}");
            return;
        }

        var evidenceRatio = eligibility.RequiredOpportunityCount <= 0
            ? 0.0f
            : Math.Clamp(eligibility.OpportunityCount / (float)eligibility.RequiredOpportunityCount, 0.0f, 2.0f);
        var capabilityGain = Math.Min(
            ProgressionPacingConstants.AdvancementMonthlyGainCap,
            1.00f + evidenceRatio * 5.0f + eligibility.NeedFactor * 2.2f);
        capabilityProgress = ApplyProgress(group.AdvancementEvidence.AdvancementProgressById, definition.Id, capabilityProgress, capabilityGain, ref advancementBudget, ProgressionPacingConstants.AdvancementMonthlyDecay);

        if (capabilityProgress < ProgressionPacingConstants.StageThreshold)
        {
            adoptionProgress = ApplyProgress(group.AdvancementEvidence.AdoptionProgressById, definition.Id, adoptionProgress, 0.0f, ref adoptionBudget, ProgressionPacingConstants.AdoptionMonthlyDecay);
            checks.Add($"{definition.Name}: capability progress {capabilityProgress:0}/100 from {eligibility.OpportunityCount}/{eligibility.RequiredOpportunityCount} causal opportunity.");
            return;
        }

        var adoptionGain = Math.Min(
            ProgressionPacingConstants.AdoptionMonthlyGainCap,
            0.75f + evidenceRatio * 3.0f + eligibility.NeedFactor * 1.8f);
        adoptionProgress = ApplyProgress(group.AdvancementEvidence.AdoptionProgressById, definition.Id, adoptionProgress, adoptionGain, ref adoptionBudget, ProgressionPacingConstants.AdoptionMonthlyDecay);

        if (adoptionProgress >= ProgressionPacingConstants.StageThreshold)
        {
            group.LearnedAdvancementIds.Add(definition.Id);
            group.AdvancementEvidence.AdvancementProgressById.Remove(definition.Id);
            group.AdvancementEvidence.AdoptionProgressById.Remove(definition.Id);
            unlockedThisMonth.Add(definition);
            checks.Add($"{definition.Name}: learned from repeated causal opportunity.");
            return;
        }

        checks.Add($"{definition.Name}: capability {capabilityProgress:0}/100, adoption {adoptionProgress:0}/100 from {eligibility.OpportunityCount}/{eligibility.RequiredOpportunityCount} opportunity.");
    }

    private static float ApplyProgress(
        IDictionary<string, float> progressById,
        string id,
        float currentProgress,
        float requestedGain,
        ref float monthlyBudget,
        float monthlyDecay)
    {
        var gain = Math.Min(requestedGain, Math.Max(0.0f, monthlyBudget));
        var progress = requestedGain <= 0.0f
            ? Math.Max(0.0f, currentProgress - monthlyDecay)
            : Math.Min(ProgressionPacingConstants.StageThreshold, currentProgress + gain);

        if (requestedGain > 0.0f)
        {
            monthlyBudget = Math.Max(0.0f, monthlyBudget - gain);
        }

        if (progress <= 0.0f)
        {
            progressById.Remove(id);
            return 0.0f;
        }

        progressById[id] = progress;
        return progress;
    }

    private static string BuildRelevantDiscoveriesSummary(PopulationGroup group, AdvancementCatalog advancementCatalog)
    {
        var relevantIds = advancementCatalog.Definitions
            .SelectMany(definition => definition.RequiredDiscoveryIds)
            .Distinct(StringComparer.Ordinal)
            .Where(group.KnownDiscoveryIds.Contains)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        return relevantIds.Length == 0 ? "none" : string.Join(", ", relevantIds);
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
        return $"Forage={evidence.ForagingOpportunityMonths} | SmallPrey={evidence.SmallPreyOpportunityMonths} | LargePrey={evidence.LargePreyOpportunityMonths} | Aquatic={evidence.AquaticOpportunityMonths} | Surplus={evidence.SurplusOpportunityMonths} | Spoilage={evidence.SpoilagePressureMonths} | Stone={evidence.StoneAccessMonths} | Hide={evidence.HideAccessMonths} | Fiber={evidence.FiberAccessMonths} | FoodNeed={evidence.FoodPressureMonths} | MaterialNeed={evidence.MaterialNeedMonths} | Stability={evidence.StabilityMonths} | Anchor={evidence.AnchoredContinuityMonths} | Organization={evidence.OrganizationalReadinessMonths} | CapabilityProgress=[{SummarizeProgress(evidence.AdvancementProgressById)}] | AdoptionProgress=[{SummarizeProgress(evidence.AdoptionProgressById)}]";
    }

    private static string SummarizeProgress(IReadOnlyDictionary<string, float> progressById)
    {
        if (progressById.Count == 0)
        {
            return "none";
        }

        return string.Join(", ", progressById.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => $"{entry.Key}:{entry.Value:0}"));
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
            FoodAccounting = group.FoodAccounting.Clone(),
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

    private static bool HasDiscoveredFloraTag(Polity? polity, Region region, FloraSpeciesCatalog floraCatalog, FloraTag tag)
    {
        return region.Ecosystem.FloraPopulations
            .Where(entry => entry.Value > 0)
            .Any(entry =>
            {
                var flora = floraCatalog.GetById(entry.Key);
                return flora is not null &&
                       flora.Tags.Contains(tag) &&
                       polity?.SpeciesAwareness.Any(state =>
                           state.SpeciesClass == SpeciesClass.Flora &&
                           string.Equals(state.SpeciesId, entry.Key, StringComparison.Ordinal) &&
                           state.CurrentStage == Discovery.DiscoveryStage.Discovered) == true;
            });
    }

    private static bool HasDiscoveredFaunaTag(Polity? polity, Region region, FaunaSpeciesCatalog faunaCatalog, FaunaTag tag)
    {
        return region.Ecosystem.FaunaPopulations
            .Where(entry => entry.Value > 0)
            .Any(entry =>
            {
                var fauna = faunaCatalog.GetById(entry.Key);
                return fauna is not null &&
                       fauna.Tags.Contains(tag) &&
                       polity?.SpeciesAwareness.Any(state =>
                           state.SpeciesClass == SpeciesClass.Fauna &&
                           string.Equals(state.SpeciesId, entry.Key, StringComparison.Ordinal) &&
                           state.CurrentStage == Discovery.DiscoveryStage.Discovered) == true;
            });
    }
}
