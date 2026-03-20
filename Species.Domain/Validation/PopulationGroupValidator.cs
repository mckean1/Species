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

            if (group.ActiveLawProposal is not null)
            {
                if (string.IsNullOrWhiteSpace(group.ActiveLawProposal.Title))
                {
                    errors.Add($"Population group {group.Id} has an active law proposal without a title.");
                }

                if (group.ActiveLawProposal.Status != Species.Domain.Enums.LawProposalStatus.Active)
                {
                    errors.Add($"Population group {group.Id} has a non-active active law proposal.");
                }
            }

            if (group.LawProposalHistory is null)
            {
                errors.Add($"Population group {group.Id} has null LawProposalHistory.");
            }

            if (group.EnactedLaws is null)
            {
                errors.Add($"Population group {group.Id} has null EnactedLaws.");
            }
            else
            {
                var activeConflictSlots = new HashSet<string>(StringComparer.Ordinal);
                foreach (var enactedLaw in group.EnactedLaws.Where(law => law.IsActive))
                {
                    if (string.IsNullOrWhiteSpace(enactedLaw.Title))
                    {
                        errors.Add($"Population group {group.Id} has an active enacted law without a title.");
                    }

                    if (enactedLaw.EnforcementStrength is < 0 or > 100)
                    {
                        errors.Add($"Population group {group.Id} has enacted law {enactedLaw.Title} with invalid EnforcementStrength.");
                    }

                    if (enactedLaw.ComplianceLevel is < 0 or > 100)
                    {
                        errors.Add($"Population group {group.Id} has enacted law {enactedLaw.Title} with invalid ComplianceLevel.");
                    }

                    if (!string.IsNullOrWhiteSpace(enactedLaw.ConflictSlot) &&
                        !activeConflictSlots.Add(enactedLaw.ConflictSlot))
                    {
                        errors.Add($"Population group {group.Id} has multiple active enacted laws in conflict slot {enactedLaw.ConflictSlot}.");
                    }
                }
            }

            if (group.PoliticalBlocs is null)
            {
                errors.Add($"Population group {group.Id} has null PoliticalBlocs.");
            }
            else
            {
                var blocSources = new HashSet<Species.Domain.Enums.ProposalBackingSource>();
                foreach (var bloc in group.PoliticalBlocs)
                {
                    if (!blocSources.Add(bloc.Source))
                    {
                        errors.Add($"Population group {group.Id} has duplicate political bloc {bloc.Source}.");
                    }

                    if (bloc.Influence is < 0 or > 100)
                    {
                        errors.Add($"Population group {group.Id} has political bloc {bloc.Source} with invalid Influence.");
                    }

                    if (bloc.Satisfaction is < 0 or > 100)
                    {
                        errors.Add($"Population group {group.Id} has political bloc {bloc.Source} with invalid Satisfaction.");
                    }
                }
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

            if (group.LearnedAdvancementIds is null)
            {
                errors.Add($"Population group {group.Id} has null LearnedAdvancementIds.");
            }

            if (group.AdvancementEvidence is null)
            {
                errors.Add($"Population group {group.Id} has null AdvancementEvidence.");
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
