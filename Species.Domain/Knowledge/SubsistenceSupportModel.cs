using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Knowledge;

public static class SubsistenceSupportModel
{
    public static int CalculateMonthlyFoodNeed(int population)
    {
        return (int)MathF.Ceiling(population * GroupSurvivalConstants.FoodNeedPerPopulationUnit);
    }

    public static float CalculateGatheringPotentialFood(
        Region region,
        PopulationGroup group,
        FloraSpeciesCatalog floraCatalog)
    {
        var multiplier = ResolveGatheringMultiplier(group, region);
        var total = 0.0f;

        foreach (var flora in region.Ecosystem.FloraPopulations)
        {
            var species = floraCatalog.GetById(flora.Key);
            if (species is null || flora.Value <= 0)
            {
                continue;
            }

            total += flora.Value * ResolveFoodPerUnit(ResolveFloraFoodPerPopulation(species), multiplier);
        }

        return total;
    }

    public static float CalculateFloraEcologySupport(Region region, FloraSpeciesCatalog floraCatalog)
    {
        var total = 0.0f;

        foreach (var flora in region.Ecosystem.FloraPopulations)
        {
            var species = floraCatalog.GetById(flora.Key);
            if (species is null || flora.Value <= 0)
            {
                continue;
            }

            total += flora.Value * ResolveFloraSupportPerPopulation(species);
        }

        return total;
    }

    public static float CalculateHuntingPotentialFood(
        Region region,
        PopulationGroup group,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var multiplier = ResolveHuntingMultiplier(group, region);
        var total = 0.0f;

        foreach (var fauna in region.Ecosystem.FaunaPopulations)
        {
            var species = faunaCatalog.GetById(fauna.Key);
            if (species is null || fauna.Value <= 0)
            {
                continue;
            }

            total += fauna.Value * ResolveFoodPerUnit(species.FoodYield, multiplier);
        }

        return total;
    }

    public static float CalculateWeightedFoodPotential(
        PopulationGroup group,
        float gatheringPotentialFood,
        float huntingPotentialFood)
    {
        var (gatherWeight, huntWeight) = group.SubsistenceMode switch
        {
            SubsistenceMode.Gatherer => (0.75f, 0.25f),
            SubsistenceMode.Hunter => (0.25f, 0.75f),
            _ => (0.55f, 0.45f)
        };

        return (gatheringPotentialFood * gatherWeight) + (huntingPotentialFood * huntWeight);
    }

    public static int NormalizeFoodSupport(float weightedPotentialFood, int monthlyFoodNeed)
    {
        if (monthlyFoodNeed <= 0)
        {
            return 100;
        }

        var ratio = weightedPotentialFood / monthlyFoodNeed;
        return ClampScore(ratio * 100.0f);
    }

    public static int ResolveWaterSupport(WaterAvailability waterAvailability)
    {
        return waterAvailability switch
        {
            WaterAvailability.Low => 25,
            WaterAvailability.Medium => 60,
            WaterAvailability.High => 100,
            _ => 50
        };
    }

    public static int CalculateCarnivoreThreat(Region region, FaunaSpeciesCatalog faunaCatalog)
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

    public static int NormalizeThreatPressure(int carnivoreThreat)
    {
        return ClampScore((carnivoreThreat / PressureCalculationConstants.ThreatCarnivoreScale) * PressureCalculationConstants.PressureScaleMaximum);
    }

    public static int ResolveFoodPerUnit(float baseFoodValue, float multiplier)
    {
        return Math.Max(1, (int)MathF.Round(baseFoodValue * GroupSurvivalConstants.FoodUnitScale * multiplier, MidpointRounding.AwayFromZero));
    }

    public static float ResolveFloraFoodPerPopulation(FloraSpeciesDefinition species)
    {
        return (species.UsableBiomass * 0.78f) + (species.RegionalAbundance * 0.22f);
    }

    public static float ResolveFloraSupportPerPopulation(FloraSpeciesDefinition species)
    {
        return (species.UsableBiomass * 0.46f) +
               (species.RegionalAbundance * 0.28f) +
               (species.RecoveryRate * 0.14f) +
               (species.ConsumptionResilience * 0.12f);
    }

    public static float ResolveGatheringMultiplier(PopulationGroup group, Region region)
    {
        var multiplier = 1.0f;

        if (group.LearnedAdvancementIds.Contains(AdvancementCatalog.ImprovedGatheringId))
        {
            multiplier *= AdvancementConstants.ImprovedGatheringMultiplier;
        }

        if (group.LearnedAdvancementIds.Contains(AdvancementCatalog.LocalResourceUseId) &&
            group.KnownDiscoveryIds.Contains($"discovery-local-region-conditions:{region.Id}"))
        {
            multiplier *= AdvancementConstants.LocalResourceUseMultiplier;
        }

        return multiplier;
    }

    public static float ResolveHuntingMultiplier(PopulationGroup group, Region region)
    {
        var multiplier = 1.0f;

        if (group.LearnedAdvancementIds.Contains(AdvancementCatalog.ImprovedHuntingId))
        {
            multiplier *= AdvancementConstants.ImprovedHuntingMultiplier;
        }

        if (group.LearnedAdvancementIds.Contains(AdvancementCatalog.LocalResourceUseId) &&
            group.KnownDiscoveryIds.Contains($"discovery-local-region-conditions:{region.Id}"))
        {
            multiplier *= AdvancementConstants.LocalResourceUseMultiplier;
        }

        return multiplier;
    }

    public static int ClampScore(float value)
    {
        return (int)MathF.Round(Math.Clamp(value, 0.0f, 100.0f), MidpointRounding.AwayFromZero);
    }
}
