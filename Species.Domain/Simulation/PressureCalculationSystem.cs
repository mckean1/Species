using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class PressureCalculationSystem
{
    public PressureCalculationResult Run(World world, FaunaSpeciesCatalog faunaCatalog)
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

            var localFloraSupport = region.Ecosystem.FloraPopulations.Values.Sum();
            var localFaunaSupport = region.Ecosystem.FaunaPopulations.Values.Sum();
            var carnivoreThreat = GetCarnivoreThreat(region, faunaCatalog);

            var foodPressure = CalculateFoodPressure(group, localFloraSupport, localFaunaSupport);
            var waterPressure = CalculateWaterPressure(region.WaterAvailability);
            var threatPressure = CalculateThreatPressure(carnivoreThreat);
            var overcrowdingPressure = CalculateOvercrowdingPressure(group.Population, localFloraSupport + localFaunaSupport);
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
                FoodPressureReason = BuildFoodPressureReason(group, localFloraSupport, localFaunaSupport, foodPressure),
                WaterPressureReason = BuildWaterPressureReason(region.WaterAvailability),
                ThreatPressureReason = BuildThreatPressureReason(carnivoreThreat, threatPressure),
                OvercrowdingPressureReason = BuildOvercrowdingReason(group.Population, localFloraSupport + localFaunaSupport, overcrowdingPressure),
                MigrationPressureReason = BuildMigrationReason(foodPressure, waterPressure, threatPressure, overcrowdingPressure, migrationPressure)
            });
        }

        return new PressureCalculationResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups),
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
            LastRegionId = group.LastRegionId,
            MonthsSinceLastMove = group.MonthsSinceLastMove,
            KnownRegionIds = new HashSet<string>(group.KnownRegionIds, StringComparer.Ordinal),
            KnownDiscoveryIds = new HashSet<string>(group.KnownDiscoveryIds, StringComparer.Ordinal),
            DiscoveryEvidence = group.DiscoveryEvidence.Clone(),
            LearnedAdvancementIds = new HashSet<string>(group.LearnedAdvancementIds, StringComparer.Ordinal),
            AdvancementEvidence = group.AdvancementEvidence.Clone(),
            Pressures = pressures
        };
    }

    private static int CalculateFoodPressure(PopulationGroup group, int localFloraSupport, int localFaunaSupport)
    {
        var foodReserveMonths = group.Population == 0
            ? PressureCalculationConstants.StoredFoodMonthsSafe
            : (float)group.StoredFood / Math.Max(1, group.Population);
        var storedFoodSafety = MathF.Min(1.0f, foodReserveMonths / PressureCalculationConstants.StoredFoodMonthsSafe);
        var localSupport = localFloraSupport + localFaunaSupport;
        var ecologySupport = MathF.Min(1.0f, localSupport / PressureCalculationConstants.LocalFoodSupportScale);
        var populationStrain = localSupport <= 0
            ? 1.0f
            : MathF.Min(1.0f, group.Population / (localSupport * PressureCalculationConstants.OvercrowdingSupportScale));

        var pressure = (1.0f - storedFoodSafety) * 45.0f +
                       (1.0f - ecologySupport) * 35.0f +
                       populationStrain * 20.0f;

        return ClampPressure(pressure);
    }

    private static int CalculateWaterPressure(WaterAvailability waterAvailability)
    {
        return waterAvailability switch
        {
            WaterAvailability.Low => 80,
            WaterAvailability.Medium => 40,
            WaterAvailability.High => 10,
            _ => 50
        };
    }

    private static int CalculateThreatPressure(int carnivoreThreat)
    {
        return ClampPressure((carnivoreThreat / PressureCalculationConstants.ThreatCarnivoreScale) * PressureCalculationConstants.PressureScaleMaximum);
    }

    private static int CalculateOvercrowdingPressure(int population, int localSupport)
    {
        if (population <= 0)
        {
            return 0;
        }

        if (localSupport <= 0)
        {
            return 100;
        }

        var ratio = population / (localSupport * PressureCalculationConstants.OvercrowdingSupportScale);
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

    private static int ClampPressure(float value)
    {
        return (int)MathF.Round(Math.Clamp(value, 0.0f, PressureCalculationConstants.PressureScaleMaximum), MidpointRounding.AwayFromZero);
    }

    private static string BuildFoodPressureReason(PopulationGroup group, int localFloraSupport, int localFaunaSupport, int pressure)
    {
        if (pressure >= 70)
        {
            return $"High because StoredFood is {group.StoredFood} and local food support is {localFloraSupport + localFaunaSupport}.";
        }

        if (pressure >= 40)
        {
            return $"Moderate because population {group.Population} is leaning on local support {localFloraSupport + localFaunaSupport}.";
        }

        return $"Low because StoredFood is {group.StoredFood} and local food support is {localFloraSupport + localFaunaSupport}.";
    }

    private static string BuildWaterPressureReason(WaterAvailability waterAvailability)
    {
        return waterAvailability switch
        {
            WaterAvailability.Low => "High because the region has low water availability.",
            WaterAvailability.Medium => "Moderate because the region has medium water availability.",
            WaterAvailability.High => "Low because the region has high water availability.",
            _ => "Derived from regional water availability."
        };
    }

    private static string BuildThreatPressureReason(int carnivoreThreat, int pressure)
    {
        return pressure >= 60
            ? $"Elevated because carnivore abundance is {carnivoreThreat}."
            : $"Derived from carnivore abundance {carnivoreThreat}.";
    }

    private static string BuildOvercrowdingReason(int population, int localSupport, int pressure)
    {
        return pressure >= 60
            ? $"High because population {population} is large relative to support {localSupport}."
            : $"Derived from population {population} versus support {localSupport}.";
    }

    private static string BuildMigrationReason(int foodPressure, int waterPressure, int threatPressure, int overcrowdingPressure, int migrationPressure)
    {
        return migrationPressure >= 60
            ? $"Elevated because food={foodPressure}, water={waterPressure}, threat={threatPressure}, overcrowding={overcrowdingPressure}."
            : $"Synthesized from food={foodPressure}, water={waterPressure}, threat={threatPressure}, overcrowding={overcrowdingPressure}.";
    }
}
