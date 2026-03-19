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
        var worldSeed = seed ?? WorldGenerationConstants.DefaultSeed;
        var totalRegions = Math.Max(regionCount ?? WorldGenerationConstants.DefaultRegionCount, WorldGenerationConstants.MinimumRegionCount);
        var random = new Random(worldSeed);

        var neighborMap = BuildNeighborMap(totalRegions, random);
        var regions = new List<Region>(totalRegions);

        for (var index = 0; index < totalRegions; index++)
        {
            var waterAvailability = RollWaterAvailability(random);
            var biome = ResolveBiome(waterAvailability, random);
            var fertility = RollFertility(biome, waterAvailability, random);
            var regionId = $"region-{index + 1:D2}";
            var regionName = BuildRegionName(index);
            var neighbors = neighborMap[index]
                .Select(neighborIndex => $"region-{neighborIndex + 1:D2}")
                .OrderBy(neighborId => neighborId, StringComparer.Ordinal)
                .ToArray();
            var provisionalRegion = new Region(regionId, regionName, fertility, biome, waterAvailability, neighbors);
            var ecosystem = RegionEcosystemSeeder.Seed(provisionalRegion, floraCatalog, faunaCatalog, random);

            regions.Add(new Region(regionId, regionName, fertility, biome, waterAvailability, neighbors, ecosystem));
        }

        var provisionalWorld = new World(worldSeed, 1, 1, regions);
        var populationGroups = PopulationGroupSpawner.Spawn(
            provisionalWorld,
            PopulationGroupSpawningConstants.DefaultGroupCount,
            random);

        return new World(worldSeed, 1, 1, regions, populationGroups);
    }

    private static Dictionary<int, HashSet<int>> BuildNeighborMap(int regionCount, Random random)
    {
        var neighbors = Enumerable.Range(0, regionCount)
            .ToDictionary(index => index, _ => new HashSet<int>());

        for (var index = 1; index < regionCount; index++)
        {
            var neighborIndex = random.Next(index);
            Connect(neighbors, index, neighborIndex);
        }

        for (var index = 0; index < regionCount; index++)
        {
            var nextIndex = (index + 1) % regionCount;
            Connect(neighbors, index, nextIndex);
        }

        for (var index = 0; index < regionCount; index++)
        {
            while (neighbors[index].Count < WorldGenerationConstants.TargetNeighborCount)
            {
                var candidate = random.Next(regionCount);
                if (candidate == index)
                {
                    continue;
                }

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

    private static WaterAvailability RollWaterAvailability(Random random)
    {
        return random.NextDouble() switch
        {
            < 0.25 => WaterAvailability.Low,
            < 0.75 => WaterAvailability.Medium,
            _ => WaterAvailability.High
        };
    }

    private static Biome ResolveBiome(
        WaterAvailability waterAvailability,
        Random random)
    {
        var roll = random.NextDouble();

        if (waterAvailability == WaterAvailability.Low)
        {
            return roll switch
            {
                < 0.45 => Biome.Desert,
                < 0.70 => Biome.Plains,
                < 0.88 => Biome.Highlands,
                _ => Biome.Tundra
            };
        }

        if (waterAvailability == WaterAvailability.High)
        {
            return roll switch
            {
                < 0.45 => Biome.Wetlands,
                < 0.75 => Biome.Forest,
                < 0.88 => Biome.Plains,
                _ => Biome.Highlands
            };
        }

        return roll switch
        {
            < 0.10 => Biome.Desert,
            < 0.18 => Biome.Tundra,
            < 0.40 => Biome.Highlands,
            < 0.72 => Biome.Plains,
            < 0.92 => Biome.Forest,
            _ => Biome.Wetlands
        };
    }

    private static double RollFertility(
        Biome biome,
        WaterAvailability waterAvailability,
        Random random)
    {
        var fertility = biome switch
        {
            Biome.Desert => 0.18,
            Biome.Tundra => 0.22,
            Biome.Highlands => 0.36,
            Biome.Plains => 0.62,
            Biome.Forest => 0.72,
            Biome.Wetlands => 0.68,
            _ => 0.50
        };

        fertility += waterAvailability switch
        {
            WaterAvailability.Low => -0.18,
            WaterAvailability.Medium => 0.00,
            WaterAvailability.High => 0.12,
            _ => 0.00
        };

        fertility += (random.NextDouble() * 0.16) - 0.08;

        return Math.Round(
            Math.Clamp(
                fertility,
                WorldGenerationConstants.MinimumFertility,
                WorldGenerationConstants.MaximumFertility),
            2);
    }
}
