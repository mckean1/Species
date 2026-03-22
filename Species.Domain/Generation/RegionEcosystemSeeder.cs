using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Discovery;
using Species.Domain.Models;

namespace Species.Domain.Generation;

/// <summary>
/// TRANSITIONAL: This seeder is deprecated in favor of PrimitiveLifeSeeder for world generation.
/// 
/// RegionEcosystemSeeder was the pre-WG-3 approach that seeded all species from catalogs
/// into regions, creating mature multi-species ecosystems at world start.
/// 
/// WG-3 replaces this with PrimitiveLifeSeeder, which seeds only primitive species
/// to establish a minimal organic foothold rather than fabricating mature ecosystems.
/// 
/// This class remains temporarily for reference and potential future use in
/// non-world-generation contexts (e.g., testing, scenario setup).
/// </summary>
public static class RegionEcosystemSeeder
{
    public static RegionEcosystem Seed(
        Region region,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        Random random)
    {
        var floraPopulations = SeedFlora(region, floraCatalog, random);
        var faunaPopulations = SeedFauna(region, floraPopulations, floraCatalog, faunaCatalog, random);
        var protoLifeSubstrate = BuildProtoLifeSubstrate(region, floraPopulations, faunaPopulations);
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

        return new RegionEcosystem(protoLifeSubstrate, floraPopulations, faunaPopulations, floraProfiles, faunaProfiles);
    }

    private static ProtoLifeSubstrate BuildProtoLifeSubstrate(
        Region region,
        IReadOnlyDictionary<string, int> floraPopulations,
        IReadOnlyDictionary<string, int> faunaPopulations)
    {
        var fertility = (float)Math.Clamp(region.Fertility, 0.0, 1.0);
        var waterFactor = region.WaterAvailability switch
        {
            WaterAvailability.High => 1.00f,
            WaterAvailability.Medium => 0.72f,
            _ => 0.42f
        };
        var temperatureFactor = region.TemperatureBand switch
        {
            TemperatureBand.Cold => 0.48f,
            TemperatureBand.Temperate => 1.00f,
            TemperatureBand.Hot => 0.76f,
            _ => 0.72f
        };
        var terrainFactor = region.TerrainRuggedness switch
        {
            TerrainRuggedness.Flat => 1.00f,
            TerrainRuggedness.Rolling => 0.78f,
            TerrainRuggedness.Rugged => 0.54f,
            _ => 0.72f
        };
        var biomeFactor = region.Biome switch
        {
            Biome.Wetlands => 1.00f,
            Biome.Forest => 0.92f,
            Biome.Plains => 0.86f,
            Biome.Highlands => 0.64f,
            Biome.Tundra => 0.38f,
            Biome.Desert => 0.22f,
            _ => 0.60f
        };

        var protoFloraCapacity = ClampNormalized((fertility * 0.42f) + (waterFactor * 0.24f) + (temperatureFactor * 0.18f) + (terrainFactor * 0.08f) + (biomeFactor * 0.08f));
        var protoFaunaCapacity = ClampNormalized((protoFloraCapacity * 0.56f) + (temperatureFactor * 0.14f) + (terrainFactor * 0.14f) + (biomeFactor * 0.10f) + (waterFactor * 0.06f));
        var floraOccupancy = NormalizePopulation(floraPopulations.Values.Sum(value => (long)value), protoFloraCapacity, ProtoLifePressureConstants.FloraCapacityPopulationScale);
        var faunaOccupancy = NormalizePopulation(faunaPopulations.Values.Sum(value => (long)value), protoFaunaCapacity, ProtoLifePressureConstants.FaunaCapacityPopulationScale);
        var floraOccupancyDeficit = ClampNormalized(1.0f - floraOccupancy);
        var faunaOccupancyDeficit = ClampNormalized(1.0f - faunaOccupancy);
        var floraSupportDeficit = ClampNormalized(Math.Max(0.0f, protoFloraCapacity - floraOccupancy));
        var faunaSupportBaseline = ClampNormalized((floraOccupancy * 0.65f) + (protoFloraCapacity * 0.20f) + (faunaOccupancy * 0.15f));
        var faunaSupportDeficit = ClampNormalized(Math.Max(0.0f, protoFaunaCapacity - faunaSupportBaseline));
        var ecologicalVacancy = ClampNormalized((floraOccupancyDeficit * 0.52f) + (faunaOccupancyDeficit * 0.48f));

        return new ProtoLifeSubstrate
        {
            ProtoFloraCapacity = protoFloraCapacity,
            ProtoFaunaCapacity = protoFaunaCapacity,
            ProtoFloraPressure = ClampNormalized(protoFloraCapacity * floraOccupancyDeficit * 0.35f),
            ProtoFaunaPressure = ClampNormalized(protoFaunaCapacity * faunaSupportBaseline * faunaOccupancyDeficit * 0.28f),
            FloraOccupancyDeficit = floraOccupancyDeficit,
            FaunaOccupancyDeficit = faunaOccupancyDeficit,
            FloraSupportDeficit = floraSupportDeficit,
            FaunaSupportDeficit = faunaSupportDeficit,
            EcologicalVacancy = ecologicalVacancy,
            RecentCollapseOpening = 0.0f,
            ProtoFloraReadinessMonths = 0,
            ProtoFaunaReadinessMonths = 0,
            ProtoFloraCandidateMonths = 0,
            ProtoFaunaCandidateMonths = 0,
            ProtoFloraGenesisCooldownMonths = 0,
            ProtoFaunaGenesisCooldownMonths = 0
        };
    }

