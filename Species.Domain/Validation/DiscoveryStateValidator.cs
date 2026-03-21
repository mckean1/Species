using Species.Domain.Catalogs;
using Species.Domain.Enums;
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
        var validPolityIds = world.Polities.Select(polity => polity.Id).ToHashSet(StringComparer.Ordinal);
        var validRouteKeys = new HashSet<string>(StringComparer.Ordinal);
        var validMaterialKeys = Enum.GetValues<MaterialResource>().Select(resource => resource.ToString()).ToHashSet(StringComparer.Ordinal);

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
            ValidateCounters(group.Id, group.DiscoveryEvidence.MaterialExposureMonthsByResourceId, validMaterialKeys, errors, "material exposure");
            ValidateCounters(group.Id, group.DiscoveryEvidence.MaterialUseMonthsByResourceId, validMaterialKeys, errors, "material use");
            ValidateCounters(group.Id, group.DiscoveryEvidence.ContactMonthsByPolityId, validPolityIds, errors, "contact polity");
            ValidateCounters(group.Id, group.DiscoveryEvidence.SharedExposureMonthsByDiscoveryId, validDiscoveryIds, errors, "shared discovery");
            ValidateProgressCounters(group.Id, group.DiscoveryEvidence.DiscoveryProgressByDiscoveryId, validDiscoveryIds, errors, "discovery progress");

            if (group.DiscoveryEvidence.RecurringFoodPressureMonths < 0 ||
                group.DiscoveryEvidence.RecurringThreatPressureMonths < 0 ||
                group.DiscoveryEvidence.RecurringMaterialShortageMonths < 0 ||
                group.DiscoveryEvidence.SettlementContinuityMonths < 0 ||
                group.DiscoveryEvidence.SeasonalObservationMonths < 0)
            {
                errors.Add($"Population group {group.Id} has negative discovery exposure memory.");
            }

            foreach (var route in group.DiscoveryEvidence.RouteTraversalCountsByRouteId)
            {
                if (!validRouteKeys.Contains(route.Key))
                {
                    errors.Add($"Population group {group.Id} references invalid route evidence key {route.Key}.");
                }
            }
        }

        foreach (var polity in tickResult.World.Polities)
        {
            foreach (var awareness in polity.SpeciesAwareness)
            {
                if (string.IsNullOrWhiteSpace(awareness.SpeciesId))
                {
                    errors.Add($"Polity {polity.Id} has a species-awareness entry with no species ID.");
                }

                if (awareness.EncounterProgress is < 0.0f or > 100.0f ||
                    awareness.DiscoveryProgress is < 0.0f or > 100.0f ||
                    awareness.KnowledgeProgress is < 0.0f or > 100.0f)
                {
                    errors.Add($"Polity {polity.Id} has species-awareness progress outside the canonical 0-100 range.");
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

    private static void ValidateCounters(
        string groupId,
        IReadOnlyDictionary<string, int> counters,
        IReadOnlySet<string> validKeys,
        ICollection<string> errors,
        string label)
    {
        foreach (var entry in counters)
        {
            if (!validKeys.Contains(entry.Key))
            {
                errors.Add($"Population group {groupId} references invalid {label} key {entry.Key}.");
            }

            if (entry.Value < 0)
            {
                errors.Add($"Population group {groupId} has negative {label} value for {entry.Key}.");
            }
        }
    }

    private static void ValidateProgressCounters(
        string groupId,
        IReadOnlyDictionary<string, float> counters,
        IReadOnlySet<string> validKeys,
        ICollection<string> errors,
        string label)
    {
        foreach (var entry in counters)
        {
            if (!validKeys.Contains(entry.Key))
            {
                errors.Add($"Population group {groupId} references invalid {label} key {entry.Key}.");
            }

            if (entry.Value is < 0.0f or > 100.0f)
            {
                errors.Add($"Population group {groupId} has {label} outside the canonical 0-100 range for {entry.Key}.");
            }
        }
    }
}
