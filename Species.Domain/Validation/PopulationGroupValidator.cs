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

            if (group.MonthsSinceLastMove < 0)
            {
                errors.Add($"Population group {group.Id} has negative MonthsSinceLastMove.");
            }

            if (!string.IsNullOrWhiteSpace(group.LastRegionId) && !regionIds.Contains(group.LastRegionId))
            {
                errors.Add($"Population group {group.Id} references invalid LastRegionId {group.LastRegionId}.");
            }

            if (group.Pressures is null)
            {
                errors.Add($"Population group {group.Id} has null Pressures.");
            }
            else
            {
                ValidatePressureRange(group.Id, nameof(group.Pressures.FoodPressure), group.Pressures.FoodPressure, errors);
                ValidatePressureRange(group.Id, nameof(group.Pressures.WaterPressure), group.Pressures.WaterPressure, errors);
                ValidatePressureRange(group.Id, nameof(group.Pressures.ThreatPressure), group.Pressures.ThreatPressure, errors);
                ValidatePressureRange(group.Id, nameof(group.Pressures.OvercrowdingPressure), group.Pressures.OvercrowdingPressure, errors);
                ValidatePressureRange(group.Id, nameof(group.Pressures.MigrationPressure), group.Pressures.MigrationPressure, errors);
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

            if (group.DiscoveryEvidence is null)
            {
                errors.Add($"Population group {group.Id} has null DiscoveryEvidence.");
            }
        }

        return errors;
    }

    private static void ValidatePressureRange(string groupId, string pressureName, int value, ICollection<string> errors)
    {
        if (value is < 0 or > 100)
        {
            errors.Add($"Population group {groupId} has {pressureName} outside the 0-100 range.");
        }
    }
}
