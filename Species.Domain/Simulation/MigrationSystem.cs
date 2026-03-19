using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class MigrationSystem
{
    public MigrationResult Run(
        World world,
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
            var shouldConsiderMigration = ShouldConsiderMigration(group, survivalChange);
            var currentScore = ScoreRegion(group, currentRegion, faunaCatalog);
            var evaluatedNeighbors = currentRegion.NeighborIds
                .Where(regionsById.ContainsKey)
                .Select(neighborId => BuildCandidate(group, regionsById[neighborId], faunaCatalog))
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Region.Id, StringComparer.Ordinal)
                .ToArray();

            var bestNeighbor = evaluatedNeighbors.FirstOrDefault();
            var shouldMove = shouldConsiderMigration &&
                             bestNeighbor is not null &&
                             bestNeighbor.Score >= currentScore + MigrationConstants.MinimumMoveMargin;

            var updatedGroup = CloneGroup(group);
            string reason;

            if (shouldMove && bestNeighbor is not null)
            {
                updatedGroup.LastRegionId = group.CurrentRegionId;
                updatedGroup.CurrentRegionId = bestNeighbor.Region.Id;
                updatedGroup.MonthsSinceLastMove = 0;
                updatedGroup.KnownRegionIds.Add(bestNeighbor.Region.Id);
                reason = $"Moved because migration pressure was {group.Pressures.MigrationPressure} and {bestNeighbor.Region.Name} scored {bestNeighbor.Score:0.0} versus {currentScore:0.0}.";
            }
            else
            {
                updatedGroup.MonthsSinceLastMove = group.MonthsSinceLastMove + 1;
                reason = BuildStayReason(group, shouldConsiderMigration, currentScore, bestNeighbor, survivalChange);
            }

            updatedGroups.Add(updatedGroup);
            changes.Add(new MigrationChange
            {
                GroupId = group.Id,
                GroupName = group.Name,
                CurrentRegionId = group.CurrentRegionId,
                CurrentRegionName = currentRegion.Name,
                MigrationPressure = group.Pressures.MigrationPressure,
                StoredFood = group.StoredFood,
                ConsideredMigration = shouldConsiderMigration,
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
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups),
            changes);
    }

    private static bool ShouldConsiderMigration(PopulationGroup group, GroupSurvivalChange? survivalChange)
    {
        if (group.Pressures.MigrationPressure >= MigrationConstants.MigrationPressureTrigger)
        {
            return true;
        }

        if (survivalChange is null)
        {
            return false;
        }

        if (survivalChange.Shortage >= MigrationConstants.SevereShortageTrigger)
        {
            return true;
        }

        if (survivalChange.StarvationLoss >= MigrationConstants.SevereStarvationTrigger)
        {
            return true;
        }

        var storedFoodPerPopulationUnit = group.Population <= 0
            ? 0.0f
            : group.StoredFood / (float)group.Population;

        return storedFoodPerPopulationUnit <= MigrationConstants.LowStoredFoodPerPopulationUnit;
    }

    private static CandidateScore BuildCandidate(PopulationGroup group, Region region, FaunaSpeciesCatalog faunaCatalog)
    {
        return new CandidateScore(region, ScoreRegion(group, region, faunaCatalog));
    }

    private static float ScoreRegion(PopulationGroup group, Region region, FaunaSpeciesCatalog faunaCatalog)
    {
        var floraSupport = MathF.Min(100.0f, (region.Ecosystem.FloraPopulations.Values.Sum() / MigrationConstants.FloraSupportScale) * 100.0f);
        var faunaSupport = MathF.Min(100.0f, (region.Ecosystem.FaunaPopulations.Values.Sum() / MigrationConstants.FaunaSupportScale) * 100.0f);
        var waterSupport = region.WaterAvailability switch
        {
            WaterAvailability.Low => 25.0f,
            WaterAvailability.Medium => 60.0f,
            WaterAvailability.High => 100.0f,
            _ => 50.0f
        };
        var threatPenalty = MathF.Min(100.0f, (GetCarnivoreThreat(region, faunaCatalog) / MigrationConstants.ThreatSupportScale) * 100.0f);

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

        var score = floraSupport * floraWeight +
                    faunaSupport * faunaWeight +
                    waterSupport * waterWeight +
                    (100.0f - threatPenalty) * threatWeight;

        score += group.KnownRegionIds.Contains(region.Id)
            ? MigrationConstants.KnownRegionBonus
            : -MigrationConstants.UnknownRegionPenalty;

        if (!string.IsNullOrWhiteSpace(group.LastRegionId) && string.Equals(group.LastRegionId, region.Id, StringComparison.Ordinal))
        {
            score -= MigrationConstants.ReturnToLastRegionPenalty;
        }

        return score;
    }

    private static int GetCarnivoreThreat(Region region, FaunaSpeciesCatalog faunaCatalog)
    {
        var threat = 0;

        foreach (var fauna in region.Ecosystem.FaunaPopulations)
        {
            var species = faunaCatalog.GetById(fauna.Key);
            if (species?.DietCategory == DietCategory.Carnivore)
            {
                threat += fauna.Value;
            }
        }

        return threat;
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
        bool consideredMigration,
        float currentScore,
        CandidateScore? bestNeighbor,
        GroupSurvivalChange? survivalChange)
    {
        if (!consideredMigration)
        {
            return $"Stayed because migration pressure {group.Pressures.MigrationPressure} was below the trigger and survival this month was tolerable.";
        }

        if (bestNeighbor is null)
        {
            return "Stayed because no neighboring region was available to evaluate.";
        }

        if (!string.IsNullOrWhiteSpace(group.LastRegionId) && string.Equals(group.LastRegionId, bestNeighbor.Region.Id, StringComparison.Ordinal))
        {
            return $"Stayed because returning to {bestNeighbor.Region.Name} was penalized by anti-thrashing and did not beat the current region enough.";
        }

        if (survivalChange is not null && survivalChange.Shortage > 0)
        {
            return $"Stayed despite shortage {survivalChange.Shortage} because the best neighbor scored only {bestNeighbor.Score:0.0} versus current {currentScore:0.0}.";
        }

        return $"Stayed because no neighboring region was sufficiently better than the current score {currentScore:0.0}.";
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
            KnownDiscoveryIds = new HashSet<string>(group.KnownDiscoveryIds, StringComparer.Ordinal)
        };
    }

    private sealed record CandidateScore(Region Region, float Score);
}
