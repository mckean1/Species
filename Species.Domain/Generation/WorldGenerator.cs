using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Generation;

public static class WorldGenerator
{
    private static readonly string[] NamePrefixes =
    [
        "Amber",
        "Ash",
        "Blue",
        "Copper",
        "Dawn",
        "Frost",
        "Golden",
        "Green",
        "Iron",
        "Mist",
        "Red",
        "Stone"
    ];

    private static readonly string[] NameSuffixes =
    [
        "Basin",
        "Reach",
        "Vale",
        "March",
        "Steppe",
        "Fen",
        "Heights",
        "Field",
        "Coast",
        "Hollow",
        "Rise",
        "Run"
    ];

    public static World Create(
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        int? seed = null,
        int? regionCount = null)
    {
        var worldSeed = seed ?? Random.Shared.Next();
        var totalRegions = Math.Max(regionCount ?? WorldGenerationConstants.DefaultRegionCount, WorldGenerationConstants.MinimumRegionCount);
        var random = new Random(worldSeed);

        var neighborMap = BuildNeighborMap(totalRegions, random);
        var temperatureScores = BuildRegionalSignal(totalRegions, random, 0.50, 0.26, 0.08, 2.0, 0.06);
        var moistureScores = BuildRegionalSignal(totalRegions, random, 0.48, 0.22, 0.10, 2.5, 0.07);
        var ruggednessScores = BuildRegionalSignal(totalRegions, random, 0.44, 0.18, 0.12, 3.0, 0.08);
        var regions = new List<Region>(totalRegions);

        for (var index = 0; index < totalRegions; index++)
        {
            var temperatureBand = ResolveTemperatureBand(temperatureScores[index]);
            var terrainRuggedness = ResolveTerrainRuggedness(ruggednessScores[index]);
            var waterAvailability = ResolveWaterAvailability(moistureScores[index], temperatureScores[index], terrainRuggedness);
            var biome = ResolveBiome(temperatureBand, waterAvailability, terrainRuggedness, moistureScores[index]);
            var fertility = ResolveFertility(temperatureScores[index], moistureScores[index], terrainRuggedness, biome, random);
            var regionId = $"region-{index + 1:D2}";
            var regionName = BuildRegionName(index);
            var neighbors = neighborMap[index]
                .Select(neighborIndex => $"region-{neighborIndex + 1:D2}")
                .OrderBy(neighborId => neighborId, StringComparer.Ordinal)
                .ToArray();
            var provisionalRegion = new Region(
                regionId,
                regionName,
                fertility,
                biome,
                waterAvailability,
                neighbors,
                temperatureBand: temperatureBand,
                terrainRuggedness: terrainRuggedness);

            // WG-3: Seed primitive life only - not mature ecosystems
            var ecosystem = PrimitiveLifeSeeder.Seed(provisionalRegion, floraCatalog, faunaCatalog, random);
            var seededRegion = new Region(
                regionId,
                regionName,
                fertility,
                biome,
                waterAvailability,
                neighbors,
                ecosystem,
                temperatureBand: temperatureBand,
                terrainRuggedness: terrainRuggedness);

            regions.Add(new Region(
                regionId,
                regionName,
                fertility,
                biome,
                waterAvailability,
                neighbors,
                ecosystem,
                Species.Domain.Simulation.MaterialEconomySystem.BuildRegionMaterialProfile(seededRegion),
                temperatureBand,
                terrainRuggedness));
        }

        // WG-3: World starts with primitive life but no sapients
        // Sapient emergence belongs to a later roadmap phase (WG-4/WG-5)
        // and should occur through historical simulation, not fabrication during world generation
        var world = new World(worldSeed, 1, 1, regions);

        return world;
    }

    private static Dictionary<int, HashSet<int>> BuildNeighborMap(int regionCount, Random random)
    {
        var neighbors = Enumerable.Range(0, regionCount)
            .ToDictionary(index => index, _ => new HashSet<int>());

        for (var index = 0; index < regionCount; index++)
        {
            var nextIndex = (index + 1) % regionCount;
            Connect(neighbors, index, nextIndex);
        }

        var targetNeighborCount = Math.Min(WorldGenerationConstants.TargetNeighborCount, Math.Max(0, regionCount - 1));

        for (var index = 0; index < regionCount; index++)
        {
            var attempts = 0;
            while (neighbors[index].Count < targetNeighborCount && attempts < regionCount * 4)
            {
                attempts++;
                var direction = random.Next(2) == 0 ? -1 : 1;
                var distance = random.Next(2, Math.Min(WorldGenerationConstants.MaximumLocalNeighborDistance, regionCount - 1) + 1);
                var candidate = NormalizeRegionIndex(index + (direction * distance), regionCount);

                Connect(neighbors, index, candidate);
            }
        }

        return neighbors;
    }

    private static void Connect(IDictionary<int, HashSet<int>> neighbors, int left, int right)
    {
        neighbors[left].Add(right);
        neighbors[right].Add(left);
    }

    private static string BuildRegionName(int index)
    {
        var prefix = NamePrefixes[index % NamePrefixes.Length];
        var suffix = NameSuffixes[index % NameSuffixes.Length];
        var cycle = (index / NamePrefixes.Length) + 1;
        return cycle == 1 ? $"{prefix} {suffix}" : $"{prefix} {suffix} {cycle}";
    }

