using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Domain.Simulation;

namespace Species.Domain.Validation;

public static class AdvancementStateValidator
{
    public static IReadOnlyList<string> Validate(World world, AdvancementCatalog advancementCatalog, SimulationTickResult tickResult)
    {
        var errors = new List<string>();
        var validAdvancementIds = advancementCatalog.Definitions.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var group in world.PopulationGroups)
        {
            if (group.AdvancementEvidence is null)
            {
                errors.Add($"Population group {group.Id} has null AdvancementEvidence.");
                continue;
            }

            foreach (var advancementId in group.LearnedAdvancementIds)
            {
                if (!validAdvancementIds.Contains(advancementId))
                {
                    errors.Add($"Population group {group.Id} references invalid advancement ID {advancementId}.");
                }
            }

            if (group.AdvancementEvidence.ForagingOpportunityMonths < 0 ||
                group.AdvancementEvidence.SmallPreyOpportunityMonths < 0 ||
                group.AdvancementEvidence.LargePreyOpportunityMonths < 0 ||
                group.AdvancementEvidence.AquaticOpportunityMonths < 0 ||
                group.AdvancementEvidence.SurplusOpportunityMonths < 0 ||
                group.AdvancementEvidence.SpoilagePressureMonths < 0 ||
                group.AdvancementEvidence.StoneAccessMonths < 0 ||
                group.AdvancementEvidence.HideAccessMonths < 0 ||
                group.AdvancementEvidence.FiberAccessMonths < 0 ||
                group.AdvancementEvidence.FoodPressureMonths < 0 ||
                group.AdvancementEvidence.MaterialNeedMonths < 0 ||
                group.AdvancementEvidence.StabilityMonths < 0 ||
                group.AdvancementEvidence.AnchoredContinuityMonths < 0 ||
                group.AdvancementEvidence.OrganizationalReadinessMonths < 0)
            {
                errors.Add($"Population group {group.Id} has negative advancement evidence.");
            }

            ValidateProgressCounters(group.Id, group.AdvancementEvidence.AdvancementProgressById, validAdvancementIds, errors, "advancement progress");
            ValidateProgressCounters(group.Id, group.AdvancementEvidence.AdoptionProgressById, validAdvancementIds, errors, "adoption progress");

            if (group.LearnedAdvancementIds.Contains(AdvancementCatalog.LargeGameHuntingId) &&
                !group.LearnedAdvancementIds.Contains(AdvancementCatalog.SmallGameHuntingId))
            {
                errors.Add($"Population group {group.Id} has Large Game Hunting without Small Game Hunting.");
            }

            if (group.LearnedAdvancementIds.Contains(AdvancementCatalog.FoodStorageId) &&
                !group.LearnedAdvancementIds.Contains(AdvancementCatalog.FoodDryingId))
            {
                errors.Add($"Population group {group.Id} has Food Storage without Food Drying.");
            }

            if (group.LearnedAdvancementIds.Contains(AdvancementCatalog.StoneToolmakingId) &&
                !group.KnownDiscoveryIds.Contains(DiscoveryCatalog.ToolStoneId))
            {
                errors.Add($"Population group {group.Id} has Stone Toolmaking without Tool Stone.");
            }
        }

        foreach (var change in tickResult.AdvancementChanges)
        {
            if (string.IsNullOrWhiteSpace(change.GroupId))
            {
                errors.Add("An advancement change is missing a group ID.");
            }
        }

        return errors;
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
                errors.Add($"Population group {groupId} references invalid {label} ID {entry.Key}.");
            }

            if (entry.Value is < 0.0f or > 100.0f)
            {
                errors.Add($"Population group {groupId} has {label} outside the canonical 0-100 range for {entry.Key}.");
            }
        }
    }
}
