using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class PoliticalScalingSystem
{
    public PoliticalScaleResult Run(World world)
    {
        if (world.Polities.Count == 0)
        {
            return new PoliticalScaleResult(world, Array.Empty<PoliticalScaleChange>());
        }

        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var updatedGroups = world.PopulationGroups.Select(group => CloneGroup(group)).ToList();
        var polities = world.Polities
            .Select(polity => polity.Clone())
            .ToDictionary(polity => polity.Id, StringComparer.Ordinal);
        var changes = new List<PoliticalScaleChange>();

        foreach (var polity in polities.Values)
        {
            CleanupAttachments(polity, polities);
        }

        RebuildMemberGroupIds(polities.Values, updatedGroups);
        UpdateScaleStates(world, polities, updatedGroups, regionsById, changes, announceTransitions: true);
        ResolveAttachmentsAndIntegrations(world, polities, updatedGroups, regionsById, changes);
        RebuildMemberGroupIds(polities.Values, updatedGroups);
        UpdateScaleStates(world, polities, updatedGroups, regionsById, changes, announceTransitions: false);
        ResolveFragmentation(world, polities, updatedGroups, regionsById, changes);
        RebuildMemberGroupIds(polities.Values, updatedGroups);
        UpdateScaleStates(world, polities, updatedGroups, regionsById, changes, announceTransitions: false);

        var survivingPolities = polities.Values
            .Where(polity => polity.MemberGroupIds.Count > 0 || polity.PoliticalAttachments.Any(attachment => attachment.IsActive))
            .OrderBy(polity => polity.Id, StringComparer.Ordinal)
            .ToArray();
        PurgeDeadReferences(survivingPolities);

        return new PoliticalScaleResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, survivingPolities, world.FocalPolityId),
            changes);
    }

    private static void ResolveAttachmentsAndIntegrations(
        World world,
        IDictionary<string, Polity> polities,
        List<PopulationGroup> groups,
        IReadOnlyDictionary<string, Region> regionsById,
        ICollection<PoliticalScaleChange> changes)
    {
        var polityIds = polities.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        for (var index = 0; index < polityIds.Length; index++)
        {
            for (var otherIndex = index + 1; otherIndex < polityIds.Length; otherIndex++)
            {
                if (!polities.TryGetValue(polityIds[index], out var first) ||
                    !polities.TryGetValue(polityIds[otherIndex], out var second))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(first.ParentPolityId) ||
                    !string.IsNullOrWhiteSpace(second.ParentPolityId))
                {
                    continue;
                }

                var firstContext = BuildContext(world, polities.Values, groups, first);
                var secondContext = BuildContext(world, polities.Values, groups, second);
                if (firstContext is null || secondContext is null)
                {
                    continue;
                }

                var firstStrength = ResolveScaleStrength(firstContext);
                var secondStrength = ResolveScaleStrength(secondContext);
                var stronger = firstStrength >= secondStrength ? first : second;
                var weaker = stronger == first ? second : first;
                var strongerContext = stronger == first ? firstContext : secondContext;
                var weakerContext = stronger == first ? secondContext : firstContext;
                var relation = stronger.InterPolityRelations.FirstOrDefault(item => string.Equals(item.OtherPolityId, weaker.Id, StringComparison.Ordinal));
                if (relation is null || relation.ContactIntensity < 18)
                {
                    continue;
                }

                var strengthGap = Math.Abs(firstStrength - secondStrength);
                if (strengthGap < 45)
                {
                    continue;
                }

                if (CanDirectIntegrate(strongerContext, weakerContext, relation, strengthGap))
                {
                    DirectIntegrate(world, stronger, weaker, polities, groups, changes);
                    continue;
                }

                if (CanBindSubordinate(strongerContext, weakerContext, relation, strengthGap))
                {
                    BindAttachment(world, stronger, weaker, PoliticalAttachmentKind.Subordinate, changes);
                    continue;
                }

                if (CanBindFederated(strongerContext, weakerContext, relation))
                {
                    BindAttachment(world, stronger, weaker, PoliticalAttachmentKind.FederatedAttachment, changes);
                }
            }
        }
    }

    private static void ResolveFragmentation(
        World world,
        IDictionary<string, Polity> polities,
        List<PopulationGroup> groups,
        IReadOnlyDictionary<string, Region> regionsById,
        ICollection<PoliticalScaleChange> changes)
    {
        foreach (var polity in polities.Values.OrderBy(polity => polity.Id, StringComparer.Ordinal).ToArray())
        {
            if (!string.IsNullOrWhiteSpace(polity.ParentPolityId))
            {
                if (polity.ScaleState.FragmentationRisk >= 72 ||
                    polity.PoliticalAttachments.Any(attachment => attachment.IsActive && attachment.Loyalty <= 25))
                {
                    ReleaseFromParent(world, polity, polities, changes);
                }

                continue;
            }

            if (polity.ScaleState.Form is PoliticalScaleForm.LocalPolity or PoliticalScaleForm.RegionalState)
            {
                continue;
            }

            if (polity.ScaleState.FragmentationRisk < 78 ||
                polity.Settlements.Count(settlement => settlement.IsActive) < 3)
            {
                continue;
            }

            CreateBreakaway(world, polity, polities, groups, regionsById, changes);
        }
    }

    private static void UpdateScaleStates(
        World world,
        IDictionary<string, Polity> polities,
        IReadOnlyList<PopulationGroup> groups,
        IReadOnlyDictionary<string, Region> regionsById,
        ICollection<PoliticalScaleChange> changes,
        bool announceTransitions)
    {
        foreach (var polity in polities.Values.OrderBy(polity => polity.Id, StringComparer.Ordinal))
        {
            var previousForm = polity.ScaleState.Form;
            var context = BuildContext(world, polities.Values, groups, polity);
            if (context is null)
            {
                continue;
            }

            var metrics = CalculateMetrics(polity, context, polities, regionsById);
            polity.ScaleState.Centralization = DriftToward(polity.ScaleState.Centralization, metrics.Centralization);
            polity.ScaleState.IntegrationDepth = DriftToward(polity.ScaleState.IntegrationDepth, metrics.IntegrationDepth);
            polity.ScaleState.AutonomyTolerance = DriftToward(polity.ScaleState.AutonomyTolerance, metrics.AutonomyTolerance);
            polity.ScaleState.CoordinationStrain = DriftToward(polity.ScaleState.CoordinationStrain, metrics.CoordinationStrain);
            polity.ScaleState.DistanceStrain = DriftToward(polity.ScaleState.DistanceStrain, metrics.DistanceStrain);
            polity.ScaleState.CompositeComplexity = DriftToward(polity.ScaleState.CompositeComplexity, metrics.CompositeComplexity);
            polity.ScaleState.OverextensionPressure = DriftToward(polity.ScaleState.OverextensionPressure, metrics.OverextensionPressure);
            polity.ScaleState.FragmentationRisk = DriftToward(polity.ScaleState.FragmentationRisk, metrics.FragmentationRisk);
            polity.ScaleState.Form = metrics.Form;
            polity.ScaleState.Summary = BuildScaleSummary(metrics.Form, polity.ScaleState);
            polity.ScaleState.ScaleContinuityMonths = previousForm == metrics.Form
                ? polity.ScaleState.ScaleContinuityMonths + 1
                : 0;
            polity.ScaleState.ExternalSuccessMonths = metrics.ExternalSuccess ? polity.ScaleState.ExternalSuccessMonths + 1 : Math.Max(0, polity.ScaleState.ExternalSuccessMonths - 1);
            polity.ScaleState.IntegrationSuccessMonths = metrics.IntegrationSuccess ? polity.ScaleState.IntegrationSuccessMonths + 1 : Math.Max(0, polity.ScaleState.IntegrationSuccessMonths - 1);

            UpdateActiveAttachmentDrift(polity, context);

            if (announceTransitions && previousForm != metrics.Form)
            {
                var message = metrics.Form switch
                {
                    PoliticalScaleForm.RegionalState => $"{polity.Name} consolidated into a broader regional state.",
                    PoliticalScaleForm.KingdomRealm => $"{polity.Name} consolidated the core into a larger kingdom-like realm.",
                    PoliticalScaleForm.CompositeRealm => $"{polity.Name} became a wider composite realm.",
                    PoliticalScaleForm.EmpireLike => $"{polity.Name} grew into an empire-like expansive state.",
                    PoliticalScaleForm.Fragmenting => $"{polity.Name} began to strain as a fragmenting realm.",
                    _ => $"{polity.Name} contracted back toward a tighter local polity."
                };

                RecordHistory(polity, string.Empty, string.Empty, PoliticalHistoryKind.Consolidation, world.CurrentYear, world.CurrentMonth, message);
                changes.Add(new PoliticalScaleChange
                {
                    PolityId = polity.Id,
                    PolityName = polity.Name,
                    Kind = "scale-form",
                    Message = message
                });
            }
        }
    }

    private static ScaleMetrics CalculateMetrics(
        Polity polity,
        PolityContext context,
        IDictionary<string, Polity> polities,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        var activeSettlements = polity.Settlements.Where(settlement => settlement.IsActive).ToArray();
        var currentPresence = polity.RegionalPresences.Where(presence => presence.IsCurrent).ToArray();
        var activeAttachments = polity.PoliticalAttachments.Where(attachment => attachment.IsActive).ToArray();
        var subordinateCount = polities.Values.Count(other => string.Equals(other.ParentPolityId, polity.Id, StringComparison.Ordinal));
        var activeRegions = activeSettlements.Select(settlement => settlement.RegionId)
            .Concat(currentPresence.Select(presence => presence.RegionId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var distanceProfile = CalculateDistanceProfile(context.CoreRegionId, activeRegions, regionsById);
        var capacity = ResolveCapacityScore(context, activeSettlements.Length, activeRegions.Length, activeAttachments.Length + subordinateCount);
        var coordinationStrain = Math.Clamp(
            Math.Max(0, activeSettlements.Length - 1) * 9 +
            Math.Max(0, activeRegions.Length - 1) * 7 +
            (activeAttachments.Length * 14) +
            (subordinateCount * 10),
            0,
            100);
        var distanceStrain = Math.Clamp((distanceProfile.AverageDistance * 14) + (distanceProfile.MaxDistance * 8), 0, 100);
        var compositeComplexity = Math.Clamp((activeAttachments.Length * 18) + (subordinateCount * 14) + (context.ParentPolityId.Length > 0 ? 10 : 0), 0, 100);
        var centralization = Math.Clamp(
            (context.Governance.Authority / 2) +
            (HasLaw(polity, GovernanceLawCatalog.StrengthenCentralAuthorityId) ? 18 : 0) +
            (HasLaw(polity, GovernanceLawCatalog.CentralizeStoresId) ? 8 : 0) -
            (context.SocialIdentity.AutonomyOrientation / 5),
            0,
            100);
        var autonomyTolerance = Math.Clamp(
            (context.SocialIdentity.AutonomyOrientation / 2) +
            (HasLaw(polity, GovernanceLawCatalog.SharedGovernanceId) ? 18 : 0) +
            (HasLaw(polity, GovernanceLawCatalog.LocalStoreAutonomyId) ? 12 : 0) +
            (HasLaw(polity, GovernanceLawCatalog.OpenMovementId) ? 6 : 0) -
            (centralization / 5),
            0,
            100);
        var integrationDepth = Math.Clamp(
            (context.Governance.Governability / 2) +
            (context.Governance.Cohesion / 4) +
            (context.MaterialProduction.StorageSupport / 6) +
            (context.ScaleState.IntegrationSuccessMonths / 2) -
            (distanceStrain / 4) -
            (compositeComplexity / 5) -
            (context.Governance.PeripheralStrain / 5) +
            (HasLaw(polity, GovernanceLawCatalog.FrontierIntegrationId) ? 10 : 0),
            0,
            100);
        var overextensionPressure = Math.Clamp(
            (coordinationStrain / 2) +
            (distanceStrain / 2) +
            (compositeComplexity / 2) +
            (context.ExternalPressure.Threat / 5) +
            (context.ExternalPressure.RaidPressure / 5) +
            (context.Governance.PeripheralStrain / 3) -
            (integrationDepth / 4) -
            (context.MaterialProduction.ResilienceBonus / 2),
            0,
            100);
        var fragmentationRisk = Math.Clamp(
            (overextensionPressure / 2) +
            (Math.Max(0, 55 - context.Governance.Legitimacy) / 2) +
            (Math.Max(0, 55 - context.Governance.Cohesion) / 2) +
            (Math.Max(0, 55 - context.Governance.Authority) / 3) +
            (context.SocialIdentity.FrontierDistinctiveness / 4) +
            (autonomyTolerance / 6) -
            (integrationDepth / 5) -
            (context.ScaleState.IntegrationSuccessMonths / 3),
            0,
            100);

        var form = ResolveForm(capacity, activeSettlements.Length, activeRegions.Length, activeAttachments.Length + subordinateCount, integrationDepth, overextensionPressure, fragmentationRisk);
        var interPolitySummary = SummarizeInterPolity(polity);
        var externalSuccess = interPolitySummary.RaidsInflicted > interPolitySummary.RaidsSuffered &&
                              context.ExternalPressure.Threat < 65;
        var integrationSuccess = activeAttachments.All(attachment => attachment.Loyalty >= 45 && attachment.IntegrationDepth >= 35) &&
                                 integrationDepth >= 50;

        return new ScaleMetrics(
            form,
            centralization,
            integrationDepth,
            autonomyTolerance,
            coordinationStrain,
            distanceStrain,
            compositeComplexity,
            overextensionPressure,
            fragmentationRisk,
            externalSuccess,
            integrationSuccess);
    }

    private static void UpdateActiveAttachmentDrift(Polity polity, PolityContext context)
    {
        foreach (var attachment in polity.PoliticalAttachments.Where(attachment => attachment.IsActive))
        {
            attachment.IntegrationDepth = DriftToward(
                attachment.IntegrationDepth,
                Math.Clamp((context.ScaleState.IntegrationDepth + context.Governance.Cohesion) / 2 - (context.Governance.PeripheralStrain / 5), 0, 100));
            attachment.Loyalty = DriftToward(
                attachment.Loyalty,
                Math.Clamp((context.Governance.Legitimacy + context.ScaleState.AutonomyTolerance) / 2 - (context.ExternalPressure.RaidPressure / 6), 0, 100));
        }
    }

    private static void DirectIntegrate(
        World world,
        Polity stronger,
        Polity weaker,
        IDictionary<string, Polity> polities,
        List<PopulationGroup> groups,
        ICollection<PoliticalScaleChange> changes)
    {
        foreach (var index in Enumerable.Range(0, groups.Count).Where(i => string.Equals(groups[i].PolityId, weaker.Id, StringComparison.Ordinal)).ToArray())
        {
            groups[index] = CloneGroup(groups[index], stronger.Id);
        }

        foreach (var settlement in weaker.Settlements.Where(settlement => settlement.IsActive))
        {
            var existing = stronger.Settlements.FirstOrDefault(item => item.IsActive && string.Equals(item.RegionId, settlement.RegionId, StringComparison.Ordinal));
            if (existing is null)
            {
                stronger.Settlements.Add(CloneSettlementForPolity(settlement, stronger.Id));
                continue;
            }

            existing.StoredFood += settlement.StoredFood;
            existing.MaterialStores.Timber += settlement.MaterialStores.Timber;
            existing.MaterialStores.Stone += settlement.MaterialStores.Stone;
            existing.MaterialStores.Fiber += settlement.MaterialStores.Fiber;
            existing.MaterialStores.Clay += settlement.MaterialStores.Clay;
            existing.MaterialStores.Hides += settlement.MaterialStores.Hides;
            existing.MaterialSupport = Math.Max(existing.MaterialSupport, settlement.MaterialSupport);
            existing.MaterialShortageMonths = Math.Max(existing.MaterialShortageMonths, settlement.MaterialShortageMonths);
            existing.MaterialSurplusMonths = Math.Max(existing.MaterialSurplusMonths, settlement.MaterialSurplusMonths);
        }

        foreach (var presence in weaker.RegionalPresences)
        {
            if (!stronger.RegionalPresences.Any(existing => string.Equals(existing.RegionId, presence.RegionId, StringComparison.Ordinal)))
            {
                stronger.RegionalPresences.Add(presence.Clone());
            }
        }

        stronger.MaterialStores.Timber += weaker.MaterialStores.Timber;
        stronger.MaterialStores.Stone += weaker.MaterialStores.Stone;
        stronger.MaterialStores.Fiber += weaker.MaterialStores.Fiber;
        stronger.MaterialStores.Clay += weaker.MaterialStores.Clay;
        stronger.MaterialStores.Hides += weaker.MaterialStores.Hides;

        RecordHistory(stronger, weaker.Id, weaker.Name, PoliticalHistoryKind.AbsorbedPolity, world.CurrentYear, world.CurrentMonth, $"{weaker.Name} was absorbed into the {stronger.Name} center.");
        stronger.PoliticalAttachments.RemoveAll(attachment => string.Equals(attachment.RelatedPolityId, weaker.Id, StringComparison.Ordinal));
        polities.Remove(weaker.Id);

        foreach (var polity in polities.Values)
        {
            polity.InterPolityRelations.RemoveAll(relation => string.Equals(relation.OtherPolityId, weaker.Id, StringComparison.Ordinal));
            foreach (var attachment in polity.PoliticalAttachments.Where(attachment => string.Equals(attachment.RelatedPolityId, weaker.Id, StringComparison.Ordinal)))
            {
                attachment.IsActive = false;
            }
        }

        changes.Add(new PoliticalScaleChange
        {
            PolityId = stronger.Id,
            PolityName = stronger.Name,
            OtherPolityId = weaker.Id,
            OtherPolityName = weaker.Name,
            Kind = "direct-integration",
            Message = $"{stronger.Name} directly integrated {weaker.Name} into the larger realm."
        });
    }

    private static void BindAttachment(
        World world,
        Polity stronger,
        Polity weaker,
        PoliticalAttachmentKind kind,
        ICollection<PoliticalScaleChange> changes)
    {
        var existing = stronger.PoliticalAttachments.FirstOrDefault(attachment => string.Equals(attachment.RelatedPolityId, weaker.Id, StringComparison.Ordinal));
        if (existing is null)
        {
            stronger.PoliticalAttachments.Add(new PoliticalAttachment
            {
                RelatedPolityId = weaker.Id,
                Kind = kind,
                EstablishedYear = world.CurrentYear,
                EstablishedMonth = world.CurrentMonth,
                IntegrationDepth = kind == PoliticalAttachmentKind.Subordinate ? 48 : 40,
                Loyalty = kind == PoliticalAttachmentKind.Subordinate ? 44 : 55,
                IsActive = true,
                RegionFocusId = weaker.CoreRegionId
            });
        }
        else
        {
            existing.Kind = kind;
            existing.IsActive = true;
        }

        weaker.ParentPolityId = stronger.Id;
        RecordHistory(stronger, weaker.Id, weaker.Name, kind == PoliticalAttachmentKind.Subordinate ? PoliticalHistoryKind.BoundSubordinate : PoliticalHistoryKind.BoundFederation, world.CurrentYear, world.CurrentMonth, BuildAttachmentSummary(stronger.Name, weaker.Name, kind));
        RecordHistory(weaker, stronger.Id, stronger.Name, PoliticalHistoryKind.Successor, world.CurrentYear, world.CurrentMonth, kind == PoliticalAttachmentKind.Subordinate ? $"{weaker.Name} fell under the {stronger.Name} center." : $"{weaker.Name} attached loosely to the {stronger.Name} center.");

        changes.Add(new PoliticalScaleChange
        {
            PolityId = stronger.Id,
            PolityName = stronger.Name,
            OtherPolityId = weaker.Id,
            OtherPolityName = weaker.Name,
            Kind = kind == PoliticalAttachmentKind.Subordinate ? "subordinate-bound" : "federated-bound",
            Message = BuildAttachmentChronicle(stronger.Name, weaker.Name, kind)
        });
    }

    private static void ReleaseFromParent(
        World world,
        Polity polity,
        IDictionary<string, Polity> polities,
        ICollection<PoliticalScaleChange> changes)
    {
        if (!polities.TryGetValue(polity.ParentPolityId, out var parent))
        {
            polity.ParentPolityId = string.Empty;
            return;
        }

        foreach (var attachment in parent.PoliticalAttachments.Where(attachment => string.Equals(attachment.RelatedPolityId, polity.Id, StringComparison.Ordinal)))
        {
            attachment.IsActive = false;
        }

        RecordHistory(parent, polity.Id, polity.Name, PoliticalHistoryKind.Breakaway, world.CurrentYear, world.CurrentMonth, $"{polity.Name} broke away from the {parent.Name} center.");
        RecordHistory(polity, parent.Id, parent.Name, PoliticalHistoryKind.Independence, world.CurrentYear, world.CurrentMonth, $"{polity.Name} broke free after long integration strain.");
        polity.ParentPolityId = string.Empty;

        changes.Add(new PoliticalScaleChange
        {
            PolityId = polity.Id,
            PolityName = polity.Name,
            OtherPolityId = parent.Id,
            OtherPolityName = parent.Name,
            Kind = "independence",
            Message = $"{polity.Name} broke away after years of poor integration."
        });
    }

    private static void CreateBreakaway(
        World world,
        Polity parent,
        IDictionary<string, Polity> polities,
        List<PopulationGroup> groups,
        IReadOnlyDictionary<string, Region> regionsById,
        ICollection<PoliticalScaleChange> changes)
    {
        var candidateSettlement = parent.Settlements
            .Where(settlement => settlement.IsActive && !string.Equals(settlement.RegionId, parent.CoreRegionId, StringComparison.Ordinal))
            .OrderByDescending(settlement => ResolveDistance(parent.CoreRegionId, settlement.RegionId, regionsById))
            .ThenBy(settlement => settlement.MaterialSupport)
            .ThenBy(settlement => settlement.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        if (candidateSettlement is null)
        {
            return;
        }

        var regionId = candidateSettlement.RegionId;
        var breakawayGroups = groups.Where(group => string.Equals(group.PolityId, parent.Id, StringComparison.Ordinal) && string.Equals(group.CurrentRegionId, regionId, StringComparison.Ordinal)).ToArray();
        if (breakawayGroups.Length == 0)
        {
            return;
        }

        var regionName = regionsById.GetValueOrDefault(regionId)?.Name ?? regionId;
        var newPolityId = BuildBreakawayPolityId(polities.Keys, regionId);
        var newPolityName = $"{regionName} Breakaway";
        var newMemberIds = new List<string>();

        for (var index = 0; index < groups.Count; index++)
        {
            var group = groups[index];
            if (!string.Equals(group.PolityId, parent.Id, StringComparison.Ordinal) ||
                !string.Equals(group.CurrentRegionId, regionId, StringComparison.Ordinal))
            {
                continue;
            }

            groups[index] = CloneGroup(group, newPolityId);
            newMemberIds.Add(group.Id);
        }

        var movedSettlements = parent.Settlements
            .Where(settlement => settlement.IsActive && string.Equals(settlement.RegionId, regionId, StringComparison.Ordinal))
            .ToArray();
        parent.Settlements.RemoveAll(settlement => settlement.IsActive && string.Equals(settlement.RegionId, regionId, StringComparison.Ordinal));
        var movedPresence = parent.RegionalPresences
            .Where(presence => string.Equals(presence.RegionId, regionId, StringComparison.Ordinal))
            .ToArray();
        parent.RegionalPresences.RemoveAll(presence => string.Equals(presence.RegionId, regionId, StringComparison.Ordinal));

        var newPolity = new Polity
        {
            Id = newPolityId,
            Name = newPolityName,
            GovernmentForm = parent.GovernmentForm,
            MemberGroupIds = [.. newMemberIds],
            HomeRegionId = regionId,
            CoreRegionId = regionId,
            PrimarySettlementId = movedSettlements.FirstOrDefault(settlement => settlement.IsPrimary)?.Id ?? movedSettlements.FirstOrDefault()?.Id ?? string.Empty,
            AnchoringKind = PolityAnchoringKind.Anchored,
            ActiveLawProposal = null,
            LawProposalHistory = [],
            EnactedLaws = parent.EnactedLaws.Where(law => law.IsActive).Select(law => law.Clone()).ToList(),
            PoliticalBlocs = PoliticalBlocCatalog.CreateInitialBlocs(parent.GovernmentForm).ToList(),
            Settlements = movedSettlements.Select(settlement => CloneSettlementForPolity(settlement, newPolityId)).ToList(),
            RegionalPresences = movedPresence.Select(presence => presence.Clone()).ToList(),
            InterPolityRelations = [],
            ParentPolityId = string.Empty,
            PoliticalAttachments = [],
            PoliticalHistory = [],
            Governance = new GovernanceState
            {
                Legitimacy = Math.Clamp(parent.Governance.Legitimacy - 8, 0, 100),
                Cohesion = Math.Clamp(parent.Governance.Cohesion - 6, 0, 100),
                Authority = Math.Clamp(parent.Governance.Authority - 12, 0, 100),
                Governability = Math.Clamp(parent.Governance.Governability - 10, 0, 100),
                PeripheralStrain = 18
            },
            ExternalPressure = new ExternalPressureState(),
            ScaleState = new PoliticalScaleState { Form = PoliticalScaleForm.RegionalState, FragmentationRisk = 35, Summary = "A breakaway regional state is consolidating." },
            SocialMemory = parent.SocialMemory.Clone(),
            SocialIdentity = parent.SocialIdentity.Clone(),
            MaterialStores = SplitStores(parent.MaterialStores, 0.35),
            MaterialProduction = new MaterialProductionState(),
            MaterialShortageMonths = Math.Max(0, parent.MaterialShortageMonths - 1),
            MaterialSurplusMonths = 0
        };

        newPolity.SocialIdentity.AutonomyOrientation = Math.Clamp(newPolity.SocialIdentity.AutonomyOrientation + 12, 0, 100);
        newPolity.SocialIdentity.FrontierDistinctiveness = Math.Clamp(newPolity.SocialIdentity.FrontierDistinctiveness + 10, 0, 100);
        RecordHistory(parent, newPolity.Id, newPolity.Name, PoliticalHistoryKind.Breakaway, world.CurrentYear, world.CurrentMonth, $"{newPolity.Name} broke away from the overstretched {parent.Name} realm.");
        RecordHistory(newPolity, parent.Id, parent.Name, PoliticalHistoryKind.Successor, world.CurrentYear, world.CurrentMonth, $"{newPolity.Name} emerged as a successor on the periphery.");

        polities[newPolityId] = newPolity;
        changes.Add(new PoliticalScaleChange
        {
            PolityId = newPolity.Id,
            PolityName = newPolity.Name,
            OtherPolityId = parent.Id,
            OtherPolityName = parent.Name,
            Kind = "breakaway",
            Message = $"{newPolity.Name} broke away after years of poor integration."
        });
    }

    private static void CleanupAttachments(Polity polity, IReadOnlyDictionary<string, Polity> polities)
    {
        polity.PoliticalAttachments.RemoveAll(attachment => string.IsNullOrWhiteSpace(attachment.RelatedPolityId));
        foreach (var attachment in polity.PoliticalAttachments)
        {
            if (!polities.ContainsKey(attachment.RelatedPolityId))
            {
                attachment.IsActive = false;
            }
        }
    }

    private static PolityContext? BuildContext(World world, IEnumerable<Polity> polities, IReadOnlyList<PopulationGroup> groups, Polity polity)
    {
        return PolityData.BuildContext(new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, groups, world.Chronicle, polities.ToArray(), world.FocalPolityId), polity);
    }

    private static void RebuildMemberGroupIds(IEnumerable<Polity> polities, IReadOnlyList<PopulationGroup> groups)
    {
        var idsByPolity = groups
            .GroupBy(group => group.PolityId, StringComparer.Ordinal)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.Select(group => group.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        foreach (var polity in polities)
        {
            polity.MemberGroupIds.Clear();
            if (idsByPolity.TryGetValue(polity.Id, out var groupIds))
            {
                polity.MemberGroupIds.AddRange(groupIds);
            }
        }
    }

    private static int ResolveCapacityScore(PolityContext context, int settlementCount, int regionCount, int attachmentCount)
    {
        return Math.Clamp(
            (context.TotalPopulation / 3) +
            (settlementCount * 18) +
            (regionCount * 14) +
            (attachmentCount * 12) +
            (context.MaterialProduction.ResilienceBonus * 3) +
            (context.MaterialProduction.StorageSupport / 2) +
            (context.Governance.Governability / 2) +
            (context.Governance.Authority / 3),
            0,
            400);
    }

    private static PoliticalScaleForm ResolveForm(
        int capacity,
        int settlementCount,
        int regionCount,
        int attachmentCount,
        int integrationDepth,
        int overextension,
        int fragmentationRisk)
    {
        if (fragmentationRisk >= 72 && (settlementCount >= 3 || attachmentCount >= 1))
        {
            return PoliticalScaleForm.Fragmenting;
        }

        if (capacity >= 220 && regionCount >= 5 && attachmentCount >= 2 && integrationDepth >= 50 && overextension < 70)
        {
            return PoliticalScaleForm.EmpireLike;
        }

        if (capacity >= 155 && regionCount >= 4 && (attachmentCount >= 1 || integrationDepth < 55))
        {
            return PoliticalScaleForm.CompositeRealm;
        }

        if (capacity >= 140 && settlementCount >= 3 && regionCount >= 3 && integrationDepth >= 52)
        {
            return PoliticalScaleForm.KingdomRealm;
        }

        if (capacity >= 90 && (settlementCount >= 2 || regionCount >= 2))
        {
            return PoliticalScaleForm.RegionalState;
        }

        return PoliticalScaleForm.LocalPolity;
    }

    private static DistanceProfile CalculateDistanceProfile(string coreRegionId, IReadOnlyList<string> regionIds, IReadOnlyDictionary<string, Region> regionsById)
    {
        if (string.IsNullOrWhiteSpace(coreRegionId) || regionIds.Count == 0 || !regionsById.ContainsKey(coreRegionId))
        {
            return new DistanceProfile(0, 0);
        }

        var distances = new Dictionary<string, int>(StringComparer.Ordinal) { [coreRegionId] = 0 };
        var pending = new Queue<string>();
        pending.Enqueue(coreRegionId);
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            foreach (var neighborId in regionsById[current].NeighborIds)
            {
                if (distances.ContainsKey(neighborId))
                {
                    continue;
                }

                distances[neighborId] = distances[current] + 1;
                pending.Enqueue(neighborId);
            }
        }

        var values = regionIds
            .Select(regionId => distances.TryGetValue(regionId, out var distance) ? distance : 3)
            .ToArray();
        return new DistanceProfile(
            (int)Math.Round(values.Average(), MidpointRounding.AwayFromZero),
            values.Length == 0 ? 0 : values.Max());
    }

    private static int ResolveScaleStrength(PolityContext context)
    {
        return ResolveCapacityScore(
            context,
            context.Polity.Settlements.Count(settlement => settlement.IsActive),
            context.Polity.RegionalPresences.Count(presence => presence.IsCurrent),
            context.Polity.PoliticalAttachments.Count(attachment => attachment.IsActive));
    }

    private static bool CanDirectIntegrate(PolityContext stronger, PolityContext weaker, InterPolityRelation relation, int strengthGap)
    {
        return stronger.ScaleState.IntegrationDepth >= 55 &&
               stronger.ScaleState.Form is not PoliticalScaleForm.LocalPolity &&
               weaker.TotalPopulation <= stronger.TotalPopulation &&
               weaker.Polity.Settlements.Count(settlement => settlement.IsActive) <= 2 &&
               (relation.Stance is InterPolityStance.OpenConflict or InterPolityStance.RaidingConflict or InterPolityStance.Hostile) &&
               weaker.Governance.Governability <= 48 &&
               strengthGap >= 70;
    }

    private static bool CanBindSubordinate(PolityContext stronger, PolityContext weaker, InterPolityRelation relation, int strengthGap)
    {
        return stronger.ScaleState.Form is not PoliticalScaleForm.LocalPolity &&
               stronger.ScaleState.IntegrationDepth >= 45 &&
               weaker.Governance.Legitimacy <= 58 &&
               (relation.Stance is InterPolityStance.Hostile or InterPolityStance.RaidingConflict or InterPolityStance.OpenConflict or InterPolityStance.Rival) &&
               strengthGap >= 55;
    }

    private static bool CanBindFederated(PolityContext stronger, PolityContext weaker, InterPolityRelation relation)
    {
        return stronger.ScaleState.Form is not PoliticalScaleForm.LocalPolity &&
               stronger.ScaleState.IntegrationDepth >= 42 &&
               relation.Stance is InterPolityStance.Cooperative or InterPolityStance.UneasyPeace &&
               relation.Cooperation >= 58 &&
               relation.Trust >= 52;
    }

    private static bool HasLaw(Polity polity, string definitionId)
    {
        return polity.EnactedLaws.Any(law => law.IsActive && string.Equals(law.DefinitionId, definitionId, StringComparison.Ordinal));
    }

    private static void RecordHistory(Polity polity, string otherPolityId, string otherPolityName, PoliticalHistoryKind kind, int year, int month, string summary)
    {
        polity.PoliticalHistory.Add(new PoliticalHistoryRecord
        {
            RelatedPolityId = otherPolityId,
            RelatedPolityName = otherPolityName,
            Kind = kind,
            Year = year,
            Month = month,
            Summary = summary
        });
    }

    private static string BuildAttachmentSummary(string strongerName, string weakerName, PoliticalAttachmentKind kind)
    {
        return kind switch
        {
            PoliticalAttachmentKind.Subordinate => $"{weakerName} was bound under the {strongerName} center.",
            PoliticalAttachmentKind.FederatedAttachment => $"{weakerName} attached loosely to the {strongerName} center.",
            _ => $"{weakerName} was attached to the {strongerName} center."
        };
    }

    private static string BuildAttachmentChronicle(string strongerName, string weakerName, PoliticalAttachmentKind kind)
    {
        return kind switch
        {
            PoliticalAttachmentKind.Subordinate => $"{weakerName} was bound loosely to the {strongerName} center.",
            PoliticalAttachmentKind.FederatedAttachment => $"{weakerName} joined the {strongerName} center in loose attachment.",
            _ => $"{weakerName} attached to the {strongerName} center."
        };
    }

    private static string BuildScaleSummary(PoliticalScaleForm form, PoliticalScaleState state)
    {
        return form switch
        {
            PoliticalScaleForm.LocalPolity => "A compact local polity still rules close to its core.",
            PoliticalScaleForm.RegionalState => "A wider regional state is holding together around a stronger core.",
            PoliticalScaleForm.KingdomRealm => "A larger kingdom-like realm is coordinating several regions under one center.",
            PoliticalScaleForm.CompositeRealm => "A composite realm is managing a wider and less uniform political structure.",
            PoliticalScaleForm.EmpireLike => "An expansive empire-like state is projecting rule across a broad reach.",
            PoliticalScaleForm.Fragmenting => state.FragmentationRisk >= 80
                ? "The realm is straining toward fragmentation."
                : "The wider state is showing visible structural cracks.",
            _ => "Political scale remains limited."
        };
    }

    private static int ResolveDistance(string coreRegionId, string otherRegionId, IReadOnlyDictionary<string, Region> regionsById)
    {
        return CalculateDistanceProfile(coreRegionId, [otherRegionId], regionsById).MaxDistance;
    }

    private static string BuildBreakawayPolityId(IEnumerable<string> existingIds, string regionId)
    {
        var baseId = $"polity-breakaway-{regionId}";
        var next = 1;
        var existing = existingIds.ToHashSet(StringComparer.Ordinal);
        var candidate = baseId;
        while (existing.Contains(candidate))
        {
            candidate = $"{baseId}-{next++:D2}";
        }

        return candidate;
    }

    private static void PurgeDeadReferences(IEnumerable<Polity> polities)
    {
        var activeIds = polities.Select(polity => polity.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var polity in polities)
        {
            polity.InterPolityRelations.RemoveAll(relation => !activeIds.Contains(relation.OtherPolityId));
            polity.PoliticalAttachments.RemoveAll(attachment => !activeIds.Contains(attachment.RelatedPolityId) && attachment.IsActive);
            if (!string.IsNullOrWhiteSpace(polity.ParentPolityId) && !activeIds.Contains(polity.ParentPolityId))
            {
                polity.ParentPolityId = string.Empty;
            }
        }
    }

    private static Settlement CloneSettlementForPolity(Settlement settlement, string polityId)
    {
        return new Settlement
        {
            Id = settlement.Id,
            Name = settlement.Name,
            PolityId = polityId,
            RegionId = settlement.RegionId,
            Type = settlement.Type,
            FoundedYear = settlement.FoundedYear,
            FoundedMonth = settlement.FoundedMonth,
            IsActive = settlement.IsActive,
            IsPrimary = settlement.IsPrimary,
            StoredFood = settlement.StoredFood,
            MaterialStores = settlement.MaterialStores.Clone(),
            MaterialSupport = settlement.MaterialSupport,
            MaterialShortageMonths = settlement.MaterialShortageMonths,
            MaterialSurplusMonths = settlement.MaterialSurplusMonths
        };
    }

    private static PopulationGroup CloneGroup(PopulationGroup group, string? polityIdOverride = null)
    {
        return new PopulationGroup
        {
            Id = group.Id,
            Name = group.Name,
            SpeciesId = group.SpeciesId,
            PolityId = polityIdOverride ?? group.PolityId,
            CurrentRegionId = group.CurrentRegionId,
            OriginRegionId = group.OriginRegionId,
            Population = group.Population,
            StoredFood = group.StoredFood,
            HungerPressure = group.HungerPressure,
            ShortageMonths = group.ShortageMonths,
            FoodStressState = group.FoodStressState,
            SubsistencePreference = group.SubsistencePreference,
            SubsistenceMode = group.SubsistenceMode,
            Pressures = group.Pressures.Clone(),
            LastRegionId = group.LastRegionId,
            MonthsSinceLastMove = group.MonthsSinceLastMove,
            KnownRegionIds = new HashSet<string>(group.KnownRegionIds, StringComparer.Ordinal),
            KnownDiscoveryIds = new HashSet<string>(group.KnownDiscoveryIds, StringComparer.Ordinal),
            DiscoveryEvidence = group.DiscoveryEvidence.Clone(),
            LearnedAdvancementIds = new HashSet<string>(group.LearnedAdvancementIds, StringComparer.Ordinal),
            AdvancementEvidence = group.AdvancementEvidence.Clone()
        };
    }

    private static MaterialStockpile SplitStores(MaterialStockpile source, double fraction)
    {
        var moved = new MaterialStockpile
        {
            Timber = (int)Math.Round(source.Timber * fraction, MidpointRounding.AwayFromZero),
            Stone = (int)Math.Round(source.Stone * fraction, MidpointRounding.AwayFromZero),
            Fiber = (int)Math.Round(source.Fiber * fraction, MidpointRounding.AwayFromZero),
            Clay = (int)Math.Round(source.Clay * fraction, MidpointRounding.AwayFromZero),
            Hides = (int)Math.Round(source.Hides * fraction, MidpointRounding.AwayFromZero)
        };

        source.Timber = Math.Max(0, source.Timber - moved.Timber);
        source.Stone = Math.Max(0, source.Stone - moved.Stone);
        source.Fiber = Math.Max(0, source.Fiber - moved.Fiber);
        source.Clay = Math.Max(0, source.Clay - moved.Clay);
        source.Hides = Math.Max(0, source.Hides - moved.Hides);
        return moved;
    }

    private static int DriftToward(int current, int target)
    {
        return Math.Clamp((current * 2 + target) / 3, 0, 100);
    }

    private static InterPolitySummary SummarizeInterPolity(Polity polity)
    {
        return new InterPolitySummary(
            polity.InterPolityRelations.Sum(relation => relation.RaidsInflicted),
            polity.InterPolityRelations.Sum(relation => relation.RaidsSuffered));
    }

    private sealed record DistanceProfile(int AverageDistance, int MaxDistance);

    private sealed record ScaleMetrics(
        PoliticalScaleForm Form,
        int Centralization,
        int IntegrationDepth,
        int AutonomyTolerance,
        int CoordinationStrain,
        int DistanceStrain,
        int CompositeComplexity,
        int OverextensionPressure,
        int FragmentationRisk,
        bool ExternalSuccess,
        bool IntegrationSuccess);

    private sealed record InterPolitySummary(int RaidsInflicted, int RaidsSuffered);
}
