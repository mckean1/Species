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

            total += flora.Value * ResolveFoodPerUnit(ResolveFloraFoodPerPopulation(region, species), multiplier);
        }

        return total;
    }

    public static float CalculateFloraEcologySupport(Region region, FloraSpeciesCatalog floraCatalog)
    {
        // Ecology support is live flora under current local conditions, not a generic "plant food exists" shortcut.
        var total = 0.0f;

        foreach (var flora in region.Ecosystem.FloraPopulations)
        {
            var species = floraCatalog.GetById(flora.Key);
            if (species is null || flora.Value <= 0)
            {
                continue;
            }

            total += flora.Value * ResolveFloraSupportPerPopulation(region, species);
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

    public static float ResolveFloraFoodPerPopulation(Region region, FloraSpeciesDefinition species)
    {
        var localFit = ResolveFloraEnvironmentalFit(region, species);
        return ((species.UsableBiomass * 0.82f) + (species.RegionalAbundance * 0.18f)) *
               (0.55f + (localFit * 0.45f));
    }

    public static float ResolveFloraSupportPerPopulation(Region region, FloraSpeciesDefinition species)
    {
        var localFit = ResolveFloraEnvironmentalFit(region, species);
        return ((species.UsableBiomass * 0.40f) +
                (species.RegionalAbundance * 0.22f) +
                (species.RecoveryRate * 0.20f) +
                (species.ConsumptionResilience * 0.18f)) *
               (0.45f + (localFit * 0.55f));
    }

    public static float ResolveFloraEnvironmentalFit(Region region, FloraSpeciesDefinition species)
    {
        if (!species.SupportedWaterAvailabilities.Contains(region.WaterAvailability))
        {
            return 0.0f;
        }

        var fertilityFit = ResolveFertilityFit((float)region.Fertility, species.HabitatFertilityMin, species.HabitatFertilityMax);
        var biomeFit = species.CoreBiomes.Contains(region.Biome) ? 1.0f : 0.42f;
        var waterFit = region.WaterAvailability switch
        {
            WaterAvailability.High => species.SupportedWaterAvailabilities.Contains(WaterAvailability.High) ? 1.00f : 0.74f,
            WaterAvailability.Medium => species.SupportedWaterAvailabilities.Contains(WaterAvailability.Medium) ? 0.92f : 0.64f,
            _ => species.SupportedWaterAvailabilities.Contains(WaterAvailability.Low) ? 0.86f : 0.28f
        };

        return Math.Clamp((fertilityFit * 0.40f) + (waterFit * 0.32f) + (biomeFit * 0.28f), 0.0f, 1.0f);
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

    private static float ResolveFertilityFit(float fertility, float preferredMin, float preferredMax)
    {
        if (fertility >= preferredMin && fertility <= preferredMax)
        {
            return 1.0f;
        }

        var distance = fertility < preferredMin
            ? preferredMin - fertility
            : fertility - preferredMax;

        return Math.Clamp(1.0f - (distance / 0.40f), 0.0f, 1.0f);
    }
}
