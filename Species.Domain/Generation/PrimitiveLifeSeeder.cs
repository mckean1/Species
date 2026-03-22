using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Discovery;
using Species.Domain.Models;

namespace Species.Domain.Generation;

/// <summary>
/// Seeds primitive life into regions after physical world generation.
/// WG-3: Establishes minimal, foundational organic state - not mature ecosystems.
/// </summary>
public static class PrimitiveLifeSeeder
{
    private static readonly PrimitiveSeedRole[] PrimitiveFloraRoles =
    [
        PrimitiveSeedRole.GroundCover,
        PrimitiveSeedRole.HardyBrush,
        PrimitiveSeedRole.WetlandGrowth
    ];

    private static readonly PrimitiveSeedRole[] PrimitiveFaunaRoles =
    [
        PrimitiveSeedRole.PrimaryHerbivore
    ];

    // Conservative seeding targets - primitive life is a foothold, not saturation
    private const float PrimitiveFloraMinAbundance = 0.30f;
    private const float PrimitiveFloraMaxAbundance = 0.60f;
    private const float PrimitiveFaunaMinAbundance = 0.20f;
    private const float PrimitiveFaunaMaxAbundance = 0.40f;

    public static RegionEcosystem Seed(
        Region region,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        Random random)
    {
        var primitiveCapacity = CalculatePrimitiveCapacity(region);
        var floraSeed = SeedPrimitiveFlora(region, floraCatalog, random);
        var faunaPopulations = SeedPrimitiveFauna(region, floraSeed.RoleSupports, faunaCatalog, random);
        var floraPopulations = floraSeed.Populations;
        var primitiveStrength = CalculatePrimitiveStrength(floraPopulations, faunaPopulations);
        var primitiveSubstrate = new PrimitiveLifeSubstrate
        {
            PrimitiveFloraCapacity = primitiveCapacity.FloraCapacity,
            PrimitiveFaunaCapacity = primitiveCapacity.FaunaCapacity,
            PrimitiveFloraStrength = primitiveStrength.FloraStrength,
            PrimitiveFaunaStrength = primitiveStrength.FaunaStrength
        };

        // Build legacy ProtoLifeSubstrate for backward compatibility with existing simulation systems
        var protoLifeSubstrate = BuildLegacyProtoLifeSubstrate(region, floraPopulations, faunaPopulations);

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

        return new RegionEcosystem(
            protoLifeSubstrate,
            floraPopulations,
            faunaPopulations,
            floraProfiles,
            faunaProfiles,
            primitiveLifeSubstrate: primitiveSubstrate);
    }

    private static (float FloraCapacity, float FaunaCapacity) CalculatePrimitiveCapacity(Region region)
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

        var floraCapacity = ClampNormalized(
            (fertility * 0.42f) +
            (waterFactor * 0.24f) +
            (temperatureFactor * 0.18f) +
            (terrainFactor * 0.08f) +
            (biomeFactor * 0.08f));

        var faunaCapacity = ClampNormalized(
            (floraCapacity * 0.56f) +
            (temperatureFactor * 0.14f) +
            (terrainFactor * 0.14f) +
            (biomeFactor * 0.10f) +
            (waterFactor * 0.06f));

