using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Domain.Simulation;

namespace Species.Domain.Validation;

public static class DiscoveryStateValidator
{
    public static IReadOnlyList<string> Validate(World world, DiscoveryCatalog discoveryCatalog, SimulationTickResult tickResult)
    {
        var errors = new List<string>();
        var validDiscoveryIds = discoveryCatalog.Definitions.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);
        var validRegionIds = world.Regions.Select(region => region.Id).ToHashSet(StringComparer.Ordinal);
        var validRouteKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var region in world.Regions)
        {
            foreach (var neighborId in region.NeighborIds)
            {
                var routeKey = discoveryCatalog.GetRouteKey(region.Id, neighborId);
                validRouteKeys.Add(routeKey);
            }
        }

        foreach (var group in tickResult.World.PopulationGroups)
        {
            if (group.DiscoveryEvidence is null)
            {
                errors.Add($"Population group {group.Id} has null DiscoveryEvidence.");
                continue;
            }

            foreach (var discoveryId in group.KnownDiscoveryIds)
            {
                if (!validDiscoveryIds.Contains(discoveryId))
                {
                    errors.Add($"Population group {group.Id} references invalid discovery ID {discoveryId}.");
                }
            }

            ValidateRegionCounters(group.Id, group.DiscoveryEvidence.SuccessfulGatheringMonthsByRegionId, validRegionIds, errors, "gathering");
            ValidateRegionCounters(group.Id, group.DiscoveryEvidence.SuccessfulHuntingMonthsByRegionId, validRegionIds, errors, "hunting");
            ValidateRegionCounters(group.Id, group.DiscoveryEvidence.MonthsSpentByRegionId, validRegionIds, errors, "residence");
            ValidateRegionCounters(group.Id, group.DiscoveryEvidence.WaterExposureMonthsByRegionId, validRegionIds, errors, "water");

            foreach (var route in group.DiscoveryEvidence.RouteTraversalCountsByRouteId)
            {
                if (!validRouteKeys.Contains(route.Key))
                {
                    errors.Add($"Population group {group.Id} references invalid route evidence key {route.Key}.");
                }
            }
        }

        foreach (var change in tickResult.DiscoveryChanges)
        {
            if (string.IsNullOrWhiteSpace(change.GroupId))
            {
                errors.Add("A discovery change is missing a group ID.");
            }
        }

        return errors;
    }

    private static void ValidateRegionCounters(
        string groupId,
        IReadOnlyDictionary<string, int> counters,
        IReadOnlySet<string> validRegionIds,
        ICollection<string> errors,
        string label)
    {
        foreach (var entry in counters)
        {
            if (!validRegionIds.Contains(entry.Key))
            {
                errors.Add($"Population group {groupId} references invalid {label} evidence region {entry.Key}.");
            }
        }
    }
}
