using Species.Domain.Models;

namespace Species.Domain.Validation;

public static class WorldValidator
{
    public static IReadOnlyList<string> Validate(World world)
    {
        var errors = new List<string>();
        var regionsById = new Dictionary<string, Region>(StringComparer.Ordinal);

        foreach (var region in world.Regions)
        {
            if (!regionsById.TryAdd(region.Id, region))
            {
                errors.Add($"Duplicate region ID detected: {region.Id}");
            }
        }

        foreach (var region in world.Regions)
        {
            var uniqueNeighbors = new HashSet<string>(StringComparer.Ordinal);

            foreach (var neighborId in region.NeighborIds)
            {
                if (!uniqueNeighbors.Add(neighborId))
                {
                    errors.Add($"Region {region.Id} has duplicate neighbor entry {neighborId}.");
                }

                if (neighborId == region.Id)
                {
                    errors.Add($"Region {region.Id} cannot list itself as a neighbor.");
                }

                if (!regionsById.ContainsKey(neighborId))
                {
                    errors.Add($"Region {region.Id} references unknown neighbor {neighborId}.");
                    continue;
                }

                if (!regionsById[neighborId].NeighborIds.Contains(region.Id, StringComparer.Ordinal))
                {
                    errors.Add($"Neighbor link between {region.Id} and {neighborId} is not reciprocal.");
                }
            }
        }

        if (errors.Count > 0 || world.Regions.Count == 0)
        {
            return errors;
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>();
        pending.Enqueue(world.Regions[0].Id);

        while (pending.Count > 0)
        {
            var regionId = pending.Dequeue();
            if (!visited.Add(regionId))
            {
                continue;
            }

            foreach (var neighborId in regionsById[regionId].NeighborIds)
            {
                if (!visited.Contains(neighborId))
                {
                    pending.Enqueue(neighborId);
                }
            }
        }

        if (visited.Count != world.Regions.Count)
        {
            errors.Add("World connectivity check failed. Some regions are unreachable.");
        }

        return errors;
    }
}
