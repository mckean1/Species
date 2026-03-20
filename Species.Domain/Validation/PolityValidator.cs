using Species.Domain.Enums;
using Species.Domain.Models;
using Species.Domain.Catalogs;

namespace Species.Domain.Validation;

public static class PolityValidator
{
    private static readonly SocialTraditionCatalog SocialTraditionCatalog = new();

    public static IReadOnlyList<string> Validate(World world)
    {
        var errors = new List<string>();
        var polityIds = new HashSet<string>(StringComparer.Ordinal);
        var groupIds = world.PopulationGroups.Select(group => group.Id).ToHashSet(StringComparer.Ordinal);
        var regionIds = world.Regions.Select(region => region.Id).ToHashSet(StringComparer.Ordinal);
        var allPolityIds = world.Polities.Select(polity => polity.Id).ToHashSet(StringComparer.Ordinal);
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

            if (!string.IsNullOrWhiteSpace(polity.HomeRegionId) && !regionIds.Contains(polity.HomeRegionId))
            {
                errors.Add($"Polity {polity.Id} references missing home region {polity.HomeRegionId}.");
            }

            if (!string.IsNullOrWhiteSpace(polity.CoreRegionId) && !regionIds.Contains(polity.CoreRegionId))
            {
                errors.Add($"Polity {polity.Id} references missing core region {polity.CoreRegionId}.");
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

                if (enactedLaw.CoreEffectiveness is < 0 or > 100 ||
                    enactedLaw.PeripheralEffectiveness is < 0 or > 100 ||
                    enactedLaw.ResistanceLevel is < 0 or > 100)
                {
                    errors.Add($"Polity {polity.Id} has enacted law {enactedLaw.Title} with invalid effectiveness values.");
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

            var settlementIds = new HashSet<string>(StringComparer.Ordinal);
            var activeSettlementRegions = new HashSet<string>(StringComparer.Ordinal);
            var activePrimarySettlements = 0;
            foreach (var settlement in polity.Settlements)
            {
                if (string.IsNullOrWhiteSpace(settlement.Id))
                {
                    errors.Add($"Polity {polity.Id} has a settlement without an ID.");
                    continue;
                }

                if (!settlementIds.Add(settlement.Id))
                {
                    errors.Add($"Polity {polity.Id} has duplicate settlement ID {settlement.Id}.");
                }

                if (!string.Equals(settlement.PolityId, polity.Id, StringComparison.Ordinal))
                {
                    errors.Add($"Settlement {settlement.Id} does not belong to polity {polity.Id}.");
                }

                if (!regionIds.Contains(settlement.RegionId))
                {
                    errors.Add($"Settlement {settlement.Id} references missing region {settlement.RegionId}.");
                }

                if (settlement.StoredFood < 0)
                {
                    errors.Add($"Settlement {settlement.Id} has negative stored food.");
                }

                ValidateMaterialStockpile(settlement.MaterialStores, $"Settlement {settlement.Id}", errors);

                if (settlement.MaterialSupport is < 0 or > 100)
                {
                    errors.Add($"Settlement {settlement.Id} has invalid MaterialSupport.");
                }

                if (settlement.IsActive && !activeSettlementRegions.Add(settlement.RegionId))
                {
                    errors.Add($"Polity {polity.Id} has multiple active settlements in region {settlement.RegionId}.");
                }

                if (settlement.IsPrimary)
                {
                    activePrimarySettlements++;

                    if (!settlement.IsActive)
                    {
                        errors.Add($"Settlement {settlement.Id} is primary but inactive.");
                    }
                }
            }

            if (activePrimarySettlements > 1)
            {
                errors.Add($"Polity {polity.Id} has multiple primary settlements.");
            }

            if (!string.IsNullOrWhiteSpace(polity.PrimarySettlementId) &&
                !polity.Settlements.Any(settlement => string.Equals(settlement.Id, polity.PrimarySettlementId, StringComparison.Ordinal)))
            {
                errors.Add($"Polity {polity.Id} references missing primary settlement {polity.PrimarySettlementId}.");
            }

            if (!string.IsNullOrWhiteSpace(polity.PrimarySettlementId) &&
                !polity.Settlements.Any(settlement =>
                    string.Equals(settlement.Id, polity.PrimarySettlementId, StringComparison.Ordinal) &&
                    settlement.IsActive &&
                    settlement.IsPrimary))
            {
                errors.Add($"Polity {polity.Id} has a primary settlement reference that does not point at the active primary site.");
            }

            ValidateMaterialStockpile(polity.MaterialStores, $"Polity {polity.Id}", errors);

            if (polity.Governance.Legitimacy is < 0 or > 100 ||
                polity.Governance.Cohesion is < 0 or > 100 ||
                polity.Governance.Authority is < 0 or > 100 ||
                polity.Governance.Governability is < 0 or > 100 ||
                polity.Governance.PeripheralStrain is < 0 or > 100)
            {
                errors.Add($"Polity {polity.Id} has invalid governance state.");
            }

            if (polity.SocialMemory.SettlementContinuityMonths < 0 ||
                polity.SocialMemory.SeasonalMobilityMonths < 0 ||
                polity.SocialMemory.HardshipMonths < 0 ||
                polity.SocialMemory.SurplusMonths < 0 ||
                polity.SocialMemory.CoordinatedGovernanceMonths < 0 ||
                polity.SocialMemory.PeripheralStrainMonths < 0 ||
                polity.SocialMemory.RiverSettlementMonths < 0 ||
                polity.SocialMemory.FrontierExposureMonths < 0)
            {
                errors.Add($"Polity {polity.Id} has negative social memory counters.");
            }

            if (polity.SocialIdentity.Rootedness is < 0 or > 100 ||
                polity.SocialIdentity.Mobility is < 0 or > 100 ||
                polity.SocialIdentity.Communalism is < 0 or > 100 ||
                polity.SocialIdentity.AutonomyOrientation is < 0 or > 100 ||
                polity.SocialIdentity.OrderOrientation is < 0 or > 100 ||
                polity.SocialIdentity.FrontierDistinctiveness is < 0 or > 100)
            {
                errors.Add($"Polity {polity.Id} has invalid social identity scores.");
            }

            if (polity.SocialIdentity.Rootedness >= 85 && polity.SocialIdentity.Mobility >= 85)
            {
                errors.Add($"Polity {polity.Id} is unrealistically both highly rooted and highly mobile.");
            }

            foreach (var traditionId in polity.SocialIdentity.TraditionIds)
            {
                if (SocialTraditionCatalog.GetById(traditionId) is null)
                {
                    errors.Add($"Polity {polity.Id} references missing social tradition {traditionId}.");
                }
            }

            if (polity.SocialIdentity.TraditionIds.Distinct(StringComparer.Ordinal).Count() != polity.SocialIdentity.TraditionIds.Count)
            {
                errors.Add($"Polity {polity.Id} has duplicate social traditions.");
            }

            if (polity.MaterialProduction.ResilienceBonus is < 0 or > 30 ||
                polity.MaterialProduction.ShelterSupport is < 0 or > 100 ||
                polity.MaterialProduction.StorageSupport is < 0 or > 100 ||
                polity.MaterialProduction.ToolSupport is < 0 or > 100 ||
                polity.MaterialProduction.TextileSupport is < 0 or > 100 ||
                polity.MaterialProduction.SurplusScore is < 0 or > 100 ||
                polity.MaterialProduction.DeficitScore is < 0 or > 100)
            {
                errors.Add($"Polity {polity.Id} has invalid material production state.");
            }

            if (polity.MaterialShortageMonths < 0 || polity.MaterialSurplusMonths < 0)
            {
                errors.Add($"Polity {polity.Id} has negative material economy counters.");
            }

            if (polity.ExternalPressure.Threat is < 0 or > 100 ||
                polity.ExternalPressure.Cooperation is < 0 or > 100 ||
                polity.ExternalPressure.FrontierFriction is < 0 or > 100 ||
                polity.ExternalPressure.RaidPressure is < 0 or > 100 ||
                polity.ExternalPressure.HostileNeighborCount < 0)
            {
                errors.Add($"Polity {polity.Id} has invalid external pressure state.");
            }

            if (polity.ScaleState.Centralization is < 0 or > 100 ||
                polity.ScaleState.IntegrationDepth is < 0 or > 100 ||
                polity.ScaleState.AutonomyTolerance is < 0 or > 100 ||
                polity.ScaleState.CoordinationStrain is < 0 or > 100 ||
                polity.ScaleState.DistanceStrain is < 0 or > 100 ||
                polity.ScaleState.CompositeComplexity is < 0 or > 100 ||
                polity.ScaleState.OverextensionPressure is < 0 or > 100 ||
                polity.ScaleState.FragmentationRisk is < 0 or > 100)
            {
                errors.Add($"Polity {polity.Id} has invalid political scale state.");
            }

            if (polity.ScaleState.ExternalSuccessMonths < 0 ||
                polity.ScaleState.IntegrationSuccessMonths < 0 ||
                polity.ScaleState.ScaleContinuityMonths < 0)
            {
                errors.Add($"Polity {polity.Id} has negative political scale counters.");
            }

            if (!string.IsNullOrWhiteSpace(polity.ParentPolityId))
            {
                if (string.Equals(polity.ParentPolityId, polity.Id, StringComparison.Ordinal))
                {
                    errors.Add($"Polity {polity.Id} cannot be its own parent polity.");
                }
                else if (!allPolityIds.Contains(polity.ParentPolityId))
                {
                    errors.Add($"Polity {polity.Id} references missing parent polity {polity.ParentPolityId}.");
                }
            }

            var attachmentTargets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var attachment in polity.PoliticalAttachments)
            {
                if (string.IsNullOrWhiteSpace(attachment.RelatedPolityId))
                {
                    errors.Add($"Polity {polity.Id} has a political attachment without a target polity.");
                    continue;
                }

                if (!attachmentTargets.Add(attachment.RelatedPolityId))
                {
                    errors.Add($"Polity {polity.Id} has duplicate political attachment {attachment.RelatedPolityId}.");
                }

                if (!allPolityIds.Contains(attachment.RelatedPolityId) && attachment.IsActive)
                {
                    errors.Add($"Polity {polity.Id} has active political attachment to missing polity {attachment.RelatedPolityId}.");
                }

                if (attachment.IntegrationDepth is < 0 or > 100 || attachment.Loyalty is < 0 or > 100)
                {
                    errors.Add($"Polity {polity.Id} has invalid political attachment values for {attachment.RelatedPolityId}.");
                }
            }

            foreach (var record in polity.PoliticalHistory)
            {
                if (record.Year < 1 || record.Month is < 1 or > 12 || string.IsNullOrWhiteSpace(record.Summary))
                {
                    errors.Add($"Polity {polity.Id} has invalid political history metadata.");
                }
            }

            var relationTargets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var relation in polity.InterPolityRelations)
            {
                if (string.IsNullOrWhiteSpace(relation.OtherPolityId))
                {
                    errors.Add($"Polity {polity.Id} has an inter-polity relation without a target polity.");
                    continue;
                }

                if (string.Equals(relation.OtherPolityId, polity.Id, StringComparison.Ordinal))
                {
                    errors.Add($"Polity {polity.Id} cannot have a self-relation.");
                }

                if (!relationTargets.Add(relation.OtherPolityId))
                {
                    errors.Add($"Polity {polity.Id} has duplicate inter-polity relation {relation.OtherPolityId}.");
                }

                if (!allPolityIds.Contains(relation.OtherPolityId))
                {
                    errors.Add($"Polity {polity.Id} references missing polity {relation.OtherPolityId} in inter-polity relations.");
                }

                if (relation.ContactIntensity is < 0 or > 100 ||
                    relation.Trust is < 0 or > 100 ||
                    relation.Hostility is < 0 or > 100 ||
                    relation.Cooperation is < 0 or > 100 ||
                    relation.FrontierFriction is < 0 or > 100 ||
                    relation.Escalation is < 0 or > 100 ||
                    relation.RaidPressure is < 0 or > 100)
                {
                    errors.Add($"Polity {polity.Id} has invalid inter-polity relation scores for {relation.OtherPolityId}.");
                }

                if (relation.RaidsInflicted < 0 ||
                    relation.RaidsSuffered < 0 ||
                    relation.CooperationMonths < 0 ||
                    relation.SharedThreatMonths < 0 ||
                    relation.PeaceMonths < 0)
                {
                    errors.Add($"Polity {polity.Id} has negative inter-polity memory counters for {relation.OtherPolityId}.");
                }
            }

            var presenceRegionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var presence in polity.RegionalPresences)
            {
                if (string.IsNullOrWhiteSpace(presence.RegionId))
                {
                    errors.Add($"Polity {polity.Id} has a regional presence without a region.");
                    continue;
                }

                if (!presenceRegionIds.Add(presence.RegionId))
                {
                    errors.Add($"Polity {polity.Id} has duplicate regional presence for {presence.RegionId}.");
                }

                if (!regionIds.Contains(presence.RegionId))
                {
                    errors.Add($"Polity {polity.Id} has regional presence in missing region {presence.RegionId}.");
                }

                if (presence.TotalMonthsPresent < 0 ||
                    presence.ConsecutiveMonthsPresent < 0 ||
                    presence.ReturnCount < 0 ||
                    presence.MonthsSinceLastPresence < 0)
                {
                    errors.Add($"Polity {polity.Id} has negative regional presence counters for {presence.RegionId}.");
                }

                if (!presence.IsCurrent && presence.ConsecutiveMonthsPresent > 0)
                {
                    errors.Add($"Polity {polity.Id} has non-current presence {presence.RegionId} with positive consecutive months.");
                }

                if (presence.IsCurrent && presence.MonthsSinceLastPresence != 0)
                {
                    errors.Add($"Polity {polity.Id} has current presence {presence.RegionId} with MonthsSinceLastPresence set.");
                }
            }

