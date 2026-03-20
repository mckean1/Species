using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Knowledge;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class MigrationSystem
{
    public MigrationResult Run(
        World world,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        IReadOnlyList<GroupSurvivalChange> survivalChanges)
    {
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var survivalByGroupId = survivalChanges.ToDictionary(change => change.GroupId, StringComparer.Ordinal);
        var updatedGroups = new List<PopulationGroup>(world.PopulationGroups.Count);
        var changes = new List<MigrationChange>(world.PopulationGroups.Count);

        foreach (var group in world.PopulationGroups.OrderBy(group => group.Id, StringComparer.Ordinal))
        {
            if (!regionsById.TryGetValue(group.CurrentRegionId, out var currentRegion))
            {
                updatedGroups.Add(CloneGroup(group));
                continue;
            }

            survivalByGroupId.TryGetValue(group.Id, out var survivalChange);
            var knowledgeContext = GroupKnowledgeContext.Create(world, group, discoveryCatalog, floraCatalog, faunaCatalog);
            var consideration = ResolveMigrationConsideration(group, survivalChange);
            var currentScore = ScoreRegion(group, currentRegion, knowledgeContext.ObserveRegion(currentRegion, currentRegion.Id));
            var evaluatedNeighbors = currentRegion.NeighborIds
                .Where(regionsById.ContainsKey)
                .Select(neighborId => BuildCandidate(group, currentRegion, regionsById[neighborId], knowledgeContext))
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Region.Id, StringComparer.Ordinal)
                .ToArray();

            var bestNeighbor = evaluatedNeighbors.FirstOrDefault();
            var requiredMoveMargin = ResolveRequiredMoveMargin(group, consideration);
            var shouldMove = consideration.ShouldConsider &&
                             bestNeighbor is not null &&
                             bestNeighbor.Score >= currentScore + requiredMoveMargin;

            var updatedGroup = CloneGroup(group);
            string reason;

            if (shouldMove && bestNeighbor is not null)
            {
                updatedGroup.LastRegionId = group.CurrentRegionId;
                updatedGroup.CurrentRegionId = bestNeighbor.Region.Id;
                updatedGroup.MonthsSinceLastMove = 0;
                updatedGroup.KnownRegionIds.Add(bestNeighbor.Region.Id);
                reason = $"Moved because {consideration.ReasonText} {bestNeighbor.Region.Name} scored {bestNeighbor.Score:0.0} versus current {currentScore:0.0}, clearing the required move margin {requiredMoveMargin:0.0}.";
            }
            else
            {
                updatedGroup.MonthsSinceLastMove = group.MonthsSinceLastMove + 1;
                reason = BuildStayReason(group, consideration, currentScore, bestNeighbor, survivalChange, requiredMoveMargin);
            }

            updatedGroups.Add(updatedGroup);
            changes.Add(new MigrationChange
            {
                GroupId = group.Id,
                GroupName = group.Name,
                CurrentRegionId = group.CurrentRegionId,
                CurrentRegionName = currentRegion.Name,
                MigrationPressure = group.Pressures.Migration.DisplayValue,
                MigrationEffectivePressure = group.Pressures.Migration.EffectiveValue,
                MigrationSeverityLabel = group.Pressures.Migration.SeverityLabel,
                StoredFood = group.StoredFood,
                ConsideredMigration = consideration.ShouldConsider,
                RequiredMoveMargin = requiredMoveMargin,
                CurrentRegionScore = currentScore,
                NeighborScoresSummary = BuildNeighborSummary(evaluatedNeighbors),
                WinningRegionId = bestNeighbor?.Region.Id ?? group.CurrentRegionId,
                WinningRegionName = bestNeighbor?.Region.Name ?? currentRegion.Name,
                WinningRegionScore = bestNeighbor?.Score ?? currentScore,
                Moved = shouldMove,
                NewRegionId = updatedGroup.CurrentRegionId,
                NewRegionName = regionsById.GetValueOrDefault(updatedGroup.CurrentRegionId)?.Name ?? currentRegion.Name,
                LastRegionId = updatedGroup.LastRegionId,
                MonthsSinceLastMove = updatedGroup.MonthsSinceLastMove,
                DecisionReason = reason
            });
        }

        return new MigrationResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, world.Polities, world.FocalPolityId),
            changes);
    }

    private static MigrationConsideration ResolveMigrationConsideration(PopulationGroup group, GroupSurvivalChange? survivalChange)
    {
        var effectivePressure = group.Pressures.Migration.EffectiveValue;
        var severity = group.Pressures.Migration.SeverityLabel.ToLowerInvariant();

        if (group.MonthsSinceLastMove <= MigrationConstants.RecentMoveCooldownMonths &&
            effectivePressure < MigrationConstants.ExtremeMigrationPressureTrigger)
        {
            return new MigrationConsideration(
                false,
                $"migration pressure was {effectivePressure} effective ({severity}, display {group.Pressures.Migration.DisplayValue}) but the group moved {group.MonthsSinceLastMove} month(s) ago and recent-move restraint held");
        }

        if (effectivePressure >= MigrationConstants.ExtremeMigrationPressureTrigger)
        {
            return new MigrationConsideration(
                true,
                $"migration pressure was extreme at {effectivePressure} effective ({severity}, display {group.Pressures.Migration.DisplayValue})");
        }

        if (effectivePressure >= MigrationConstants.MigrationPressureTrigger)
        {
            return new MigrationConsideration(
                true,
                $"migration pressure was {effectivePressure} effective ({severity}, display {group.Pressures.Migration.DisplayValue}) and crossed the trigger {MigrationConstants.MigrationPressureTrigger}");
        }

        if (survivalChange is null)
        {
            return new MigrationConsideration(
                false,
                $"migration pressure was {effectivePressure} effective ({severity}, display {group.Pressures.Migration.DisplayValue}) and no acute survival trigger was present");
        }

        if (survivalChange.Shortage >= MigrationConstants.SevereShortageTrigger)
        {
            return new MigrationConsideration(
                true,
                $"shortage {survivalChange.Shortage} crossed the severe-shortage trigger {MigrationConstants.SevereShortageTrigger} while migration pressure remained {effectivePressure}");
        }

        if (survivalChange.StarvationLoss >= MigrationConstants.SevereStarvationTrigger)
        {
            return new MigrationConsideration(
                true,
                $"starvation loss {survivalChange.StarvationLoss} crossed the starvation trigger {MigrationConstants.SevereStarvationTrigger} while migration pressure remained {effectivePressure}");
        }

        var storedFoodPerPopulationUnit = group.Population <= 0
            ? 0.0f
            : group.StoredFood / (float)group.Population;

        if (storedFoodPerPopulationUnit <= MigrationConstants.LowStoredFoodPerPopulationUnit)
        {
            return new MigrationConsideration(
                true,
                $"stored food per population unit fell to {storedFoodPerPopulationUnit:0.00}, below the low-stores trigger {MigrationConstants.LowStoredFoodPerPopulationUnit:0.00}");
        }

        return new MigrationConsideration(
            false,
            $"migration pressure stayed below the trigger at {effectivePressure} effective ({severity}, display {group.Pressures.Migration.DisplayValue}) and survival remained tolerable");
    }

    private static CandidateScore BuildCandidate(
        PopulationGroup group,
        Region currentRegion,
        Region region,
        GroupKnowledgeContext knowledgeContext)
    {
        return new CandidateScore(region, ScoreRegion(group, currentRegion, knowledgeContext.ObserveRegion(region, currentRegion.Id)));
    }

    private static float ScoreRegion(
        PopulationGroup group,
        Region currentRegion,
        RegionKnowledgeSnapshot knowledge)
    {
        var monthlyFoodNeed = SubsistenceSupportModel.CalculateMonthlyFoodNeed(group.Population);
        var floraSupport = SubsistenceSupportModel.NormalizeFoodSupport(knowledge.GatheringPotentialFood, monthlyFoodNeed);
        var faunaSupport = SubsistenceSupportModel.NormalizeFoodSupport(knowledge.HuntingPotentialFood, monthlyFoodNeed);
        var waterSupport = knowledge.WaterSupport;
        var threatPenalty = knowledge.ThreatPressure;
        var floraConfidence = ResolveConfidence(knowledge.FloraKnowledge);
        var faunaConfidence = ResolveConfidence(knowledge.FaunaKnowledge);
        var waterConfidence = ResolveConfidence(knowledge.WaterKnowledge);
        var routeConfidence = ResolveConfidence(knowledge.RouteKnowledge);

        var (floraWeight, faunaWeight, waterWeight, threatWeight) = group.SubsistenceMode switch
        {
            SubsistenceMode.Gatherer => (
                MigrationConstants.GathererFloraWeight,
                MigrationConstants.GathererFaunaWeight,
                MigrationConstants.GathererWaterWeight,
                MigrationConstants.GathererThreatWeight),
            SubsistenceMode.Hunter => (
                MigrationConstants.HunterFloraWeight,
                MigrationConstants.HunterFaunaWeight,
                MigrationConstants.HunterWaterWeight,
                MigrationConstants.HunterThreatWeight),
            _ => (
                MigrationConstants.MixedFloraWeight,
                MigrationConstants.MixedFaunaWeight,
                MigrationConstants.MixedWaterWeight,
                MigrationConstants.MixedThreatWeight)
        };

        var score = (floraSupport * floraConfidence) * floraWeight +
                    (faunaSupport * faunaConfidence) * faunaWeight +
                    (waterSupport * waterConfidence) * waterWeight +
                    (100.0f - (threatPenalty * routeConfidence)) * threatWeight;

        score += knowledge.IsKnownRegion
            ? MigrationConstants.KnownRegionBonus
            : -MigrationConstants.UnknownRegionPenalty;

        if (knowledge.ConditionsKnowledge == KnowledgeLevel.Known)
        {
            score += DiscoveryConstants.LocalRegionConditionsBonus;
        }

        if (!string.Equals(currentRegion.Id, knowledge.RegionId, StringComparison.Ordinal))
        {
            score += knowledge.RouteKnowledge == KnowledgeLevel.Known
                ? DiscoveryConstants.KnownRouteBonus
                : knowledge.RouteKnowledge == KnowledgeLevel.Partial || knowledge.RouteKnowledge == KnowledgeLevel.Rumored
                    ? -DiscoveryConstants.UnknownRoutePenalty * 0.5f
                    : -DiscoveryConstants.UnknownRoutePenalty;

            if (group.LearnedAdvancementIds.Contains(AdvancementCatalog.OrganizedTravelId) &&
                knowledge.RouteKnowledge == KnowledgeLevel.Known)
            {
                score += AdvancementConstants.OrganizedTravelKnownRouteBonus;
            }
        }

        if (!string.IsNullOrWhiteSpace(group.LastRegionId) && string.Equals(group.LastRegionId, knowledge.RegionId, StringComparison.Ordinal))
        {
            score -= MigrationConstants.ReturnToLastRegionPenalty;
        }

        return score;
    }

    private static float ResolveConfidence(KnowledgeLevel level)
    {
        return level switch
        {
            KnowledgeLevel.Known => 1.00f,
            KnowledgeLevel.Partial => 0.82f,
            KnowledgeLevel.Rumored => 0.62f,
            _ => 0.45f
        };
    }

    private static string BuildNeighborSummary(IReadOnlyList<CandidateScore> evaluatedNeighbors)
    {
        if (evaluatedNeighbors.Count == 0)
        {
            return "none";
        }

        return string.Join(
            ", ",
            evaluatedNeighbors.Select(candidate => $"{candidate.Region.Id} ({candidate.Region.Name})={candidate.Score:0.0}"));
    }

    private static string BuildStayReason(
        PopulationGroup group,
        MigrationConsideration consideration,
        float currentScore,
        CandidateScore? bestNeighbor,
        GroupSurvivalChange? survivalChange,
        float requiredMoveMargin)
    {
        if (!consideration.ShouldConsider)
        {
            return $"Stayed because {consideration.ReasonText}.";
        }

        if (bestNeighbor is null)
        {
            return $"Stayed because {consideration.ReasonText}, but no neighboring region was available to evaluate.";
        }

        if (!string.IsNullOrWhiteSpace(group.LastRegionId) && string.Equals(group.LastRegionId, bestNeighbor.Region.Id, StringComparison.Ordinal))
        {
            return $"Stayed because {consideration.ReasonText}, but returning to {bestNeighbor.Region.Name} was penalized by anti-thrashing and did not clear the required move margin {requiredMoveMargin:0.0}.";
        }

        if (survivalChange is not null && survivalChange.Shortage > 0)
        {
            return $"Stayed because {consideration.ReasonText}, but the best neighbor scored only {bestNeighbor.Score:0.0} versus current {currentScore:0.0} and did not clear the required move margin {requiredMoveMargin:0.0}.";
        }

        return $"Stayed because {consideration.ReasonText}, and no neighboring region was sufficiently better than the current score {currentScore:0.0}; required move margin was {requiredMoveMargin:0.0}.";
    }

    private static float ResolveRequiredMoveMargin(PopulationGroup group, MigrationConsideration consideration)
    {
        var requiredMargin = MigrationConstants.MinimumMoveMargin;
        if (group.MonthsSinceLastMove <= MigrationConstants.RecentMoveCooldownMonths)
        {
            requiredMargin += MigrationConstants.RecentMoveMarginPenalty;
        }

        if (!string.IsNullOrWhiteSpace(group.LastRegionId))
        {
            requiredMargin += MigrationConstants.ReturnMoveMarginPenalty;
        }

        if (consideration.WasEmergencyTrigger)
        {
            requiredMargin = Math.Max(MigrationConstants.MinimumMoveMargin - 2.0f, requiredMargin - 4.0f);
        }

        if (group.Pressures.Migration.EffectiveValue >= MigrationConstants.ExtremeMigrationPressureTrigger)
        {
            requiredMargin = Math.Max(MigrationConstants.MinimumMoveMargin - 2.0f, requiredMargin - 2.0f);
        }

        return requiredMargin;
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

    private sealed record CandidateScore(Region Region, float Score);

    private sealed record MigrationConsideration(bool ShouldConsider, string ReasonText)
    {
        public bool WasEmergencyTrigger =>
            ReasonText.Contains("severe-shortage trigger", StringComparison.Ordinal) ||
            ReasonText.Contains("starvation trigger", StringComparison.Ordinal) ||
            ReasonText.Contains("low-stores trigger", StringComparison.Ordinal);
    }
}
