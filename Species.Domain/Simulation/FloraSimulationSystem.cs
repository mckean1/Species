using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class FloraSimulationSystem
{
    public FloraSimulationResult Run(World world, FloraSpeciesCatalog floraCatalog)
    {
        var updatedRegions = new List<Region>(world.Regions.Count);
        var changes = new List<FloraPopulationChange>();

        foreach (var region in world.Regions)
        {
            var updatedFlora = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var floraEntry in region.Ecosystem.FloraPopulations.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                var species = floraCatalog.GetById(floraEntry.Key);
                if (species is null)
                {
                    continue;
                }

                var waterSupported = species.SupportedWaterAvailabilities.Contains(region.WaterAvailability);
                var coreBiomeFit = species.CoreBiomes.Contains(region.Biome);
                var fertilityFit = GetFertilityFit((float)region.Fertility, species.PreferredFertilityMin, species.PreferredFertilityMax);
                var targetPopulation = GetTargetPopulation(species, waterSupported, coreBiomeFit, fertilityFit);
                var newPopulation = MoveTowardTarget(floraEntry.Value, targetPopulation, species.GrowthRate, waterSupported);

                if (newPopulation > 0)
                {
                    updatedFlora[floraEntry.Key] = newPopulation;
                }

                changes.Add(new FloraPopulationChange
                {
                    RegionId = region.Id,
                    RegionName = region.Name,
                    FloraSpeciesId = species.Id,
                    FloraSpeciesName = species.Name,
                    PreviousPopulation = floraEntry.Value,
                    TargetPopulation = targetPopulation,
                    NewPopulation = newPopulation,
                    Outcome = ResolveOutcome(floraEntry.Value, newPopulation),
                    WaterSupported = waterSupported,
                    CoreBiomeFit = coreBiomeFit,
                    FertilityFit = fertilityFit,
                    PrimaryCause = ResolvePrimaryCause(waterSupported, coreBiomeFit, fertilityFit, floraEntry.Value, targetPopulation, newPopulation)
                });
            }

            var updatedRegion = new Region(
                region.Id,
                region.Name,
                region.Fertility,
                region.Biome,
                region.WaterAvailability,
                region.NeighborIds,
                new RegionEcosystem(updatedFlora, new Dictionary<string, int>(region.Ecosystem.FaunaPopulations, StringComparer.Ordinal)));

            updatedRegions.Add(updatedRegion);
        }

        return new FloraSimulationResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, updatedRegions, world.PopulationGroups),
            changes);
    }

    private static int GetTargetPopulation(
        FloraSpeciesDefinition species,
        bool waterSupported,
        bool coreBiomeFit,
        float fertilityFit)
    {
        if (!waterSupported)
        {
            return 0;
        }

        var biomeFitMultiplier = coreBiomeFit
            ? FloraSimulationConstants.CoreBiomeFitMultiplier
            : FloraSimulationConstants.NonCoreBiomeFitMultiplier;
        var targetNormalized =
            FloraSimulationConstants.BaseTargetContribution +
            (species.GrowthRate * FloraSimulationConstants.GrowthRateTargetWeight) +
            (species.FoodValue * FloraSimulationConstants.FoodValueTargetWeight) +
            (fertilityFit * FloraSimulationConstants.FertilityTargetWeight) +
            (coreBiomeFit ? FloraSimulationConstants.CoreBiomeTargetBonus : 0.0f);

        targetNormalized *= biomeFitMultiplier;

        return ToPopulationCount(ClampNormalized(targetNormalized));
    }

    private static int MoveTowardTarget(
        int currentPopulation,
        int targetPopulation,
        float growthRate,
        bool waterSupported)
    {
        var adjustmentRate = waterSupported
            ? MathF.Max(FloraSimulationConstants.MinimumMonthlyAdjustmentRate, growthRate * FloraSimulationConstants.GrowthRateAdjustmentWeight)
            : FloraSimulationConstants.UnsupportedWaterDeclineRate;
        var updatedValue = currentPopulation + ((targetPopulation - currentPopulation) * adjustmentRate);
        var nextPopulation = Math.Max(0, (int)MathF.Round(updatedValue, MidpointRounding.AwayFromZero));

        if (targetPopulation == 0 && nextPopulation <= FloraSimulationConstants.ExtinctionThresholdPopulation)
        {
            return 0;
        }

        return nextPopulation;
    }

    private static float GetFertilityFit(float fertility, float preferredMin, float preferredMax)
    {
        if (fertility >= preferredMin && fertility <= preferredMax)
        {
            return 1.0f;
        }

        var distance = fertility < preferredMin
            ? preferredMin - fertility
            : fertility - preferredMax;

        return ClampNormalized(1.0f - (distance / FloraSimulationConstants.FertilityFitFalloffRange));
    }

    private static string ResolveOutcome(int previousPopulation, int newPopulation)
    {
        if (newPopulation == 0 && previousPopulation > 0)
        {
            return "WentExtinct";
        }

        if (newPopulation > previousPopulation)
        {
            return "Grew";
        }

        if (newPopulation < previousPopulation)
        {
            return "Declined";
        }

        return "Stabilized";
    }

    private static string ResolvePrimaryCause(
        bool waterSupported,
        bool coreBiomeFit,
        float fertilityFit,
        int previousPopulation,
        int targetPopulation,
        int newPopulation)
    {
        if (!waterSupported)
        {
            return "Unsupported water";
        }

        if (newPopulation == 0 && previousPopulation > 0)
        {
            return "Regional extinction";
        }

        if (!coreBiomeFit)
        {
            return "Non-core biome";
        }

        if (fertilityFit < 0.40f)
        {
            return "Fertility mismatch";
        }

        if (targetPopulation > previousPopulation)
        {
            return "Strong habitat fit";
        }

        if (targetPopulation < previousPopulation)
        {
            return "Below habitat target";
        }

        return "Near habitat target";
    }

    private static int ToPopulationCount(float value)
    {
        return (int)MathF.Round(value * EcologySeedingConstants.PopulationScale, MidpointRounding.AwayFromZero);
    }

    private static float ClampNormalized(float value)
    {
        return MathF.Round(
            Math.Clamp(
                value,
                SpeciesDefinitionConstants.NormalizedMinimum,
                SpeciesDefinitionConstants.NormalizedMaximum),
            2);
    }
}
