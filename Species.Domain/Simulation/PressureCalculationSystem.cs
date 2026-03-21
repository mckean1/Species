using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
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
                updatedGroups.Add(CloneGroup(group, group.Pressures.Clone()));
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
            var foodContribution = CalculateFoodPressure(group, visibleFoodSupport);
            var waterContribution = CalculateWaterPressure(regionKnowledge.WaterSupport);
            var threatContribution = CalculateThreatPressure(regionKnowledge.ThreatPressure);
            var overcrowdingContribution = CalculateOvercrowdingPressure(group.Population, weightedFoodPotential, monthlyFoodNeed);

            if (group.LearnedAdvancementIds.Contains(AdvancementCatalog.StrongerShelterId))
            {
                threatContribution = ClampPressure(threatContribution * AdvancementConstants.StrongerShelterThreatMultiplier);
                overcrowdingContribution = ClampPressure(overcrowdingContribution * AdvancementConstants.StrongerShelterCrowdingMultiplier);
            }

            var migrationContribution = CalculateMigrationPressure(foodContribution, waterContribution, threatContribution, overcrowdingContribution);
            var foodDetail = BuildPressureDetail(
                PressureDefinitions.Food,
                group.Pressures.Food,
                foodContribution,
                BuildFoodPressureReason(group, regionKnowledge, visibleFoodSupport, foodContribution));
            var waterDetail = BuildPressureDetail(
                PressureDefinitions.Water,
                group.Pressures.Water,
                waterContribution,
                BuildWaterPressureReason(regionKnowledge, waterContribution));
            var threatDetail = BuildPressureDetail(
                PressureDefinitions.Threat,
                group.Pressures.Threat,
                threatContribution,
                BuildThreatPressureReason(regionKnowledge, threatContribution));
            var overcrowdingDetail = BuildPressureDetail(
                PressureDefinitions.Overcrowding,
                group.Pressures.Overcrowding,
                overcrowdingContribution,
                BuildOvercrowdingReason(group.Population, weightedFoodPotential, monthlyFoodNeed, overcrowdingContribution));
            var migrationDetail = BuildPressureDetail(
                PressureDefinitions.Migration,
                group.Pressures.Migration,
                migrationContribution,
                BuildMigrationReason(foodContribution, waterContribution, threatContribution, overcrowdingContribution, migrationContribution));

            var pressures = new PressureState
            {
                Food = ToPressureValue(foodDetail),
                Water = ToPressureValue(waterDetail),
                Threat = ToPressureValue(threatDetail),
                Overcrowding = ToPressureValue(overcrowdingDetail),
                Migration = ToPressureValue(migrationDetail)
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
                Food = foodDetail,
                Water = waterDetail,
                Threat = threatDetail,
                Overcrowding = overcrowdingDetail,
                Migration = migrationDetail
            });
        }

        return new PressureCalculationResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, world.Polities, world.FocalPolityId),
            changes);
    }

    private static PopulationGroup CloneGroup(PopulationGroup group, PressureState pressures)
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
            LastRegionId = group.LastRegionId,
            MonthsSinceLastMove = group.MonthsSinceLastMove,
            KnownRegionIds = new HashSet<string>(group.KnownRegionIds, StringComparer.Ordinal),
            KnownDiscoveryIds = new HashSet<string>(group.KnownDiscoveryIds, StringComparer.Ordinal),
            DiscoveryEvidence = group.DiscoveryEvidence.Clone(),
            LearnedAdvancementIds = new HashSet<string>(group.LearnedAdvancementIds, StringComparer.Ordinal),
            AdvancementEvidence = group.AdvancementEvidence.Clone(),
            Pressures = pressures.Clone()
        };
    }

    private static int CalculateFoodPressure(PopulationGroup group, int visibleFoodSupport)
    {
        var foodReserveMonths = group.Population == 0
            ? PressureCalculationConstants.StoredFoodMonthsSafe
            : (float)group.StoredFood / Math.Max(1, group.Population);
        var storedFoodSafety = MathF.Min(1.0f, foodReserveMonths / PressureCalculationConstants.StoredFoodMonthsSafe);
        var ecologySupport = visibleFoodSupport / PressureCalculationConstants.PressureScaleMaximum;
        var hungerCarryover = group.HungerPressure * 25.0f;
        var stressBonus = group.FoodStressState switch
        {
            FoodStressState.HungerPressure => 8.0f,
            FoodStressState.SevereShortage => 18.0f,
            FoodStressState.Starvation => 32.0f,
            _ => 0.0f
        };

        var pressure = (1.0f - storedFoodSafety) * 45.0f +
                       (1.0f - ecologySupport) * 55.0f +
                       hungerCarryover +
                       stressBonus;

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
            return $"High because stored food is thin, only {DescribeKnowledge(knowledge.OverallKnowledge)} food support is visible, and visible food is not guaranteed to be usable food.";
        }

        if (pressure >= 40)
        {
            return $"Moderate because the polity can account for about {visibleFoodSupport}% of current food need from what it knows, before actor-specific usable access losses.";
        }

        return $"Low because stored food and known local support look manageable, and recent usable-food stress is limited.";
    }

    private static string BuildWaterPressureReason(RegionKnowledgeSnapshot knowledge, int contribution)
    {
        var source = knowledge.WaterKnowledge switch
        {
            KnowledgeLevel.Knowledge => "Derived from established local water knowledge.",
            KnowledgeLevel.Discovery => "Derived from discovered local water patterns.",
            KnowledgeLevel.Encounter => "Derived from recent encounters and route exposure rather than established water knowledge.",
            _ => "Derived from uncertain local water knowledge."
        };

        return contribution >= 70
            ? $"{source} Monthly strain is currently severe."
            : source;
    }

    private static string BuildThreatPressureReason(RegionKnowledgeSnapshot knowledge, int contribution)
    {
        return contribution >= 60
            ? $"Elevated because {DescribeKnowledge(knowledge.FaunaKnowledge)} local danger signs are unfavorable."
            : $"Derived from {DescribeKnowledge(knowledge.FaunaKnowledge)} local threat knowledge.";
    }

    private static string BuildOvercrowdingReason(int population, float weightedFoodPotential, int monthlyFoodNeed, int contribution)
    {
        var supportMonths = monthlyFoodNeed <= 0 ? 0.0f : weightedFoodPotential / monthlyFoodNeed;
        return contribution >= 60
            ? $"High because population {population} is pressing against about {supportMonths:0.0} months of visible local support."
            : $"Derived from population {population} versus visible local carrying support.";
    }

    private static string BuildMigrationReason(int foodPressure, int waterPressure, int threatPressure, int overcrowdingPressure, int migrationPressure)
    {
        return migrationPressure >= 60
            ? $"Elevated because food={foodPressure}, water={waterPressure}, threat={threatPressure}, overcrowding={overcrowdingPressure}."
            : $"Synthesized from food={foodPressure}, water={waterPressure}, threat={threatPressure}, overcrowding={overcrowdingPressure}.";
    }

    private static PressureChangeDetail BuildPressureDetail(
        PressureDefinition definition,
        PressureValue prior,
        int monthlyContribution,
        string reasonText)
    {
        var afterContribution = prior.RawValue + monthlyContribution;
        var decayedRaw = ApplyDecay(afterContribution, definition);
        var boundedRaw = PressureMath.ApplySafetyBounds(decayedRaw, definition);
        var finalValue = PressureMath.CreateValue(definition, boundedRaw);
        return new PressureChangeDetail
        {
            PriorRaw = prior.RawValue,
            MonthlyContribution = monthlyContribution,
            DecayApplied = afterContribution - decayedRaw,
            FinalRaw = finalValue.RawValue,
            Effective = finalValue.EffectiveValue,
            Display = finalValue.DisplayValue,
            SeverityLabel = finalValue.SeverityLabel,
            ReasonText = reasonText
        };
    }

    private static int ApplyDecay(int rawValue, PressureDefinition definition)
    {
        return definition.DecayMode switch
        {
            PressureDecayMode.PassiveTowardZero => PressureMath.MoveTowardZero(rawValue, definition.DecayRate),
            _ => rawValue
        };
    }

    private static PressureValue ToPressureValue(PressureChangeDetail detail)
    {
        return new PressureValue
        {
            RawValue = detail.FinalRaw,
            EffectiveValue = detail.Effective,
            DisplayValue = detail.Display,
            SeverityLabel = detail.SeverityLabel
        };
    }

    private static int ClampPressure(float value)
    {
        return (int)MathF.Round(Math.Clamp(value, 0.0f, PressureCalculationConstants.PressureScaleMaximum), MidpointRounding.AwayFromZero);
    }

    private static string DescribeKnowledge(KnowledgeLevel level)
    {
        return level switch
        {
            KnowledgeLevel.Knowledge => "known",
            KnowledgeLevel.Discovery => "discovered",
            KnowledgeLevel.Encounter => "encountered",
            _ => "uncertain"
        };
    }
}
