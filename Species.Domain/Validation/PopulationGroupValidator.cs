using Species.Domain.Models;

namespace Species.Domain.Validation;

public static class PopulationGroupValidator
{
    public static IReadOnlyList<string> Validate(World world)
    {
        var errors = new List<string>();
        var regionIds = world.Regions.Select(region => region.Id).ToHashSet(StringComparer.Ordinal);
        var groupIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var group in world.PopulationGroups)
        {
            if (string.IsNullOrWhiteSpace(group.Id))
            {
                errors.Add("A population group is missing an ID.");
            }
            else if (!groupIds.Add(group.Id))
            {
                errors.Add($"Duplicate population group ID detected: {group.Id}");
            }

            if (string.IsNullOrWhiteSpace(group.Name))
            {
                errors.Add($"Population group {group.Id} is missing a name.");
            }

            if (string.IsNullOrWhiteSpace(group.SpeciesId))
            {
                errors.Add($"Population group {group.Id} is missing a SpeciesId.");
            }

            if (!regionIds.Contains(group.CurrentRegionId))
            {
                errors.Add($"Population group {group.Id} references invalid CurrentRegionId {group.CurrentRegionId}.");
            }

            if (!regionIds.Contains(group.OriginRegionId))
            {
                errors.Add($"Population group {group.Id} references invalid OriginRegionId {group.OriginRegionId}.");
            }

            if (group.Population < 0)
            {
                errors.Add($"Population group {group.Id} has negative Population.");
            }

            if (group.StoredFood < 0)
            {
                errors.Add($"Population group {group.Id} has negative StoredFood.");
            }

            if (group.KnownRegionIds is null)
            {
                errors.Add($"Population group {group.Id} has null KnownRegionIds.");
            }
            else
            {
                if (!group.KnownRegionIds.Contains(group.CurrentRegionId))
                {
                    errors.Add($"Population group {group.Id} must know its current region.");
                }

                if (!group.KnownRegionIds.Contains(group.OriginRegionId))
                {
                    errors.Add($"Population group {group.Id} must know its origin region.");
                }
            }

            if (group.KnownDiscoveryIds is null)
            {
                errors.Add($"Population group {group.Id} has null KnownDiscoveryIds.");
            }
        }

        return errors;
    }
}
