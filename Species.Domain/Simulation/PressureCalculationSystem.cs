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
            var foodContribution = CalculateFoodPressure(group);
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
                StartingFoodStores = group.FoodAccounting.StartingTotalStores,
                EndingFoodStores = group.FoodAccounting.EndingTotalStores,
                FoodInflow = group.FoodAccounting.FoodInflow,
                FoodConsumption = group.FoodAccounting.FoodConsumption,
                FoodLosses = group.FoodAccounting.FoodLosses,
                NetFoodChange = group.FoodAccounting.NetFoodChange,
                UnresolvedFoodDeficit = group.FoodAccounting.UnresolvedDeficit,
                FinalFoodCondition = group.FoodAccounting.FoodStressState.ToString(),
                VisibleFoodSupport = visibleFoodSupport,
                VisibleWaterSupport = regionKnowledge.WaterSupport,
                WaterKnowledgeLevel = regionKnowledge.WaterKnowledge.ToString(),
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
            FoodAccounting = group.FoodAccounting.Clone(),
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

    private static int CalculateFoodPressure(PopulationGroup group)
    {
        var accounting = group.FoodAccounting;
        var monthlyDemand = accounting.MonthlyDemand > 0
            ? accounting.MonthlyDemand
            : SubsistenceSupportModel.CalculateMonthlyFoodNeed(group.Population);
        var endingStores = accounting.EndingTotalStores > 0 ? accounting.EndingTotalStores : group.StoredFood;
        var foodReserveMonths = monthlyDemand <= 0
            ? PressureCalculationConstants.StoredFoodMonthsSafe
            : endingStores / (float)Math.Max(1, monthlyDemand);
        var storedFoodSafety = MathF.Min(1.0f, foodReserveMonths / PressureCalculationConstants.StoredFoodMonthsSafe);
        var deficitRatio = monthlyDemand <= 0
            ? 0.0f
            : Math.Clamp(accounting.UnresolvedDeficit / (float)Math.Max(1, monthlyDemand), 0.0f, 1.5f);
        var usableCoverage = monthlyDemand <= 0
            ? 1.0f
            : Math.Clamp(accounting.UsableFoodConsumed / (float)Math.Max(1, monthlyDemand), 0.0f, 1.0f);
        var hungerCarryover = group.HungerPressure * PressureCalculationConstants.FoodHungerCarryoverWeight;
        var stressBonus = group.FoodStressState switch
        {
            FoodStressState.HungerPressure => 6.0f,
            FoodStressState.SevereShortage => 16.0f,
            FoodStressState.Starvation => 28.0f,
            _ => 0.0f
        };

        var pressure = (1.0f - storedFoodSafety) * PressureCalculationConstants.FoodReserveWeight +
                       deficitRatio * PressureCalculationConstants.FoodEcologyWeight +
                       (1.0f - usableCoverage) * PressureCalculationConstants.FoodShortageMonthWeight * 4.0f +
                       (group.ShortageMonths * PressureCalculationConstants.FoodShortageMonthWeight) +
                       hungerCarryover +
                       stressBonus -
                       PressureCalculationConstants.FoodMonthlyRelief;

        if (accounting.NetFoodChange > 0 && accounting.UnresolvedDeficit == 0)
        {
            pressure -= Math.Min(10.0f, accounting.NetFoodChange / (float)Math.Max(1, monthlyDemand) * 12.0f);
        }

        return ClampPressure(MathF.Max(0.0f, pressure));
    }

    private static int CalculateWaterPressure(float waterSupport)
    {
        var shortfall = Math.Max(0.0f, PressureCalculationConstants.WaterStableSupportThreshold - waterSupport);
        var criticalShortfall = Math.Max(0.0f, PressureCalculationConstants.WaterCriticalSupportThreshold - waterSupport);
        var strain = (shortfall * PressureCalculationConstants.WaterScarcityWeight) +
                     (criticalShortfall * PressureCalculationConstants.WaterCriticalWeight);
        return ComputePressureImpulse(strain, PressureCalculationConstants.WaterMonthlyRelief);
    }

    private static int CalculateThreatPressure(float threatPressure)
    {
        return ComputePressureImpulse(threatPressure, PressureCalculationConstants.ThreatMonthlyRelief);
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
        return ComputePressureImpulse(MathF.Max(0.0f, (ratio - 0.5f) * 100.0f), PressureCalculationConstants.OvercrowdingMonthlyRelief);
    }

    private static int CalculateMigrationPressure(int foodPressure, int waterPressure, int threatPressure, int overcrowdingPressure)
    {
        var pressure = foodPressure * PressureCalculationConstants.MigrationFoodWeight +
                       waterPressure * PressureCalculationConstants.MigrationWaterWeight +
                       threatPressure * PressureCalculationConstants.MigrationThreatWeight +
                       overcrowdingPressure * PressureCalculationConstants.MigrationOvercrowdingWeight;

        return ComputePressureImpulse(pressure, PressureCalculationConstants.MigrationMonthlyRelief);
    }

    private static string BuildFoodPressureReason(PopulationGroup group, RegionKnowledgeSnapshot knowledge, int visibleFoodSupport, int pressure)
    {
        var accounting = group.FoodAccounting;
        if (pressure >= 70)
        {
            return $"High because ending stores are {accounting.EndingTotalStores}, unresolved deficit is {accounting.UnresolvedDeficit}, and the final food state is {accounting.FoodStressState}.";
        }

        if (pressure >= 40)
        {
            return $"Moderate because ending stores are {accounting.EndingTotalStores}, net food changed by {accounting.NetFoodChange:+#;-#;0}, and final coverage remains strained.";
        }

        return $"Low because ending stores are {accounting.EndingTotalStores}, net food changed by {accounting.NetFoodChange:+#;-#;0}, and unresolved deficit is {accounting.UnresolvedDeficit}.";
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

    private static int ComputePressureImpulse(float rawPressure, float monthlyRelief)
    {
        return ClampPressure(MathF.Max(0.0f, rawPressure - monthlyRelief));
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