            if (polity.AnchoringKind == PolityAnchoringKind.Anchored &&
                string.IsNullOrWhiteSpace(polity.CoreRegionId))
            {
                errors.Add($"Anchored polity {polity.Id} is missing a core region.");
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
        foreach (var polity in world.Polities)
        {
            if (!string.IsNullOrWhiteSpace(polity.ParentPolityId))
            {
                var parent = world.Polities.FirstOrDefault(other => string.Equals(other.Id, polity.ParentPolityId, StringComparison.Ordinal));
                if (parent is not null &&
                    !parent.PoliticalAttachments.Any(attachment => attachment.IsActive && string.Equals(attachment.RelatedPolityId, polity.Id, StringComparison.Ordinal)))
                {
                    errors.Add($"Polity {polity.Id} names {polity.ParentPolityId} as parent without reciprocal active attachment.");
                }
            }

            foreach (var relation in polity.InterPolityRelations)
            {
                var reciprocal = world.Polities
                    .FirstOrDefault(other => string.Equals(other.Id, relation.OtherPolityId, StringComparison.Ordinal))
                    ?.InterPolityRelations
                    .FirstOrDefault(otherRelation => string.Equals(otherRelation.OtherPolityId, polity.Id, StringComparison.Ordinal));
                if (reciprocal is null)
                {
                    errors.Add($"Polity relation {polity.Id}->{relation.OtherPolityId} is missing a reciprocal record.");
                }
            }
        }

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

        if (string.IsNullOrWhiteSpace(proposal.ReasonSummary))
        {
            errors.Add($"Polity {polityId} has law proposal {proposal.Title} without a reason summary.");
        }

        if (string.IsNullOrWhiteSpace(proposal.TradeoffSummary))
        {
            errors.Add($"Polity {polityId} has law proposal {proposal.Title} without a tradeoff summary.");
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

    private static void ValidateMaterialStockpile(MaterialStockpile stockpile, string ownerLabel, ICollection<string> errors)
    {
        if (stockpile.Timber < 0 ||
            stockpile.Stone < 0 ||
            stockpile.Fiber < 0 ||
            stockpile.Clay < 0 ||
            stockpile.Hides < 0)
        {
            errors.Add($"{ownerLabel} has negative material stores.");
        }
    }
}
