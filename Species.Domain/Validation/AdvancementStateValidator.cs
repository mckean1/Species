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

            if (group.AdvancementEvidence.SuccessfulGatheringWithKnowledgeMonths < 0 ||
                group.AdvancementEvidence.SuccessfulHuntingWithKnowledgeMonths < 0 ||
                group.AdvancementEvidence.SurplusStoredFoodMonths < 0 ||
                group.AdvancementEvidence.KnownRouteTravelMonths < 0 ||
                group.AdvancementEvidence.SuccessfulResidenceWithRegionKnowledgeMonths < 0 ||
                group.AdvancementEvidence.MaterialPracticeMonths < 0 ||
                group.AdvancementEvidence.StoragePressureMonths < 0 ||
                group.AdvancementEvidence.ShelterReadinessMonths < 0 ||
                group.AdvancementEvidence.StabilityMonths < 0 ||
                group.AdvancementEvidence.ContactLearningMonths < 0)
            {
                errors.Add($"Population group {group.Id} has negative advancement evidence.");
            }

            if (group.LearnedAdvancementIds.Contains(AdvancementCatalog.ImprovedHuntingId) &&
                !group.KnownDiscoveryIds.Contains(DiscoveryCatalog.SeasonalTrackingId))
            {
                errors.Add($"Population group {group.Id} has Improved Hunting without Seasonal Tracking.");
            }

            if (group.LearnedAdvancementIds.Contains(AdvancementCatalog.FoodStorageId) &&
                !group.KnownDiscoveryIds.Contains(DiscoveryCatalog.PreservationCluesId))
            {
                errors.Add($"Population group {group.Id} has Food Storage without Preservation Clues.");
            }

            if (group.LearnedAdvancementIds.Contains(AdvancementCatalog.StrongerShelterId) &&
                !group.KnownDiscoveryIds.Contains(DiscoveryCatalog.ShelterMethodsId))
            {
                errors.Add($"Population group {group.Id} has Stronger Shelter without Shelter Methods.");
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
}
