using Species.Domain.Models;

namespace Species.Domain.Validation;

public static class WorldValidator
{
    public static IReadOnlyList<string> Validate(World world)
    {
        var errors = new List<string>();
        var regionsById = new Dictionary<string, Region>(StringComparer.Ordinal);

        if (world.CurrentYear < 1)
        {
            errors.Add("World year must be at least 1.");
        }

        if (world.CurrentMonth is < 1 or > 12)
        {
            errors.Add("World month must be between 1 and 12.");
        }

        if (world.Regions.Count == 0)
        {
            errors.Add("World must contain at least one region.");
        }

        if (world.PopulationGroups is null)
        {
            errors.Add("World population groups collection cannot be null.");
        }

        if (world.Chronicle is null)
        {
            errors.Add("World chronicle cannot be null.");
        }

        foreach (var region in world.Regions)
        {
            if (!regionsById.TryAdd(region.Id, region))
            {
                errors.Add($"Duplicate region ID detected: {region.Id}");
            }

            if (region.Fertility < 0.0 || region.Fertility > 1.0)
            {
                errors.Add($"Region {region.Id} has fertility outside the normalized range.");
            }

            if (region.MaterialProfile is null)
            {
                errors.Add($"Region {region.Id} is missing material profile.");
            }
            else if (region.MaterialProfile.Opportunities.Timber < 0 ||
                     region.MaterialProfile.Opportunities.Stone < 0 ||
                     region.MaterialProfile.Opportunities.Fiber < 0 ||
                     region.MaterialProfile.Opportunities.Clay < 0 ||
                     region.MaterialProfile.Opportunities.Hides < 0 ||
                     region.MaterialProfile.ShelterPotential < 0 ||
                     region.MaterialProfile.StoragePotential < 0 ||
                     region.MaterialProfile.ToolPotential < 0 ||
                     region.MaterialProfile.TextilePotential < 0 ||
                     region.MaterialProfile.HidePotential < 0)
            {
                errors.Add($"Region {region.Id} has invalid material opportunities.");
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
