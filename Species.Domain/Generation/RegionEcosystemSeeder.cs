using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Generation;

public static class RegionEcosystemSeeder
{
    public static RegionEcosystem Seed(
        Region region,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        Random random)
    {
        var floraPopulations = SeedFlora(region, floraCatalog, random);
        var faunaPopulations = SeedFauna(region, floraPopulations, faunaCatalog, random);
        var floraProfiles = floraPopulations.Keys.ToDictionary(
            speciesId => speciesId,
            speciesId => new RegionalBiologicalProfile
            {
                SpeciesId = speciesId,
                RegionId = region.Id,
                Traits = floraCatalog.GetById(speciesId)?.BaselineTraits.Clone() ?? new BiologicalTraitProfile(),
                LastPopulation = floraPopulations[speciesId],
                ViabilityScore = 50
            },
            StringComparer.Ordinal);
        var faunaProfiles = faunaPopulations.Keys.ToDictionary(
            speciesId => speciesId,
            speciesId => new RegionalBiologicalProfile
            {
                SpeciesId = speciesId,
                RegionId = region.Id,
                Traits = faunaCatalog.GetById(speciesId)?.BaselineTraits.Clone() ?? new BiologicalTraitProfile(),
                LastPopulation = faunaPopulations[speciesId],
                ViabilityScore = 50
            },
            StringComparer.Ordinal);

        return new RegionEcosystem(floraPopulations, faunaPopulations, floraProfiles, faunaProfiles);
    }

    private static IReadOnlyDictionary<string, int> SeedFlora(
        Region region,
        FloraSpeciesCatalog floraCatalog,
        Random random)
    {
        var populations = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var species in floraCatalog.Definitions)
        {
            if (!species.SupportedWaterAvailabilities.Contains(region.WaterAvailability))
            {
                continue;
            }

            var abundance = GetFloraAbundance(region, species, random);
            if (abundance < EcologySeedingConstants.MinimumSeededPopulation)
            {
                continue;
            }

            populations[species.Id] = ToPopulationCount(abundance);
        }

        return populations;
    }

    private static IReadOnlyDictionary<string, int> SeedFauna(
        Region region,
        IReadOnlyDictionary<string, int> floraPopulations,
        FaunaSpeciesCatalog faunaCatalog,
        Random random)
    {
        var populations = new Dictionary<string, int>(StringComparer.Ordinal);
        var floraSupport = floraPopulations.Count == 0
            ? 0.0f
            : ToPopulationSupport(floraPopulations.Values.Average());
        var preySupport = 0.0f;

        foreach (var species in faunaCatalog.Definitions)
        {
            if (!species.SupportedWaterAvailabilities.Contains(region.WaterAvailability))
            {
                continue;
            }

            var abundance = GetFaunaAbundance(region, species, floraSupport, preySupport, random);
            if (abundance < EcologySeedingConstants.MinimumSeededPopulation)
            {
                continue;
            }

            populations[species.Id] = ToPopulationCount(abundance);

            if (species.DietCategory != DietCategory.Carnivore)
            {
                preySupport = Math.Max(preySupport, abundance);
            }
        }

        return populations;
    }

    private static float GetFloraAbundance(
        Region region,
        FloraSpeciesDefinition species,
        Random random)
    {
        var biomeMultiplier = species.CoreBiomes.Contains(region.Biome)
            ? 1.0f
            : EcologySeedingConstants.FloraNonCoreBiomeMultiplier;
        var fertilityFit = GetFertilityFit((float)region.Fertility, species.PreferredFertilityMin, species.PreferredFertilityMax);
        var variance = 1.0f + NextSignedVariance(random, EcologySeedingConstants.FloraRandomVarianceRange);
        var abundance = (species.GrowthRate * 0.55f) +
                        (species.FoodValue * 0.10f) +
                        (fertilityFit * 0.35f) +
                        (species.CoreBiomes.Contains(region.Biome) ? EcologySeedingConstants.FloraBiomeMatchBonus : 0.0f);

        abundance *= EcologySeedingConstants.FloraWaterSupportMultiplier;
        abundance *= biomeMultiplier;
        abundance *= variance;

        return ClampNormalized(abundance);
    }

    private static float GetFaunaAbundance(
        Region region,
        FaunaSpeciesDefinition species,
        float floraSupport,
        float preySupport,
        Random random)
    {
        var biomeMultiplier = species.CoreBiomes.Contains(region.Biome)
            ? 1.0f
            : EcologySeedingConstants.FaunaNonCoreBiomeMultiplier;
        var support = species.DietCategory switch
        {
            DietCategory.Herbivore => floraSupport * EcologySeedingConstants.HerbivoreFloraSupportMultiplier,
            DietCategory.Omnivore => (floraSupport * EcologySeedingConstants.OmnivoreFloraSupportMultiplier) +
                                     (preySupport * EcologySeedingConstants.OmnivorePreySupportMultiplier),
            DietCategory.Carnivore => preySupport * EcologySeedingConstants.CarnivorePreySupportMultiplier,
            _ => 0.0f
        };

        if (species.DietCategory == DietCategory.Herbivore && floraSupport < EcologySeedingConstants.WeakFloraSupportThreshold)
        {
            support *= 0.25f;
        }

        if (species.DietCategory == DietCategory.Carnivore && preySupport < EcologySeedingConstants.WeakPreySupportThreshold)
        {
            support *= 0.10f;
        }

        var variance = 1.0f + NextSignedVariance(random, EcologySeedingConstants.FaunaRandomVarianceRange);
        var abundance = (support * 0.60f) +
                        (species.ReproductionRate * 0.15f) +
                        ((1.0f - species.FoodRequirement) * 0.10f) +
                        ((1.0f - species.MigrationTendency) * 0.05f) +
                        (species.CoreBiomes.Contains(region.Biome) ? EcologySeedingConstants.FaunaBiomeMatchBonus : 0.0f);

        abundance *= EcologySeedingConstants.FaunaWaterSupportMultiplier;
        abundance *= biomeMultiplier;
        abundance *= variance;

        return ClampNormalized(abundance);
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

        return ClampNormalized(1.0f - (distance / 0.40f));
    }

    private static float NextSignedVariance(Random random, float range)
    {
        return ((float)random.NextDouble() * range * 2.0f) - range;
    }

    private static int ToPopulationCount(float value)
    {
        return (int)MathF.Round(value * EcologySeedingConstants.PopulationScale, MidpointRounding.AwayFromZero);
    }

    private static float ToPopulationSupport(double population)
    {
        return (float)(population / EcologySeedingConstants.PopulationScale);
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