    private static float NormalizePopulation(double totalPopulation, float capacity, float scale)
    {
        if (capacity <= 0.0f || scale <= 0.0f)
        {
            return 0.0f;
        }

        return ClampNormalized((float)(totalPopulation / Math.Max(1.0f, capacity * scale)));
    }

    private static IReadOnlyDictionary<string, int> SeedFlora(
        Region region,
        FloraSpeciesCatalog floraCatalog,
        Random random)
    {
        var populations = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var species in floraCatalog.Definitions)
        {
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
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        Random random)
    {
        var populations = new Dictionary<string, int>(StringComparer.Ordinal);
        var floraSupport = floraPopulations.Count == 0
            ? 0.0f
            : (float)floraPopulations.Sum(entry =>
            {
                var flora = floraCatalog.GetById(entry.Key);
                return flora is null ? 0.0 : entry.Value * SubsistenceSupportModel.ResolveFloraSupportPerPopulation(region, flora);
            }) / EcologySeedingConstants.PopulationScale;
        var preySupportBySpeciesId = new Dictionary<string, float>(StringComparer.Ordinal);

        foreach (var species in faunaCatalog.Definitions)
        {
            if (!species.SupportedWaterAvailabilities.Contains(region.WaterAvailability))
            {
                continue;
            }

            var abundance = GetFaunaAbundance(region, species, floraPopulations, floraCatalog, preySupportBySpeciesId, random);
            if (abundance < EcologySeedingConstants.MinimumSeededPopulation)
            {
                continue;
            }

            populations[species.Id] = ToPopulationCount(abundance);
            preySupportBySpeciesId[species.Id] = abundance * species.FoodYield;
        }

        return populations;
    }

    private static float GetFloraAbundance(
        Region region,
        FloraSpeciesDefinition species,
        Random random)
    {
        if (!species.SupportedWaterAvailabilities.Contains(region.WaterAvailability))
        {
            return 0.0f;
        }

        var biomeMultiplier = species.CoreBiomes.Contains(region.Biome)
            ? 1.0f
            : EcologySeedingConstants.FloraNonCoreBiomeMultiplier;
        var fertilityFit = GetFertilityFit((float)region.Fertility, species.HabitatFertilityMin, species.HabitatFertilityMax);
        var variance = 1.0f + NextSignedVariance(random, EcologySeedingConstants.FloraRandomVarianceRange);
        var supportFit = ResolveFloraSupportFit(region, species, fertilityFit);
        var abundance = (supportFit * 0.34f) +
                        (species.GrowthRate * 0.18f) +
                        (species.RecoveryRate * 0.16f) +
                        (species.RegionalAbundance * 0.22f) +
                        (species.SpreadTendency * 0.10f) +
                        (species.CoreBiomes.Contains(region.Biome) ? EcologySeedingConstants.FloraBiomeMatchBonus : 0.0f);

        abundance *= EcologySeedingConstants.FloraWaterSupportMultiplier;
        abundance *= biomeMultiplier;
        abundance *= variance;

        return ClampNormalized(abundance);
    }

    private static float ResolveFloraSupportFit(Region region, FloraSpeciesDefinition species, float fertilityFit)
    {
        var waterFit = species.SupportedWaterAvailabilities.Contains(region.WaterAvailability) ? 1.0f : 0.0f;
        var biomeFit = species.CoreBiomes.Contains(region.Biome) ? 1.0f : EcologySeedingConstants.FloraNonCoreBiomeMultiplier;
        return ClampNormalized((fertilityFit * 0.42f) + (waterFit * 0.36f) + (biomeFit * 0.22f));
    }

