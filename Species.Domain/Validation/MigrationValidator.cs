using Species.Domain.Constants;
using Species.Domain.Models;
using Species.Domain.Simulation;

namespace Species.Domain.Validation;

public static class MigrationValidator
{
    public static IReadOnlyList<string> Validate(World previousWorld, SimulationTickResult tickResult)
    {
        var errors = new List<string>();
        var previousGroupsById = previousWorld.PopulationGroups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var previousRegionsById = previousWorld.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);

        foreach (var change in tickResult.MigrationChanges)
        {
            if (!previousGroupsById.TryGetValue(change.GroupId, out var previousGroup))
            {
                errors.Add($"Migration change references unknown previous group {change.GroupId}.");
                continue;
            }

            if (!previousRegionsById.TryGetValue(change.CurrentRegionId, out var previousRegion))
            {
                errors.Add($"Migration change references invalid current region {change.CurrentRegionId} for {change.GroupId}.");
                continue;
            }

            if (change.Moved)
            {
                if (!previousRegion.NeighborIds.Contains(change.NewRegionId, StringComparer.Ordinal))
                {
                    errors.Add($"Group {change.GroupId} moved to non-adjacent region {change.NewRegionId}.");
                }

                if (!string.Equals(change.LastRegionId, previousGroup.CurrentRegionId, StringComparison.Ordinal))
                {
                    errors.Add($"Group {change.GroupId} did not record the correct LastRegionId on move.");
                }

                if (change.MonthsSinceLastMove != 0)
                {
                    errors.Add($"Group {change.GroupId} should reset MonthsSinceLastMove after moving.");
                }
            }
            else if (change.MonthsSinceLastMove < previousGroup.MonthsSinceLastMove)
            {
                errors.Add($"Group {change.GroupId} regressed MonthsSinceLastMove without moving.");
            }

            if (change.WinningRegionScore < change.CurrentRegionScore + MigrationConstants.MinimumMoveMargin && change.Moved)
            {
                errors.Add($"Group {change.GroupId} moved without clearing the minimum migration margin.");
            }
        }

        foreach (var group in tickResult.World.PopulationGroups)
        {
            if (!group.KnownRegionIds.Contains(group.CurrentRegionId))
            {
                errors.Add($"Group {group.Id} does not know its current region after migration.");
            }
        }

        return errors;
    }
}
