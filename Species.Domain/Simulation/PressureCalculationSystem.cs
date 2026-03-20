using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Knowledge;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class PressureCalculationSystem
{
    public PressureCalculationResult Run(
        World world,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var updatedGroups = new List<PopulationGroup>(world.PopulationGroups.Count);
        var changes = new List<GroupPressureChange>(world.PopulationGroups.Count);

        foreach (var group in world.PopulationGroups)
        {
            if (!regionsById.TryGetValue(group.CurrentRegionId, out var region))
            {
                updatedGroups.Add(CloneGroup(group, new PressureState()));
                continue;
            }

            var knowledgeContext = GroupKnowledgeContext.Create(world, group, discoveryCatalog, floraCatalog, faunaCatalog);
            var regionKnowledge = knowledgeContext.ObserveRegion(region, group.CurrentRegionId);
            var monthlyFoodNeed = SubsistenceSupportModel.CalculateMonthlyFoodNeed(group.Population);
            var weightedFoodPotential = SubsistenceSupportModel.CalculateWeightedFoodPotential(
                group,
                regionKnowledge.GatheringPotentialFood,
                regionKnowledge.HuntingPotentialFood);
            var visibleFoodSupport = SubsistenceSupportModel.NormalizeFoodSupport(weightedFoodPotential, monthlyFoodNeed);
            var foodPressure = CalculateFoodPressure(group, visibleFoodSupport);
            var waterPressure = CalculateWaterPressure(regionKnowledge.WaterSupport);
            var threatPressure = CalculateThreatPressure(regionKnowledge.ThreatPressure);
            var overcrowdingPressure = CalculateOvercrowdingPressure(group.Population, weightedFoodPotential, monthlyFoodNeed);
            var migrationPressure = CalculateMigrationPressure(foodPressure, waterPressure, threatPressure, overcrowdingPressure);

            var pressures = new PressureState
            {
                FoodPressure = foodPressure,
                WaterPressure = waterPressure,
                ThreatPressure = threatPressure,
                OvercrowdingPressure = overcrowdingPressure,
                MigrationPressure = migrationPressure
            };

            updatedGroups.Add(CloneGroup(group, pressures));
            changes.Add(new GroupPressureChange
            {
                GroupId = group.Id,
                GroupName = group.Name,
                CurrentRegionId = region.Id,
                CurrentRegionName = region.Name,
                Population = group.Population,
                StoredFood = group.StoredFood,
                Pressures = pressures,
                FoodPressureReason = BuildFoodPressureReason(group, regionKnowledge, visibleFoodSupport, foodPressure),
                WaterPressureReason = BuildWaterPressureReason(regionKnowledge),
                ThreatPressureReason = BuildThreatPressureReason(regionKnowledge, threatPressure),
                OvercrowdingPressureReason = BuildOvercrowdingReason(group.Population, weightedFoodPotential, monthlyFoodNeed, overcrowdingPressure),
                MigrationPressureReason = BuildMigrationReason(foodPressure, waterPressure, threatPressure, overcrowdingPressure, migrationPressure)
            });
        }

        return new PressureCalculationResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle),
            changes);
    }

    private static PopulationGroup CloneGroup(PopulationGroup group, PressureState pressures)
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
            PoliticalBlocs = group.PoliticalBlocs.Select(bloc => bloc.Clone()).ToList(),
            Pressures = pressures
        };
    }

    private static int CalculateFoodPressure(PopulationGroup group, int visibleFoodSupport)
    {
        var foodReserveMonths = group.Population == 0
            ? PressureCalculationConstants.StoredFoodMonthsSafe
            : (float)group.StoredFood / Math.Max(1, group.Population);
        var storedFoodSafety = MathF.Min(1.0f, foodReserveMonths / PressureCalculationConstants.StoredFoodMonthsSafe);
        var ecologySupport = visibleFoodSupport / PressureCalculationConstants.PressureScaleMaximum;

        var pressure = (1.0f - storedFoodSafety) * 45.0f +
                       (1.0f - ecologySupport) * 55.0f;

        return ClampPressure(pressure);
    }

    private static int CalculateWaterPressure(float waterSupport)
    {
        return ClampPressure(100.0f - waterSupport);
    }

    private static int CalculateThreatPressure(float threatPressure)
    {
        return ClampPressure(threatPressure);
    }

    private static int CalculateOvercrowdingPressure(int population, float weightedFoodPotential, int monthlyFoodNeed)
    {
        if (population <= 0)
        {
            return 0;
        }

        if (weightedFoodPotential <= 0 || monthlyFoodNeed <= 0)
        {
            return 100;
        }

        var supportCapacity = weightedFoodPotential / monthlyFoodNeed;
        var ratio = population / Math.Max(1.0f, supportCapacity * population * PressureCalculationConstants.OvercrowdingSupportScale);
        return ClampPressure(MathF.Max(0.0f, (ratio - 0.5f) * 100.0f));
    }

    private static int CalculateMigrationPressure(int foodPressure, int waterPressure, int threatPressure, int overcrowdingPressure)
    {
        var pressure = foodPressure * PressureCalculationConstants.MigrationFoodWeight +
                       waterPressure * PressureCalculationConstants.MigrationWaterWeight +
                       threatPressure * PressureCalculationConstants.MigrationThreatWeight +
                       overcrowdingPressure * PressureCalculationConstants.MigrationOvercrowdingWeight;

        return ClampPressure(pressure);
    }

    private static string BuildFoodPressureReason(PopulationGroup group, RegionKnowledgeSnapshot knowledge, int visibleFoodSupport, int pressure)
    {
        if (pressure >= 70)
        {
            return $"High because stored food is thin and only {DescribeKnowledge(knowledge.OverallKnowledge)} food support is visible.";
        }

        if (pressure >= 40)
        {
            return $"Moderate because the polity can account for about {visibleFoodSupport}% of current food need from what it knows.";
        }

        return $"Low because stored food and known local support look manageable.";
    }

    private static string BuildWaterPressureReason(RegionKnowledgeSnapshot knowledge)
    {
        return knowledge.WaterKnowledge switch
        {
            KnowledgeLevel.Known => "Derived from known local water sources.",
            KnowledgeLevel.Partial => "Derived from partial water observations in the current region.",
            KnowledgeLevel.Rumored => "Derived from rumors and route familiarity rather than confirmed water knowledge.",
            _ => "Derived from uncertain local water knowledge."
        };
    }

    private static string BuildThreatPressureReason(RegionKnowledgeSnapshot knowledge, int pressure)
    {
        return pressure >= 60
            ? $"Elevated because {DescribeKnowledge(knowledge.FaunaKnowledge)} local danger signs are unfavorable."
            : $"Derived from {DescribeKnowledge(knowledge.FaunaKnowledge)} local threat knowledge.";
    }

    private static string BuildOvercrowdingReason(int population, float weightedFoodPotential, int monthlyFoodNeed, int pressure)
    {
        var supportMonths = monthlyFoodNeed <= 0 ? 0.0f : weightedFoodPotential / monthlyFoodNeed;
        return pressure >= 60
            ? $"High because population {population} is pressing against about {supportMonths:0.0} months of visible local support."
            : $"Derived from population {population} versus visible local carrying support.";
    }

    private static string BuildMigrationReason(int foodPressure, int waterPressure, int threatPressure, int overcrowdingPressure, int migrationPressure)
    {
        return migrationPressure >= 60
            ? $"Elevated because food={foodPressure}, water={waterPressure}, threat={threatPressure}, overcrowding={overcrowdingPressure}."
            : $"Synthesized from food={foodPressure}, water={waterPressure}, threat={threatPressure}, overcrowding={overcrowdingPressure}.";
    }

    private static int ClampPressure(float value)
    {
        return (int)MathF.Round(Math.Clamp(value, 0.0f, PressureCalculationConstants.PressureScaleMaximum), MidpointRounding.AwayFromZero);
    }

    private static string DescribeKnowledge(KnowledgeLevel level)
    {
        return level switch
        {
            KnowledgeLevel.Known => "known",
            KnowledgeLevel.Partial => "partially known",
            KnowledgeLevel.Rumored => "rumored",
            _ => "uncertain"
        };
    }
}
