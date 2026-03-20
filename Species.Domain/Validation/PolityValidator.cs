using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Validation;

public static class PolityValidator
{
    public static IReadOnlyList<string> Validate(World world)
    {
        var errors = new List<string>();
        var polityIds = new HashSet<string>(StringComparer.Ordinal);
        var groupIds = world.PopulationGroups.Select(group => group.Id).ToHashSet(StringComparer.Ordinal);
        var allSources = Enum.GetValues<ProposalBackingSource>().ToHashSet();

        foreach (var polity in world.Polities)
        {
            if (string.IsNullOrWhiteSpace(polity.Id))
            {
                errors.Add("A polity is missing an ID.");
                continue;
            }

            if (!polityIds.Add(polity.Id))
            {
                errors.Add($"Duplicate polity ID detected: {polity.Id}");
            }

            if (string.IsNullOrWhiteSpace(polity.Name))
            {
                errors.Add($"Polity {polity.Id} is missing a name.");
            }

            if (polity.MemberGroupIds.Count == 0)
            {
                errors.Add($"Polity {polity.Id} has no member groups.");
            }

            foreach (var groupId in polity.MemberGroupIds)
            {
                if (!groupIds.Contains(groupId))
                {
                    errors.Add($"Polity {polity.Id} references missing member group {groupId}.");
                }
            }

            if (polity.ActiveLawProposal is not null)
            {
                ValidateLawProposal(polity.Id, polity.ActiveLawProposal, expectActive: true, errors);
            }

            foreach (var proposal in polity.LawProposalHistory)
            {
                ValidateLawProposal(polity.Id, proposal, expectActive: false, errors);
            }

            var activeConflictSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var enactedLaw in polity.EnactedLaws.Where(law => law.IsActive))
            {
                if (string.IsNullOrWhiteSpace(enactedLaw.Title))
                {
                    errors.Add($"Polity {polity.Id} has an active enacted law without a title.");
                }

                if (enactedLaw.EnforcementStrength is < 0 or > 100)
                {
                    errors.Add($"Polity {polity.Id} has enacted law {enactedLaw.Title} with invalid EnforcementStrength.");
                }

                if (enactedLaw.ComplianceLevel is < 0 or > 100)
                {
                    errors.Add($"Polity {polity.Id} has enacted law {enactedLaw.Title} with invalid ComplianceLevel.");
                }

                if (!string.IsNullOrWhiteSpace(enactedLaw.ConflictSlot) &&
                    !activeConflictSlots.Add(enactedLaw.ConflictSlot))
                {
                    errors.Add($"Polity {polity.Id} has multiple active enacted laws in conflict slot {enactedLaw.ConflictSlot}.");
                }
            }

            var blocSources = new HashSet<ProposalBackingSource>();
            if (polity.PoliticalBlocs.Count != allSources.Count)
            {
                errors.Add($"Polity {polity.Id} does not have the full MVP political bloc set.");
            }

            foreach (var bloc in polity.PoliticalBlocs)
            {
                if (!blocSources.Add(bloc.Source))
                {
                    errors.Add($"Polity {polity.Id} has duplicate political bloc {bloc.Source}.");
                }

                if (bloc.Influence is < 0 or > 100)
                {
                    errors.Add($"Polity {polity.Id} has political bloc {bloc.Source} with invalid Influence.");
                }

                if (bloc.Satisfaction is < 0 or > 100)
                {
                    errors.Add($"Polity {polity.Id} has political bloc {bloc.Source} with invalid Satisfaction.");
                }
            }
        }

        if (world.Polities.Count > 0 && string.IsNullOrWhiteSpace(world.FocalPolityId))
        {
            errors.Add("World is missing a focal polity reference.");
        }
        else if (!string.IsNullOrWhiteSpace(world.FocalPolityId) &&
                 !world.Polities.Any(polity => string.Equals(polity.Id, world.FocalPolityId, StringComparison.Ordinal)))
        {
            errors.Add($"World focal polity {world.FocalPolityId} does not exist.");
        }

        var knownPolityIds = world.Polities.Select(polity => polity.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var group in world.PopulationGroups)
        {
            if (string.IsNullOrWhiteSpace(group.PolityId))
            {
                errors.Add($"Population group {group.Id} is missing a PolityId.");
                continue;
            }

            if (!knownPolityIds.Contains(group.PolityId))
            {
                errors.Add($"Population group {group.Id} references missing polity {group.PolityId}.");
            }
        }

        return errors;
    }

    private static void ValidateLawProposal(string polityId, LawProposal proposal, bool expectActive, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(proposal.Id))
        {
            errors.Add($"Polity {polityId} has a law proposal without an ID.");
        }

        if (string.IsNullOrWhiteSpace(proposal.DefinitionId))
        {
            errors.Add($"Polity {polityId} has law proposal {proposal.Title} without a definition ID.");
        }

        if (string.IsNullOrWhiteSpace(proposal.Summary))
        {
            errors.Add($"Polity {polityId} has law proposal {proposal.Title} without a summary.");
        }

        if (proposal.Support is < 0 or > 100)
        {
            errors.Add($"Polity {polityId} has law proposal {proposal.Title} with invalid Support.");
        }

        if (proposal.Opposition is < 0 or > 100)
        {
            errors.Add($"Polity {polityId} has law proposal {proposal.Title} with invalid Opposition.");
        }

        if (proposal.Urgency is < 0 or > 100)
        {
            errors.Add($"Polity {polityId} has law proposal {proposal.Title} with invalid Urgency.");
        }

        if (proposal.AgeInMonths < 0 || proposal.IgnoredMonths < 0)
        {
            errors.Add($"Polity {polityId} has law proposal {proposal.Title} with negative age values.");
        }

        if (proposal.SecondaryBackingSource == proposal.PrimaryBackingSource)
        {
            errors.Add($"Polity {polityId} has law proposal {proposal.Title} with duplicate backing sources.");
        }

        if (!expectActive && proposal.Status == LawProposalStatus.Active)
        {
            errors.Add($"Polity {polityId} has an active-status proposal stored in history.");
        }

        if (expectActive && proposal.Status != LawProposalStatus.Active)
        {
            errors.Add($"Polity {polityId} has a non-active active law proposal.");
        }
    }
}
