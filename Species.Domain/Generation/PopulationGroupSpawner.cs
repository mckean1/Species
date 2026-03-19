using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Generation;

public static class PopulationGroupSpawner
{
    private const string DefaultSpeciesId = "species-human";

    public static IReadOnlyList<PopulationGroup> Spawn(World world, int groupCount, Random random)
    {
        var viableRegions = world.Regions
            .Select(region => new
            {
                Region = region,
                Score = CalculateViabilityScore(region)
            })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Region.Id, StringComparer.Ordinal)
            .Take(Math.Max(groupCount * PopulationGroupSpawningConstants.RegionCandidateMultiplier, groupCount))
            .Select(candidate => candidate.Region)
            .ToArray();

        if (viableRegions.Length == 0)
        {
            return Array.Empty<PopulationGroup>();
        }

        var groups = new List<PopulationGroup>(groupCount);

        for (var index = 0; index < groupCount; index++)
        {
            var region = viableRegions[index % viableRegions.Length];
            var population = random.Next(
                PopulationGroupSpawningConstants.MinimumStartingPopulation,
                PopulationGroupSpawningConstants.MaximumStartingPopulation + 1);
            var storedFood = random.Next(
                PopulationGroupSpawningConstants.MinimumStartingStoredFood,
                PopulationGroupSpawningConstants.MaximumStartingStoredFood + 1);
            var subsistenceMode = ResolveSubsistenceMode(region);

            groups.Add(new PopulationGroup
            {
                Id = $"group-{index + 1:D2}",
                Name = BuildGroupName(region, index),
                SpeciesId = DefaultSpeciesId,
                CurrentRegionId = region.Id,
                OriginRegionId = region.Id,
                Population = population,
                StoredFood = storedFood,
                SubsistenceMode = subsistenceMode,
                KnownRegionIds = new HashSet<string>(GetKnownRegionIds(region), StringComparer.Ordinal),
                KnownDiscoveryIds = new HashSet<string>(StringComparer.Ordinal)
            });
        }

        return groups;
    }

    private static int CalculateViabilityScore(Region region)
    {
        var floraScore = region.Ecosystem.FloraPopulations.Values.Sum();
        var faunaScore = region.Ecosystem.FaunaPopulations.Values.Sum();
        return floraScore + faunaScore + (int)Math.Round(region.Fertility * 100);
    }

    private static SubsistenceMode ResolveSubsistenceMode(Region region)
    {
        var floraScore = region.Ecosystem.FloraPopulations.Values.Sum();
        var faunaScore = region.Ecosystem.FaunaPopulations.Values.Sum();

        if (floraScore > faunaScore * 2)
        {
            return SubsistenceMode.Gatherer;
        }

        if (faunaScore > floraScore * 2)
        {
            return SubsistenceMode.Hunter;
        }

        return SubsistenceMode.Mixed;
    }

    private static IEnumerable<string> GetKnownRegionIds(Region region)
    {
        yield return region.Id;

        foreach (var neighborId in region.NeighborIds.OrderBy(id => id, StringComparer.Ordinal))
        {
            yield return neighborId;
        }
    }

    private static string BuildGroupName(Region region, int index)
    {
        return $"{region.Name} Group {index + 1}";
    }
}