    private static double[] BuildRegionalSignal(
        int regionCount,
        Random random,
        double baseline,
        double primaryAmplitude,
        double secondaryAmplitude,
        double harmonic,
        double noiseRange)
    {
        var raw = new double[regionCount];
        var primaryPhase = random.NextDouble() * Math.PI * 2.0;
        var secondaryPhase = random.NextDouble() * Math.PI * 2.0;
        var bias = (random.NextDouble() * 0.14) - 0.07;

        for (var index = 0; index < regionCount; index++)
        {
            var angle = (index / (double)regionCount) * Math.PI * 2.0;
            var primary = Math.Sin(angle + primaryPhase) * primaryAmplitude;
            var secondary = Math.Sin((angle * harmonic) + secondaryPhase) * secondaryAmplitude;
            var noise = (random.NextDouble() * noiseRange * 2.0) - noiseRange;
            raw[index] = ClampNormalizedSignal(baseline + bias + primary + secondary + noise);
        }

        return SmoothSignal(raw);
    }

    private static double[] SmoothSignal(IReadOnlyList<double> raw)
    {
        var smoothed = new double[raw.Count];

        for (var index = 0; index < raw.Count; index++)
        {
            var previous = raw[NormalizeRegionIndex(index - 1, raw.Count)];
            var next = raw[NormalizeRegionIndex(index + 1, raw.Count)];
            var farPrevious = raw[NormalizeRegionIndex(index - 2, raw.Count)];
            var farNext = raw[NormalizeRegionIndex(index + 2, raw.Count)];

            smoothed[index] = ClampNormalizedSignal(
                (raw[index] * 0.42) +
                (previous * 0.22) +
                (next * 0.22) +
                (farPrevious * 0.07) +
                (farNext * 0.07));
        }

        return smoothed;
    }

    private static TemperatureBand ResolveTemperatureBand(double score)
    {
        return score switch
        {
            < 0.34 => TemperatureBand.Cold,
            < 0.68 => TemperatureBand.Temperate,
            _ => TemperatureBand.Hot
        };
    }

    private static TerrainRuggedness ResolveTerrainRuggedness(double score)
    {
        return score switch
        {
            < 0.34 => TerrainRuggedness.Flat,
            < 0.68 => TerrainRuggedness.Rolling,
            _ => TerrainRuggedness.Rugged
        };
    }

    private static WaterAvailability ResolveWaterAvailability(
        double moistureScore,
        double temperatureScore,
        TerrainRuggedness terrainRuggedness)
    {
        var evaporationPenalty = temperatureScore > 0.62
            ? (temperatureScore - 0.62) * 0.34
            : 0.0;
        var terrainModifier = terrainRuggedness switch
        {
            TerrainRuggedness.Flat => 0.06,
            TerrainRuggedness.Rolling => 0.01,
            TerrainRuggedness.Rugged => -0.05,
            _ => 0.0
        };
        var waterScore = ClampNormalizedSignal((moistureScore * 0.86) + 0.08 + terrainModifier - evaporationPenalty);

        return waterScore switch
        {
            < 0.36 => WaterAvailability.Low,
            < 0.70 => WaterAvailability.Medium,
            _ => WaterAvailability.High
        };
    }

    private static Biome ResolveBiome(
        TemperatureBand temperatureBand,
        WaterAvailability waterAvailability,
        TerrainRuggedness terrainRuggedness,
        double moistureScore)
    {
        if (temperatureBand == TemperatureBand.Cold)
        {
            return waterAvailability == WaterAvailability.High && terrainRuggedness != TerrainRuggedness.Rugged
                ? Biome.Forest
                : Biome.Tundra;
        }

        if (waterAvailability == WaterAvailability.Low)
        {
            if (temperatureBand == TemperatureBand.Hot)
            {
                return Biome.Desert;
            }

            return terrainRuggedness == TerrainRuggedness.Rugged
                ? Biome.Highlands
                : Biome.Plains;
        }

        if (waterAvailability == WaterAvailability.High)
        {
            if (terrainRuggedness == TerrainRuggedness.Flat || moistureScore >= 0.74)
            {
                return Biome.Wetlands;
            }

            return terrainRuggedness == TerrainRuggedness.Rugged
                ? Biome.Highlands
                : Biome.Forest;
        }

        if (terrainRuggedness == TerrainRuggedness.Rugged)
        {
            return Biome.Highlands;
        }

        return moistureScore >= 0.58 ? Biome.Forest : Biome.Plains;
    }

    private static double ResolveFertility(
        double temperatureScore,
        double moistureScore,
        TerrainRuggedness terrainRuggedness,
        Biome biome,
        Random random)
    {
        var climateSupport = ClampNormalizedSignal(1.0 - (Math.Abs(temperatureScore - 0.56) / 0.56));
        var terrainSupport = terrainRuggedness switch
        {
            TerrainRuggedness.Flat => 0.86,
            TerrainRuggedness.Rolling => 0.70,
            TerrainRuggedness.Rugged => 0.44,
            _ => 0.65
        };
        var biomeSupport = biome switch
        {
            Biome.Desert => 0.12,
            Biome.Tundra => 0.22,
            Biome.Highlands => 0.36,
            Biome.Plains => 0.68,
            Biome.Forest => 0.74,
            Biome.Wetlands => 0.64,
            _ => 0.50
        };
        var fertility =
            (moistureScore * 0.42) +
            (climateSupport * 0.24) +
            (terrainSupport * 0.20) +
            (biomeSupport * 0.14) +
            ((random.NextDouble() * 0.08) - 0.04);

        return Math.Round(
            Math.Clamp(
                fertility,
                WorldGenerationConstants.MinimumFertility,
                WorldGenerationConstants.MaximumFertility),
            2);
    }

    private static double ClampNormalizedSignal(double value)
    {
        return Math.Clamp(value, 0.0, 1.0);
    }

    private static int NormalizeRegionIndex(int index, int regionCount)
    {
        return ((index % regionCount) + regionCount) % regionCount;
    }
}
