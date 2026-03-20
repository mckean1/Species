using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
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
                if (species is null || species.IsExtinct)
                {
                    continue;
                }

                var profile = region.Ecosystem.FloraProfiles.GetValueOrDefault(species.Id);
                var traits = profile?.Traits ?? species.BaselineTraits;
                var waterSupported = species.SupportedWaterAvailabilities.Contains(region.WaterAvailability);
                var coreBiomeFit = species.CoreBiomes.Contains(region.Biome);
                var fertilityFit = GetFertilityFit((float)region.Fertility, species.PreferredFertilityMin, species.PreferredFertilityMax);
                var biologicalFit = ResolveBiologicalFit(region, traits, coreBiomeFit, fertilityFit);
                var targetPopulation = GetTargetPopulation(species, waterSupported, coreBiomeFit, fertilityFit, traits, biologicalFit);
                var newPopulation = MoveTowardTarget(floraEntry.Value, targetPopulation, species.GrowthRate, waterSupported, traits);

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
                    BiologicalFit = biologicalFit,
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
                new RegionEcosystem(
                    updatedFlora,
                    new Dictionary<string, int>(region.Ecosystem.FaunaPopulations, StringComparer.Ordinal),
                    CloneProfiles(region.Ecosystem.FloraProfiles),
                    CloneProfiles(region.Ecosystem.FaunaProfiles),
                    region.Ecosystem.FossilRecords.ToArray(),
                    region.Ecosystem.BiologicalHistoryRecords.ToArray()),
                region.MaterialProfile.Clone());

            updatedRegions.Add(updatedRegion);
        }

        return new FloraSimulationResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, updatedRegions, world.PopulationGroups, world.Chronicle, world.Polities, world.FocalPolityId),
            changes);
    }

    private static int GetTargetPopulation(
        FloraSpeciesDefinition species,
        bool waterSupported,
        bool coreBiomeFit,
        float fertilityFit,
        BiologicalTraitProfile traits,
        float biologicalFit)
    {
        if (!waterSupported)
        {
            return 0;
        }

        var biomeFitMultiplier = coreBiomeFit
            ? FloraSimulationConstants.CoreBiomeFitMultiplier
            : FloraSimulationConstants.NonCoreBiomeFitMultiplier + (traits.Flexibility / 250.0f);
        var targetNormalized =
            FloraSimulationConstants.BaseTargetContribution +
            (species.GrowthRate * FloraSimulationConstants.GrowthRateTargetWeight) +
            (species.FoodValue * FloraSimulationConstants.FoodValueTargetWeight) +
            (fertilityFit * FloraSimulationConstants.FertilityTargetWeight) +
            (coreBiomeFit ? FloraSimulationConstants.CoreBiomeTargetBonus : 0.0f);

        targetNormalized *= biomeFitMultiplier;
        targetNormalized *= biologicalFit;
        targetNormalized *= 0.90f + ((traits.Resilience + traits.Defense) / 400.0f);
        targetNormalized *= 0.92f + (traits.Reproduction / 300.0f);

        return ToPopulationCount(ClampNormalized(targetNormalized));
    }

    private static int MoveTowardTarget(
        int currentPopulation,
        int targetPopulation,
        float growthRate,
        bool waterSupported,
        BiologicalTraitProfile traits)
    {
        var adjustmentRate = waterSupported
            ? MathF.Max(FloraSimulationConstants.MinimumMonthlyAdjustmentRate, growthRate * FloraSimulationConstants.GrowthRateAdjustmentWeight)
            : FloraSimulationConstants.UnsupportedWaterDeclineRate;
        adjustmentRate *= 0.92f + (traits.Reproduction / 260.0f);
        adjustmentRate *= 0.92f + (traits.Resilience / 300.0f);
        var updatedValue = currentPopulation + ((targetPopulation - currentPopulation) * adjustmentRate);
        var nextPopulation = Math.Max(0, (int)MathF.Round(updatedValue, MidpointRounding.AwayFromZero));

        if (targetPopulation == 0 && nextPopulation <= FloraSimulationConstants.ExtinctionThresholdPopulation)
        {
            return 0;
        }

        return nextPopulation;
    }

    private static float ResolveBiologicalFit(Region region, BiologicalTraitProfile traits, bool coreBiomeFit, float fertilityFit)
    {
        var coldDemand = region.Biome switch
        {
            Biome.Tundra => 78,
            Biome.Highlands => 62,
            _ => 34
        };
        var heatDemand = region.Biome switch
        {
            Biome.Desert => 80,
            Biome.Wetlands => 58,
            Biome.Plains => 52,
            _ => 36
        };
        var droughtDemand = region.WaterAvailability switch
        {
            WaterAvailability.Low => 82,
            WaterAvailability.Medium => 46,
            _ => 22
        };

        var coldFit = 1.0f - MathF.Abs(traits.ColdTolerance - coldDemand) / 100.0f;
        var heatFit = 1.0f - MathF.Abs(traits.HeatTolerance - heatDemand) / 100.0f;
        var droughtFit = 1.0f - MathF.Abs(traits.DroughtTolerance - droughtDemand) / 100.0f;
        var flexibilityBonus = traits.Flexibility / 220.0f;
        var sizeTradeoff = 1.0f + ((traits.BodySize - 50) / 250.0f) - ((droughtDemand > 60 ? traits.BodySize : 0) / 500.0f);
        var fit = ((coldFit * 0.20f) + (heatFit * 0.20f) + (droughtFit * 0.25f) + (fertilityFit * 0.20f) +
                   ((coreBiomeFit ? 1.0f : 0.78f) * 0.15f));

        fit += flexibilityBonus;
        fit *= sizeTradeoff;
        return Math.Clamp(fit, 0.35f, 1.35f);
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

    private static Dictionary<string, RegionalBiologicalProfile> CloneProfiles(IReadOnlyDictionary<string, RegionalBiologicalProfile> profiles)
    {
        return profiles.ToDictionary(entry => entry.Key, entry => entry.Value.Clone(), StringComparer.Ordinal);
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