        return (floraCapacity, faunaCapacity);
    }

    private static (float FloraStrength, float FaunaStrength) CalculatePrimitiveStrength(
        IReadOnlyDictionary<string, int> floraPopulations,
        IReadOnlyDictionary<string, int> faunaPopulations)
    {
        var floraTotal = floraPopulations.Values.Sum(value => (long)value);
        var faunaTotal = faunaPopulations.Values.Sum(value => (long)value);

        // Normalize against conservative scale - primitive life is a foothold, not saturation
        var floraStrength = ClampNormalized((float)floraTotal / 800.0f);
        var faunaStrength = ClampNormalized((float)faunaTotal / 400.0f);

        return (floraStrength, faunaStrength);
    }

    private static (IReadOnlyDictionary<string, int> Populations, IReadOnlyDictionary<PrimitiveSeedRole, float> RoleSupports) SeedPrimitiveFlora(
        Region region,
        FloraSpeciesCatalog floraCatalog,
        Random random)
    {
        var populations = new Dictionary<string, int>(StringComparer.Ordinal);
        var roleSupports = new Dictionary<PrimitiveSeedRole, float>();

        foreach (var role in PrimitiveFloraRoles)
        {
            var species = SelectPrimitiveFloraCandidate(region, floraCatalog, role);
            if (species is null)
            {
                continue;
            }

            var abundance = GetFloraAbundance(region, species, random);
            if (abundance < EcologySeedingConstants.MinimumSeededPopulation)
            {
                continue;
            }

            var primitiveAbundance = abundance * random.NextSingle(PrimitiveFloraMinAbundance, PrimitiveFloraMaxAbundance);
            var population = ToPopulationCount(primitiveAbundance);
            populations[species.Id] = population;
            roleSupports[role] = population * SubsistenceSupportModel.ResolveFloraSupportPerPopulation(region, species);
        }

        return (populations, roleSupports);
    }

    private static FloraSpeciesDefinition? SelectPrimitiveFloraCandidate(
        Region region,
        FloraSpeciesCatalog floraCatalog,
        PrimitiveSeedRole role)
    {
        return floraCatalog.GetPrimitiveSeedCandidates(role)
            .Select(species => new
            {
                Species = species,
                Fit = GetPrimitiveEnvironmentalFit(region, species.HabitatFertilityMin, species.HabitatFertilityMax, species.CoreBiomes, species.SupportedWaterAvailabilities, species.PrimitiveSeedMetadata)
            })
            .Where(candidate => candidate.Fit > 0.0f)
            .OrderByDescending(candidate => candidate.Fit)
            .ThenByDescending(candidate => candidate.Species.PrimitiveSeedMetadata?.Priority ?? 0)
            .ThenByDescending(candidate => candidate.Species.RegionalAbundance)
            .ThenBy(candidate => candidate.Species.Id, StringComparer.Ordinal)
            .Select(candidate => candidate.Species)
            .FirstOrDefault();
    }

    private static IReadOnlyDictionary<string, int> SeedPrimitiveFauna(
        Region region,
        IReadOnlyDictionary<PrimitiveSeedRole, float> floraRoleSupports,
        FaunaSpeciesCatalog faunaCatalog,
        Random random)
    {
        var populations = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var role in PrimitiveFaunaRoles)
        {
            var species = SelectPrimitiveFaunaCandidate(region, floraRoleSupports, faunaCatalog, role);
            if (species is null)
            {
                continue;
            }

            var abundance = GetFaunaAbundance(region, species, floraRoleSupports, random);
            if (abundance < EcologySeedingConstants.MinimumSeededPopulation)
            {
                continue;
            }

            var primitiveAbundance = abundance * random.NextSingle(PrimitiveFaunaMinAbundance, PrimitiveFaunaMaxAbundance);
            populations[species.Id] = ToPopulationCount(primitiveAbundance);
        }

        return populations;
    }

    private static FaunaSpeciesDefinition? SelectPrimitiveFaunaCandidate(
        Region region,
        IReadOnlyDictionary<PrimitiveSeedRole, float> floraRoleSupports,
        FaunaSpeciesCatalog faunaCatalog,
        PrimitiveSeedRole role)
    {
        return faunaCatalog.GetPrimitiveSeedCandidates(role)
            .Select(species => new
            {
                Species = species,
                Fit = GetPrimitiveEnvironmentalFit(region, species.HabitatFertilityMin, species.HabitatFertilityMax, species.CoreBiomes, species.SupportedWaterAvailabilities, species.PrimitiveSeedMetadata),
                Support = ResolveSeedDietSupport(species, floraRoleSupports)
            })
            .Where(candidate => candidate.Fit > 0.0f && candidate.Support > 0.0f)
            .OrderByDescending(candidate => candidate.Fit)
            .ThenByDescending(candidate => candidate.Support)
            .ThenByDescending(candidate => candidate.Species.PrimitiveSeedMetadata?.Priority ?? 0)
            .ThenByDescending(candidate => candidate.Species.RegionalAbundance)
            .ThenBy(candidate => candidate.Species.Id, StringComparer.Ordinal)
            .Select(candidate => candidate.Species)
            .FirstOrDefault();
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
        IReadOnlyDictionary<PrimitiveSeedRole, float> floraRoleSupports,
        Random random)
    {
        var fertilityFit = GetFertilityFit((float)region.Fertility, species.HabitatFertilityMin, species.HabitatFertilityMax);
        var biomeMultiplier = species.CoreBiomes.Contains(region.Biome)
            ? 1.0f
            : EcologySeedingConstants.FaunaNonCoreBiomeMultiplier;
        var support = ResolveSeedDietSupport(species, floraRoleSupports);
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
        FaunaSpeciesDefinition species,
        IReadOnlyDictionary<PrimitiveSeedRole, float> floraRoleSupports)
    {
        var supportedRoles = species.PrimitiveSeedMetadata?.SupportedFoodRoles;
        if (supportedRoles is null || supportedRoles.Count == 0)
        {
            return 0.0f;
        }

        var support = 0.0f;
        foreach (var role in supportedRoles.Distinct())
        {
            if (floraRoleSupports.TryGetValue(role, out var roleSupport))
            {
                support += roleSupport * EcologySeedingConstants.HerbivoreFloraSupportMultiplier;
            }
        }

        return support;
    }

    private static float GetPrimitiveEnvironmentalFit(
        Region region,
        float habitatFertilityMin,
        float habitatFertilityMax,
        IReadOnlyList<Biome> coreBiomes,
        IReadOnlyList<WaterAvailability> supportedWaterAvailabilities,
        PrimitiveSeedMetadata? seedMetadata)
    {
        if (seedMetadata is null)
        {
            return 0.0f;
        }

        var fertilityFit = GetFertilityFit((float)region.Fertility, habitatFertilityMin, habitatFertilityMax);
        var waterFit = supportedWaterAvailabilities.Contains(region.WaterAvailability) ? 1.0f : 0.0f;
        if (waterFit <= 0.0f)
        {
            return 0.0f;
        }

        var biomeFit = coreBiomes.Contains(region.Biome)
            ? 1.0f
            : 0.45f;
        var temperatureFit = seedMetadata.SupportedTemperatureBands.Count == 0 || seedMetadata.SupportedTemperatureBands.Contains(region.TemperatureBand)
            ? 1.0f
            : 0.0f;
        var terrainFit = seedMetadata.SupportedTerrainRuggednesses.Count == 0 || seedMetadata.SupportedTerrainRuggednesses.Contains(region.TerrainRuggedness)
            ? 1.0f
            : 0.0f;

        if (temperatureFit <= 0.0f || terrainFit <= 0.0f)
        {
            return 0.0f;
        }

        return ClampNormalized(
            (fertilityFit * 0.30f) +
            (waterFit * 0.24f) +
            (biomeFit * 0.22f) +
            (temperatureFit * 0.14f) +
            (terrainFit * 0.10f));
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
        return ClampNormalized(1.0f - (distance * 2.0f));
    }

    private static float ToPopulationSupport(float support)
    {
        return (float)Math.Sqrt(Math.Clamp(support / (EcologySeedingConstants.PopulationScale * 0.50), 0.0, 1.0));
    }

    private static int ToPopulationCount(float abundance)
    {
        return (int)Math.Max(1, Math.Round(abundance * EcologySeedingConstants.PopulationScale, MidpointRounding.AwayFromZero));
    }

    private static float NextSignedVariance(Random random, float range)
    {
        return (float)((random.NextDouble() * range * 2.0) - range);
    }

    private static float ClampNormalized(float value)
    {
        return Math.Clamp(value, 0.0f, 1.0f);
    }

    // Build legacy ProtoLifeSubstrate for backward compatibility
    private static ProtoLifeSubstrate BuildLegacyProtoLifeSubstrate(
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
}

public static class RandomExtensions
{
    public static float NextSingle(this Random random, float min, float max)
    {
        return min + (random.NextSingle() * (max - min));
    }
}