    private static float GetFaunaAbundance(
        Region region,
        FaunaSpeciesDefinition species,
        IReadOnlyDictionary<string, int> floraPopulations,
        FloraSpeciesCatalog floraCatalog,
        IReadOnlyDictionary<string, float> preySupportBySpeciesId,
        Random random)
    {
        var fertilityFit = GetFertilityFit((float)region.Fertility, species.HabitatFertilityMin, species.HabitatFertilityMax);
        var biomeMultiplier = species.CoreBiomes.Contains(region.Biome)
            ? 1.0f
            : EcologySeedingConstants.FaunaNonCoreBiomeMultiplier;
        var support = ResolveSeedDietSupport(region, species, floraPopulations, floraCatalog, preySupportBySpeciesId);
        var supportFactor = ClampNormalized(ToPopulationSupport(support));

        var variance = 1.0f + NextSignedVariance(random, EcologySeedingConstants.FaunaRandomVarianceRange);
        var carryingSupport = Math.Clamp(
            support / Math.Max(0.01f, species.RequiredIntake * EcologySeedingConstants.PopulationScale * EcologySeedingConstants.FaunaSeedingSupportBuffer),
            0.0f,
            1.0f);
        var abundance = (supportFactor * 0.42f) +
                        (fertilityFit * 0.14f) +
                        (species.ReproductionRate * 0.12f) +
                        ((1.0f - species.RequiredIntake) * 0.08f) +
                        (species.FeedingEfficiency * 0.08f) +
                        ((1.0f - species.MortalitySensitivity) * 0.06f) +
                        ((1.0f - species.Mobility) * 0.04f) +
                        (species.RegionalAbundance * 0.06f) +
                        (species.CoreBiomes.Contains(region.Biome) ? EcologySeedingConstants.FaunaBiomeMatchBonus : 0.0f);

        abundance *= carryingSupport;
        abundance *= EcologySeedingConstants.FaunaWaterSupportMultiplier;
        abundance *= biomeMultiplier;
        abundance *= variance;

        return ClampNormalized(abundance);
    }

    private static float ResolveSeedDietSupport(
        Region region,
        FaunaSpeciesDefinition species,
        IReadOnlyDictionary<string, int> floraPopulations,
        FloraSpeciesCatalog floraCatalog,
        IReadOnlyDictionary<string, float> preySupportBySpeciesId)
    {
        // Seeder support intentionally follows the same preferred-then-fallback structure as
        // the live food web so emergency diet options do not overstate starting viability.
        var preferredLinks = species.DietLinks.Where(link => !link.IsFallback).ToArray();
        var fallbackLinks = species.DietLinks.Where(link => link.IsFallback).ToArray();
        var preferredSupport = ResolveSeedDietSupport(region, preferredLinks, floraPopulations, floraCatalog, preySupportBySpeciesId, includeFallbackPenalty: false);
        var remainingSupportNeed = Math.Max(0.0f, (species.RequiredIntake * EcologySeedingConstants.PopulationScale) - preferredSupport);
        var fallbackSupport = remainingSupportNeed <= 0.01f
            ? 0.0f
            : Math.Min(
                remainingSupportNeed,
                ResolveSeedDietSupport(region, fallbackLinks, floraPopulations, floraCatalog, preySupportBySpeciesId, includeFallbackPenalty: true));
        var support = preferredSupport + fallbackSupport;

        if (preferredLinks.Length > 0 && preferredSupport < EcologySeedingConstants.WeakFloraSupportThreshold * 0.75f)
        {
            support *= 0.55f;
        }

        return support;
    }

    private static float ResolveSeedDietSupport(
        Region region,
        IReadOnlyList<FaunaDietLink> links,
        IReadOnlyDictionary<string, int> floraPopulations,
        FloraSpeciesCatalog floraCatalog,
        IReadOnlyDictionary<string, float> preySupportBySpeciesId,
        bool includeFallbackPenalty)
    {
        if (links.Count == 0)
        {
            return 0.0f;
        }

        var totalWeight = links.Sum(link => link.Weight);
        if (totalWeight <= 0.0f)
        {
            return 0.0f;
        }

        var support = 0.0f;
        foreach (var link in links)
        {
            var share = link.Weight / totalWeight;
            var linkSupport = link.TargetKind switch
            {
                FaunaDietTargetKind.FloraSpecies => ResolveSeedFloraSupport(region, link.TargetSpeciesId, floraPopulations, floraCatalog) * EcologySeedingConstants.HerbivoreFloraSupportMultiplier,
                FaunaDietTargetKind.FaunaSpecies => preySupportBySpeciesId.GetValueOrDefault(link.TargetSpeciesId) * EcologySeedingConstants.CarnivorePreySupportMultiplier,
                FaunaDietTargetKind.ScavengePool => preySupportBySpeciesId.Values.Sum() * EcologySeedingConstants.OmnivorePreySupportMultiplier * FaunaSimulationConstants.ScavengeSupportShareMultiplier,
                _ => 0.0f
            };

            if (includeFallbackPenalty)
            {
                linkSupport *= 1.0f - FaunaSimulationConstants.FallbackDietPenalty;
            }

            support += linkSupport * share;
        }

        return support;
    }

    private static float ResolveSeedFloraSupport(
        Region region,
        string floraSpeciesId,
        IReadOnlyDictionary<string, int> floraPopulations,
        FloraSpeciesCatalog floraCatalog)
    {
        if (!floraPopulations.TryGetValue(floraSpeciesId, out var population))
        {
            return 0.0f;
        }

        var flora = floraCatalog.GetById(floraSpeciesId);
        return flora is null ? 0.0f : population * SubsistenceSupportModel.ResolveFloraSupportPerPopulation(region, flora);
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
