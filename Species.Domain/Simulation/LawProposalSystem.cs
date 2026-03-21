using Species.Domain.Catalogs;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class LawProposalSystem
{
    private static readonly IReadOnlyDictionary<GovernmentForm, GovernmentFormProposalBehavior> Behaviors =
        GovernmentFormLawBehaviorCatalog.Behaviors;

    private static readonly IReadOnlySet<GovernmentForm> AllGovernmentForms =
        Enum.GetValues<GovernmentForm>().ToHashSet();

    private static readonly IReadOnlyList<LawProposalDefinition> Definitions =
    [
        new()
        {
            Id = GovernanceLawCatalog.CentralizeStoresId,
            Title = "Centralize Stores",
            Summary = "Core authority would direct shared food and material stores from the main sites.",
            IntentSummary = "Shared stores would be gathered under central direction.",
            TradeoffSummary = "Improves emergency coordination, but raises local resentment and frontier strain.",
            Category = LawProposalCategory.Food,
            ConflictGroup = LawConflictGroup.Food,
            ConflictSlot = "store-control",
            ImpactScale = 3,
            GovernmentForms = AllGovernmentForms,
            ConflictingDefinitionIds = [GovernanceLawCatalog.LocalStoreAutonomyId],
            Score = (_, _, context) => ScoreWhen(
                context.Polity.Settlements.Count(settlement => settlement.IsActive) >= 2,
                18 + (context.MaterialShortageMonths * 12) + (context.MaterialProduction.DeficitScore / 2) +
                (context.Governance.Authority / 6) + (context.ScaleState.Centralization / 10) + (context.TotalStoredFood <= Math.Max(1, context.TotalPopulation / 2) ? 12 : 0))
        },
        new()
        {
            Id = GovernanceLawCatalog.LocalStoreAutonomyId,
            Title = "Affirm Local Store Autonomy",
            Summary = "Settlements would retain broader control over their own stores and distribution.",
            IntentSummary = "Local sites would keep wider authority over stores and relief.",
            TradeoffSummary = "Eases frontier resentment and can lift legitimacy, but weakens central coordination.",
            Category = LawProposalCategory.Custom,
            ConflictGroup = LawConflictGroup.Food,
            ConflictSlot = "store-control",
            ImpactScale = 2,
            GovernmentForms = AllGovernmentForms,
            ConflictingDefinitionIds = [GovernanceLawCatalog.CentralizeStoresId],
            Score = (_, _, context) => ScoreWhen(
                context.Polity.Settlements.Count(settlement => settlement.IsActive) >= 2,
                14 + (context.Governance.PeripheralStrain / 2) + ((100 - context.Governance.Legitimacy) / 3) +
                ((100 - context.Governance.Cohesion) / 4) + (context.ScaleState.AutonomyTolerance / 10))
        },
        new()
        {
            Id = GovernanceLawCatalog.ExtractionObligationId,
            Title = "Impose Extraction Obligation",
            Summary = "Settlements and camps would be pressed to contribute more labor and material output.",
            IntentSummary = "Regional extraction would be pushed harder to reinforce the polity.",
            TradeoffSummary = "Strengthens stores and tools when capacity exists, but can erode legitimacy and compliance.",
            Category = LawProposalCategory.Order,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "extraction-burden",
            ImpactScale = 3,
            GovernmentForms = AllGovernmentForms,
            ConflictingDefinitionIds = [GovernanceLawCatalog.EaseExtractionBurdenId],
            Score = (_, _, context) => ScoreWhen(
                context.MaterialProduction.DeficitScore >= 30 || context.Pressures.Threat.EffectiveValue >= 45,
                20 + (context.MaterialProduction.DeficitScore / 2) + (context.Pressures.Threat.EffectiveValue / 4) +
                (context.Governance.Authority / 7) + (context.ScaleState.OverextensionPressure / 12))
        },
        new()
        {
            Id = GovernanceLawCatalog.EaseExtractionBurdenId,
            Title = "Ease Extraction Burden",
            Summary = "Extraction obligations would be lightened to reduce strain on local communities.",
            IntentSummary = "Local extraction demands would be relaxed for stability.",
            TradeoffSummary = "Reduces resistance and improves cohesion, but slows stockpile recovery.",
            Category = LawProposalCategory.Custom,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "extraction-burden",
            ImpactScale = 2,
            GovernmentForms = AllGovernmentForms,
            ConflictingDefinitionIds = [GovernanceLawCatalog.ExtractionObligationId],
            Score = (_, _, context) => ScoreWhen(
                context.Governance.Legitimacy <= 55 || context.Governance.PeripheralStrain >= 35,
                14 + ((100 - context.Governance.Legitimacy) / 3) + (context.Governance.PeripheralStrain / 2) +
                (context.MaterialShortageMonths >= 2 ? 6 : 0) + (context.ScaleState.FragmentationRisk / 12))
        },
        new()
        {
            Id = GovernanceLawCatalog.CrisisMovementRestrictionId,
            Title = "Restrict Crisis Movement",
            Summary = "Movement would be curtailed during instability so the core can hold people and stores together.",
            IntentSummary = "Movement would be tightened to preserve order in crisis.",
            TradeoffSummary = "Can reduce immediate disorder, but harms legitimacy and frontier compliance.",
            Category = LawProposalCategory.Movement,
            ConflictGroup = LawConflictGroup.Movement,
            ConflictSlot = "mobility-order",
            ImpactScale = 3,
            GovernmentForms = AllGovernmentForms,
            ConflictingDefinitionIds = [GovernanceLawCatalog.OpenMovementId],
            Score = (_, _, context) => ScoreWhen(
                context.Pressures.Migration.EffectiveValue >= 40 || context.Pressures.Threat.EffectiveValue >= 45,
                18 + (context.Pressures.Migration.EffectiveValue / 2) + (context.Pressures.Threat.EffectiveValue / 4) +
                (context.Governance.Authority / 8) + (context.ScaleState.Centralization / 12))
        },
        new()
        {
            Id = GovernanceLawCatalog.OpenMovementId,
            Title = "Protect Open Movement",
            Summary = "Routes would remain open and movement restrictions would be narrowed even under strain.",
            IntentSummary = "Movement and route access would be preserved across the polity.",
            TradeoffSummary = "Supports frontier life and legitimacy, but limits tight crisis control.",
            Category = LawProposalCategory.Movement,
            ConflictGroup = LawConflictGroup.Movement,
            ConflictSlot = "mobility-order",
            ImpactScale = 2,
            GovernmentForms = AllGovernmentForms,
            ConflictingDefinitionIds = [GovernanceLawCatalog.CrisisMovementRestrictionId],
            Score = (_, _, context) => ScoreWhen(
                context.Polity.RegionalPresences.Count(presence => presence.IsCurrent) >= 2,
                12 + (context.Governance.PeripheralStrain / 2) + ((100 - context.Governance.Legitimacy) / 4) +
                (context.Pressures.Migration.EffectiveValue / 4) + (context.ScaleState.AutonomyTolerance / 12))
        },
        new()
        {
            Id = GovernanceLawCatalog.StrengthenCentralAuthorityId,
            Title = "Strengthen Central Authority",
            Summary = "Authority would be concentrated more tightly around the core leadership.",
            IntentSummary = "Decision-making would be concentrated so orders land more clearly.",
            TradeoffSummary = "Raises authority and enforcement, but risks bloc resentment and legitimacy loss.",
            Category = LawProposalCategory.Order,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "authority-structure",
            ImpactScale = 3,
            GovernmentForms = AllGovernmentForms,
            ConflictingDefinitionIds = [GovernanceLawCatalog.SharedGovernanceId],
            Score = (_, _, context) => ScoreWhen(
                context.Pressures.Threat.EffectiveValue >= 35 || context.Governance.Authority <= 55,
                16 + (context.Pressures.Threat.EffectiveValue / 4) + ((100 - context.Governance.Authority) / 3) +
                (context.MaterialShortageMonths >= 2 ? 5 : 0) + (context.ScaleState.Centralization / 10))
        },
        new()
        {
            Id = GovernanceLawCatalog.SharedGovernanceId,
            Title = "Affirm Shared Governance",
            Summary = "Councils and local voices would gain a stronger hand in law and coordination.",
            IntentSummary = "Authority would be shared more broadly across the polity.",
            TradeoffSummary = "Raises legitimacy and cohesion, but lowers central command and slows harsh measures.",
            Category = LawProposalCategory.Custom,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "authority-structure",
            ImpactScale = 2,
            GovernmentForms = AllGovernmentForms,
            ConflictingDefinitionIds = [GovernanceLawCatalog.StrengthenCentralAuthorityId],
            Score = (_, _, context) => ScoreWhen(
                context.Governance.Legitimacy <= 60 || context.Governance.Cohesion <= 60,
                16 + ((100 - context.Governance.Legitimacy) / 3) + ((100 - context.Governance.Cohesion) / 3) + (context.ScaleState.FragmentationRisk / 14))
        },
        new()
        {
            Id = GovernanceLawCatalog.FrontierIntegrationId,
            Title = "Adopt Frontier Integration Charter",
            Summary = "Peripheral settlements would be tied in more deliberately through obligations and recognition.",
            IntentSummary = "Core and frontier ties would be formalized to steady the wider polity.",
            TradeoffSummary = "Can improve cohesion and reduce frontier drift, but costs central stores and attention.",
            Category = LawProposalCategory.Custom,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "frontier-relationship",
            ImpactScale = 2,
            GovernmentForms = AllGovernmentForms,
            Score = (_, _, context) => ScoreWhen(
                context.Polity.Settlements.Count(settlement => settlement.IsActive) >= 2 ||
                context.Polity.RegionalPresences.Count(presence => presence.IsCurrent) >= 3,
                15 + (context.Governance.PeripheralStrain / 2) + ((100 - context.Governance.Cohesion) / 4) +
                (context.Polity.Settlements.Count(settlement => settlement.IsActive) * 3) + (context.ScaleState.CompositeComplexity / 10))
        }
    ];

    public (World World, IReadOnlyList<LawProposalChange> Changes) Run(World world, string playerPolityId)
    {
        if (world.Polities.Count == 0)
        {
            return (world, Array.Empty<LawProposalChange>());
        }

        var focusPolity = world.Polities.FirstOrDefault(polity => string.Equals(polity.Id, playerPolityId, StringComparison.Ordinal));
        if (focusPolity is null)
        {
            return (world, Array.Empty<LawProposalChange>());
        }

        var updatedPolity = focusPolity.Clone();
        var context = PolityData.BuildContext(world, updatedPolity);
        if (context is null)
        {
            return (world, Array.Empty<LawProposalChange>());
        }

        var aggregateGroup = BuildAggregateGroup(context);
        var region = ResolveRegion(world, context);
        if (region is null)
        {
            return (world, Array.Empty<LawProposalChange>());
        }

        if (updatedPolity.ActiveLawProposal is null)
        {
            var generated = GenerateProposal(updatedPolity, context, aggregateGroup, region);
            if (generated is null)
            {
                return (world, Array.Empty<LawProposalChange>());
            }

            updatedPolity.ActiveLawProposal = generated;
            return (ReplacePolity(world, updatedPolity), Array.Empty<LawProposalChange>());
        }

        var activeProposal = updatedPolity.ActiveLawProposal.Clone();
        activeProposal.AgeInMonths++;
        activeProposal.IgnoredMonths++;

        var definition = Definitions.FirstOrDefault(item => string.Equals(item.Id, activeProposal.DefinitionId, StringComparison.Ordinal));
        if (definition is null)
        {
            updatedPolity.ActiveLawProposal = null;
            return (ReplacePolity(world, updatedPolity), Array.Empty<LawProposalChange>());
        }

        var behavior = Behaviors[updatedPolity.GovernmentForm];
        var relevance = definition.Score(aggregateGroup, region, context);
        UpdateMomentum(activeProposal, behavior, context, relevance);

        var ignoredResolution = ResolveIgnoredProposal(activeProposal, behavior, context, relevance);
        if (ignoredResolution is not null)
        {
            var changes = FinalizeProposal(world, updatedPolity, activeProposal, ignoredResolution.Value);
            return (ReplacePolity(world, updatedPolity), changes);
        }

        updatedPolity.ActiveLawProposal = activeProposal;
        return (ReplacePolity(world, updatedPolity), Array.Empty<LawProposalChange>());
    }

    public (World World, IReadOnlyList<LawProposalChange> Changes) ResolvePlayerDecision(
        World world,
        string playerPolityId,
        LawProposalStatus status)
    {
        var focusPolity = world.Polities.FirstOrDefault(polity => string.Equals(polity.Id, playerPolityId, StringComparison.Ordinal));
        if (focusPolity?.ActiveLawProposal is null)
        {
            return (world, Array.Empty<LawProposalChange>());
        }

        var updatedPolity = focusPolity.Clone();
        var activeProposal = updatedPolity.ActiveLawProposal;
        if (activeProposal is null)
        {
            return (world, Array.Empty<LawProposalChange>());
        }

        var resolvedProposal = activeProposal.Clone();
        var changes = FinalizeProposal(world, updatedPolity, resolvedProposal, status);
        return (ReplacePolity(world, updatedPolity), changes);
    }

    private static Region? ResolveRegion(World world, PolityContext context)
    {
        return world.Regions.FirstOrDefault(region => string.Equals(region.Id, context.CoreRegionId, StringComparison.Ordinal)) ??
               world.Regions.FirstOrDefault(region => string.Equals(region.Id, context.CurrentRegionId, StringComparison.Ordinal));
    }

    private static LawProposal? GenerateProposal(Polity polity, PolityContext context, PopulationGroup aggregateGroup, Region region)
    {
        var behavior = Behaviors[polity.GovernmentForm];
        var candidate = Definitions
            .Where(definition => definition.GovernmentForms.Contains(polity.GovernmentForm))
            .Select(definition =>
            {
                var relevance = definition.Score(aggregateGroup, region, context);
                if (relevance == 0 || !IsEligibleByLawState(polity, definition) || IsBlockedByEnactedLaw(polity, definition))
                {
                    return new { Definition = definition, Relevance = 0, WeightedScore = 0 };
                }

                var weightedScore = relevance +
                    (behavior.GetCategoryWeight(definition.Category) * 3) +
                    ResolveEnactedLawModifier(polity, definition) +
                    ResolveBlocProposalModifier(polity, context, definition) +
                    ResolveSocialIdentityModifier(context, definition.Id) +
                    ResolveExternalPressureModifier(context, definition.Id) +
                    ResolveGovernmentBias(behavior, definition.Id) +
                    ((definition.ImpactScale - 1) * behavior.ExtremityAllowance * 2);
                return new { Definition = definition, Relevance = relevance, WeightedScore = weightedScore };
            })
            .Where(item => item.Relevance >= 35)
            .OrderByDescending(item => item.WeightedScore)
            .ThenByDescending(item => item.Relevance)
            .ThenBy(item => item.Definition.Title, StringComparer.Ordinal)
            .FirstOrDefault();

        if (candidate is null)
        {
            return null;
        }

        var backing = ResolveBackingSources(polity, context, behavior, candidate.Definition);
        var supportBase = 32 + (candidate.Relevance / 2) + (behavior.PlayerDecisionStrength * 2) - (candidate.Definition.ImpactScale * 5);
        supportBase += ResolveBackingSupportShift(polity, context, candidate.Definition, backing.Primary, backing.Secondary);
        supportBase += Math.Clamp((context.Governance.Legitimacy - 50) / 5, -6, 6);
        supportBase += ResolveProposalSupportIdentityShift(context, candidate.Definition.Id);

        var oppositionBase = 22 + ((100 - candidate.Relevance) / 4) + (candidate.Definition.ImpactScale * 8);
        oppositionBase += ResolveBlocOppositionShift(polity, context, candidate.Definition, backing.Primary, backing.Secondary);
        oppositionBase += Math.Clamp((context.Governance.PeripheralStrain - 25) / 6, 0, 10);
        oppositionBase += ResolveProposalOppositionIdentityShift(context, candidate.Definition.Id);

        var urgency = Math.Clamp(
            candidate.Relevance + (candidate.Definition.ImpactScale * 10) + (behavior.GetCategoryWeight(candidate.Definition.Category) * 2),
            10,
            100);

        return new LawProposal
        {
            Id = $"{polity.Id}:{candidate.Definition.Id}:{candidate.Relevance}",
            DefinitionId = candidate.Definition.Id,
            Title = candidate.Definition.Title,
            Summary = candidate.Definition.Summary,
            ReasonSummary = BuildReasonSummary(candidate.Definition.Id, context),
            TradeoffSummary = candidate.Definition.TradeoffSummary,
            Category = candidate.Definition.Category,
            Status = LawProposalStatus.Active,
            Support = Math.Clamp(supportBase, 5, 95),
            Opposition = Math.Clamp(oppositionBase, 5, 95),
            Urgency = urgency,
            AgeInMonths = 0,
            IgnoredMonths = 0,
            ImpactScale = candidate.Definition.ImpactScale,
            GovernmentForm = polity.GovernmentForm,
            PrimaryBackingSource = backing.Primary,
            SecondaryBackingSource = backing.Secondary
        };
    }

    private static void UpdateMomentum(LawProposal proposal, GovernmentFormProposalBehavior behavior, PolityContext context, int relevance)
    {
        var governanceDrag = Math.Max(0, 55 - context.Governance.Governability) / 6;
        proposal.Urgency = Math.Clamp(Math.Max(proposal.Urgency, relevance + (proposal.ImpactScale * 8) - governanceDrag), 0, 100);

        if (proposal.IgnoredMonths < 18)
        {
            return;
        }

        var supportDrift = (relevance - 50) / 10;
        var indecision = 1 + ((proposal.IgnoredMonths - 18) / 12) * behavior.IndecisionPenaltyStrength;
        proposal.Support = Math.Clamp(proposal.Support + supportDrift - governanceDrag, 0, 100);
        proposal.Opposition = Math.Clamp(proposal.Opposition + Math.Max(0, -supportDrift) + indecision + governanceDrag, 0, 100);
    }

    private static LawProposalStatus? ResolveIgnoredProposal(
        LawProposal proposal,
        GovernmentFormProposalBehavior behavior,
        PolityContext context,
        int relevance)
    {
        if (proposal.IgnoredMonths < 48)
        {
            return null;
        }

        var timePressure = 1 + ((proposal.IgnoredMonths - 48) / 6);
        var supportMargin = proposal.Support - proposal.Opposition;
        var governanceAssist = (context.Governance.Authority - 50) / 8;
        var passScore = supportMargin + (behavior.AutoPassBias * 6) + (relevance / 4) + timePressure + governanceAssist;
        var vetoScore = (-supportMargin) + (behavior.AutoVetoBias * 6) + ((100 - relevance) / 4) + timePressure;
        var abstainScore = (behavior.AbstainBias * 8) + Math.Abs(supportMargin / 4);

        if (passScore >= vetoScore && passScore >= abstainScore && passScore >= 28)
        {
            return LawProposalStatus.Passed;
        }

        if (vetoScore >= passScore && vetoScore >= abstainScore && vetoScore >= 28)
        {
            return LawProposalStatus.Vetoed;
        }

        if (abstainScore >= 16)
        {
            return LawProposalStatus.Abstained;
        }

        return null;
    }

    private static IReadOnlyList<LawProposalChange> FinalizeProposal(World world, Polity polity, LawProposal proposal, LawProposalStatus status)
    {
        proposal.Status = status;
        polity.ActiveLawProposal = null;
        polity.LawProposalHistory.Add(proposal);

        if (status == LawProposalStatus.Passed)
        {
            ApplyPassedLaw(world, polity, proposal);
        }

        if (status == LawProposalStatus.Abstained)
        {
            return Array.Empty<LawProposalChange>();
        }

        return
        [
            new LawProposalChange
            {
                GroupId = polity.Id,
                GroupName = polity.Name,
                ProposalTitle = proposal.Title,
                ChronicleLine = BuildLawChronicleLine(polity.Name, proposal, status),
                Status = status
            }
        ];
    }

    private static string BuildLawChronicleLine(string polityName, LawProposal proposal, LawProposalStatus status)
    {
        if (status == LawProposalStatus.Vetoed)
        {
            return $"{polityName} rejected {proposal.Title.ToLowerInvariant()}.";
        }

        return proposal.DefinitionId switch
        {
            GovernanceLawCatalog.CentralizeStoresId => $"{polityName} centralized its stores under emergency law.",
            GovernanceLawCatalog.LocalStoreAutonomyId => $"{polityName} left store control with local settlements.",
            GovernanceLawCatalog.ExtractionObligationId => $"{polityName} imposed stricter extraction obligations.",
            GovernanceLawCatalog.EaseExtractionBurdenId => $"{polityName} eased extraction burdens on local sites.",
            GovernanceLawCatalog.CrisisMovementRestrictionId => $"{polityName} restricted movement during instability.",
            GovernanceLawCatalog.OpenMovementId => $"{polityName} kept routes open despite instability.",
            GovernanceLawCatalog.StrengthenCentralAuthorityId => $"{polityName} concentrated authority around the core order.",
            GovernanceLawCatalog.SharedGovernanceId => $"{polityName} widened shared governance across the polity.",
            GovernanceLawCatalog.FrontierIntegrationId => $"{polityName} tied frontier settlements more closely to the core.",
            _ => $"{polityName} passed {proposal.Title}."
        };
    }

    private static int ScoreWhen(bool eligible, int score)
    {
        return eligible ? Math.Clamp(score, 0, 100) : 0;
    }

    private static bool IsBlockedByEnactedLaw(Polity polity, LawProposalDefinition definition)
    {
        return polity.EnactedLaws.Any(law =>
            law.IsActive &&
            (string.Equals(law.DefinitionId, definition.Id, StringComparison.Ordinal) ||
             (!string.IsNullOrWhiteSpace(definition.ConflictSlot) &&
              string.Equals(law.ConflictSlot, definition.ConflictSlot, StringComparison.Ordinal) &&
              !definition.RequiredActiveDefinitionIds.Contains(law.DefinitionId, StringComparer.Ordinal)) ||
             definition.ConflictingDefinitionIds.Contains(law.DefinitionId, StringComparer.Ordinal)));
    }

    private static bool IsEligibleByLawState(Polity polity, LawProposalDefinition definition)
    {
        if (definition.RequiredActiveDefinitionIds.Count == 0)
        {
            return true;
        }

        return definition.RequiredActiveDefinitionIds.All(requiredId =>
            polity.EnactedLaws.Any(law => law.IsActive && string.Equals(law.DefinitionId, requiredId, StringComparison.Ordinal)));
    }

    private static int ResolveEnactedLawModifier(Polity polity, LawProposalDefinition definition)
    {
        var modifier = 0;
        foreach (var enactedLaw in polity.EnactedLaws.Where(law => law.IsActive))
        {
            modifier += definition.RelatedLawScoreModifiers.GetValueOrDefault(enactedLaw.DefinitionId);
        }

        return modifier;
    }

    private static int ResolveGovernmentBias(GovernmentFormProposalBehavior behavior, string definitionId)
    {
        return definitionId switch
        {
            GovernanceLawCatalog.CentralizeStoresId => behavior.CentralizationBias / 8,
            GovernanceLawCatalog.LocalStoreAutonomyId => behavior.SharedGovernanceBias / 8,
            GovernanceLawCatalog.StrengthenCentralAuthorityId => behavior.CentralizationBias / 6,
            GovernanceLawCatalog.SharedGovernanceId => behavior.SharedGovernanceBias / 6,
            GovernanceLawCatalog.CrisisMovementRestrictionId => behavior.CentralizationBias / 9,
            GovernanceLawCatalog.OpenMovementId => behavior.SharedGovernanceBias / 9,
            _ => 0
        };
    }

    private static int ResolveSocialIdentityModifier(PolityContext context, string definitionId)
    {
        return definitionId switch
        {
            GovernanceLawCatalog.CentralizeStoresId => (context.SocialIdentity.Communalism / 12) + (HasTradition(context, SocialTraditionCatalog.StorageDisciplineId) ? 8 : 0) - (context.SocialIdentity.AutonomyOrientation / 14),
            GovernanceLawCatalog.LocalStoreAutonomyId => (context.SocialIdentity.AutonomyOrientation / 10) + (context.SocialIdentity.FrontierDistinctiveness / 14),
            GovernanceLawCatalog.ExtractionObligationId => (context.SocialIdentity.OrderOrientation / 12) - (context.SocialIdentity.AutonomyOrientation / 12),
            GovernanceLawCatalog.EaseExtractionBurdenId => (context.SocialIdentity.AutonomyOrientation / 12) + (context.SocialIdentity.FrontierDistinctiveness / 16),
            GovernanceLawCatalog.CrisisMovementRestrictionId => (context.SocialIdentity.OrderOrientation / 12) - (context.SocialIdentity.Mobility / 12),
            GovernanceLawCatalog.OpenMovementId => (context.SocialIdentity.Mobility / 10) + (HasTradition(context, SocialTraditionCatalog.SeasonalReturnId) ? 8 : 0),
            GovernanceLawCatalog.StrengthenCentralAuthorityId => (context.SocialIdentity.OrderOrientation / 10) - (context.SocialIdentity.AutonomyOrientation / 10),
            GovernanceLawCatalog.SharedGovernanceId => (context.SocialIdentity.Communalism / 10) + (context.SocialIdentity.AutonomyOrientation / 14),
            GovernanceLawCatalog.FrontierIntegrationId => (context.SocialIdentity.FrontierDistinctiveness / 12) + (context.SocialIdentity.Rootedness / 16),
            _ => 0
        };
    }

    private static int ResolveProposalSupportIdentityShift(PolityContext context, string definitionId)
    {
        return Math.Clamp(ResolveSocialIdentityModifier(context, definitionId) / 2, -10, 12);
    }

    private static int ResolveProposalOppositionIdentityShift(PolityContext context, string definitionId)
    {
        return definitionId switch
        {
            GovernanceLawCatalog.CentralizeStoresId or GovernanceLawCatalog.StrengthenCentralAuthorityId
                => Math.Clamp((context.SocialIdentity.AutonomyOrientation / 10) + (context.SocialIdentity.FrontierDistinctiveness / 16), 0, 12),
            GovernanceLawCatalog.CrisisMovementRestrictionId
                => Math.Clamp((context.SocialIdentity.Mobility / 10) + (HasTradition(context, SocialTraditionCatalog.SeasonalReturnId) ? 6 : 0), 0, 12),
            GovernanceLawCatalog.SharedGovernanceId
                => Math.Clamp(context.SocialIdentity.OrderOrientation / 14, 0, 8),
            _ => 0
        };
    }

    private static void ApplyPassedLaw(World world, Polity polity, LawProposal proposal)
    {
        var definition = Definitions.First(item => string.Equals(item.Id, proposal.DefinitionId, StringComparison.Ordinal));
        var behavior = Behaviors[polity.GovernmentForm];
        var enactedLaw = new EnactedLaw
        {
            DefinitionId = proposal.DefinitionId,
            Title = proposal.Title,
            Summary = proposal.Summary,
            IntentSummary = definition.IntentSummary,
            TradeoffSummary = definition.TradeoffSummary,
            Category = proposal.Category,
            ConflictGroup = definition.ConflictGroup,
            ConflictSlot = definition.ConflictSlot,
            ImpactScale = proposal.ImpactScale,
            EnactedOnYear = world.CurrentYear,
            EnactedOnMonth = world.CurrentMonth,
            EnforcementStrength = behavior.EnforcementTendency,
            ComplianceLevel = behavior.ComplianceTendency,
            CoreEffectiveness = Math.Clamp((behavior.EnforcementTendency + behavior.AuthorityBaseline) / 2, 0, 100),
            PeripheralEffectiveness = Math.Clamp((behavior.ComplianceTendency + behavior.CohesionBaseline) / 2, 0, 100),
            ResistanceLevel = 0,
            IsActive = true
        };

        foreach (var existing in polity.EnactedLaws.Where(law =>
                     string.Equals(law.DefinitionId, proposal.DefinitionId, StringComparison.Ordinal) ||
                     definition.RepealsDefinitionIds.Contains(law.DefinitionId, StringComparer.Ordinal) ||
                     (!string.IsNullOrWhiteSpace(definition.ConflictSlot) &&
                      string.Equals(law.ConflictSlot, definition.ConflictSlot, StringComparison.Ordinal)) ||
                     definition.ConflictingDefinitionIds.Contains(law.DefinitionId, StringComparer.Ordinal)))
        {
            existing.IsActive = false;
        }

        polity.EnactedLaws.RemoveAll(law => !law.IsActive);
        polity.EnactedLaws.Add(enactedLaw);
    }

    private static (ProposalBackingSource Primary, ProposalBackingSource? Secondary) ResolveBackingSources(
        Polity polity,
        PolityContext context,
        GovernmentFormProposalBehavior behavior,
        LawProposalDefinition definition)
    {
        PoliticalBlocSystem.EnsureBlocs(polity);
        var blocsBySource = polity.PoliticalBlocs.ToDictionary(bloc => bloc.Source);
        var rankedSources = Enum.GetValues<ProposalBackingSource>()
            .Select(source => new
            {
                Source = source,
                Score = behavior.GetBackingSourceWeight(source) +
                        PoliticalBlocCatalog.GetCategoryWeight(source, definition.Category) +
                        ResolveSourceStateWeight(context, source, definition.Id) +
                        ResolveBlocBackingWeight(blocsBySource.GetValueOrDefault(source))
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Source)
            .ToArray();

        var primary = rankedSources[0].Source;
        ProposalBackingSource? secondary = null;
        if (rankedSources.Length > 1 && rankedSources[1].Score >= Math.Max(8, rankedSources[0].Score - 2))
        {
            secondary = rankedSources[1].Source;
        }

        return (primary, secondary);
    }

    private static int ResolveSourceStateWeight(PolityContext context, ProposalBackingSource source, string definitionId)
    {
        return source switch
        {
            ProposalBackingSource.Warriors when definitionId is GovernanceLawCatalog.CrisisMovementRestrictionId or GovernanceLawCatalog.StrengthenCentralAuthorityId
                => context.Pressures.Threat.EffectiveValue / 8 + context.Governance.Authority / 15 + context.ExternalPressure.Threat / 8,
            ProposalBackingSource.Merchants when definitionId is GovernanceLawCatalog.OpenMovementId or GovernanceLawCatalog.LocalStoreAutonomyId
                => context.MaterialProduction.StorageSupport / 12 + context.Pressures.Migration.EffectiveValue / 16 + context.ExternalPressure.Cooperation / 10,
            ProposalBackingSource.Elders when definitionId is GovernanceLawCatalog.SharedGovernanceId or GovernanceLawCatalog.LocalStoreAutonomyId
                => (100 - context.Governance.Authority) / 10 + context.Governance.Cohesion / 15,
            ProposalBackingSource.CommonFolk when definitionId is GovernanceLawCatalog.CentralizeStoresId or GovernanceLawCatalog.SharedGovernanceId
                => context.Pressures.Food.EffectiveValue / 10 + (100 - context.Governance.Legitimacy) / 12 + context.ExternalPressure.RaidPressure / 10,
            ProposalBackingSource.FrontierSettlers when definitionId is GovernanceLawCatalog.OpenMovementId or GovernanceLawCatalog.FrontierIntegrationId or GovernanceLawCatalog.LocalStoreAutonomyId
                => context.Governance.PeripheralStrain / 8 + context.Pressures.Migration.EffectiveValue / 12 + context.ExternalPressure.FrontierFriction / 8,
            ProposalBackingSource.Priests when definitionId is GovernanceLawCatalog.StrengthenCentralAuthorityId
                => context.Pressures.Threat.EffectiveValue / 20 + context.Governance.Authority / 18,
            _ => 0
        };
    }

    private static int ResolveBackingSupportShift(
        Polity polity,
        PolityContext context,
        LawProposalDefinition definition,
        ProposalBackingSource primary,
        ProposalBackingSource? secondary)
    {
        PoliticalBlocSystem.EnsureBlocs(polity);
        var blocsBySource = polity.PoliticalBlocs.ToDictionary(bloc => bloc.Source);
        var shift = ResolveSingleBlocSupportShift(context, definition.Id, primary, blocsBySource.GetValueOrDefault(primary));
        if (secondary is not null)
        {
            shift += ResolveSingleBlocSupportShift(context, definition.Id, secondary.Value, blocsBySource.GetValueOrDefault(secondary.Value)) / 2;
        }

        return Math.Clamp(shift, -15, 20);
    }

    private static int ResolveSingleBlocSupportShift(
        PolityContext context,
        string definitionId,
        ProposalBackingSource source,
        PoliticalBloc? bloc)
    {
        var shift = source switch
        {
            ProposalBackingSource.Warriors when definitionId is GovernanceLawCatalog.CrisisMovementRestrictionId or GovernanceLawCatalog.StrengthenCentralAuthorityId
                => context.Pressures.Threat.EffectiveValue >= 45 || context.ExternalPressure.Threat >= 40 ? 7 : 3,
            ProposalBackingSource.Merchants when definitionId is GovernanceLawCatalog.OpenMovementId or GovernanceLawCatalog.LocalStoreAutonomyId
                => context.Pressures.Threat.EffectiveValue < 60 && context.ExternalPressure.RaidPressure < 35 ? 6 : 2,
            ProposalBackingSource.CommonFolk when definitionId is GovernanceLawCatalog.CentralizeStoresId or GovernanceLawCatalog.SharedGovernanceId
                => context.Pressures.Food.EffectiveValue >= 45 || context.ExternalPressure.RaidPressure >= 35 ? 6 : 3,
            ProposalBackingSource.Elders when definitionId is GovernanceLawCatalog.SharedGovernanceId or GovernanceLawCatalog.LocalStoreAutonomyId
                => context.Governance.Legitimacy <= 55 ? 5 : 2,
            ProposalBackingSource.FrontierSettlers when definitionId is GovernanceLawCatalog.FrontierIntegrationId or GovernanceLawCatalog.OpenMovementId
                => context.Governance.PeripheralStrain >= 30 || context.ExternalPressure.FrontierFriction >= 25 ? 5 : 2,
            _ => 0
        };

        if (bloc is null)
        {
            return shift;
        }

        shift += (bloc.Influence - 50) / 8;
        shift += (bloc.Satisfaction - 50) / 6;
        return shift;
    }

    private static int ResolveBlocOppositionShift(
        Polity polity,
        PolityContext context,
        LawProposalDefinition definition,
        ProposalBackingSource primary,
        ProposalBackingSource? secondary)
    {
        PoliticalBlocSystem.EnsureBlocs(polity);
        var opposition = 0;
        foreach (var bloc in polity.PoliticalBlocs)
        {
            if (bloc.Source == primary || bloc.Source == secondary)
            {
                continue;
            }

            var categoryWeight = PoliticalBlocCatalog.GetCategoryWeight(bloc.Source, definition.Category);
            if (categoryWeight >= 7)
            {
                continue;
            }

            opposition += Math.Max(0, (bloc.Influence - 45) / 10);
            opposition += Math.Max(0, (55 - bloc.Satisfaction) / 12);
        }

        if (definition.Id is GovernanceLawCatalog.CentralizeStoresId or GovernanceLawCatalog.ExtractionObligationId or GovernanceLawCatalog.CrisisMovementRestrictionId)
        {
            opposition += context.Governance.PeripheralStrain / 12;
        }

        return Math.Clamp(opposition, 0, 20);
    }

    private static int ResolveBlocProposalModifier(Polity polity, PolityContext context, LawProposalDefinition definition)
    {
        PoliticalBlocSystem.EnsureBlocs(polity);
        var modifier = 0;
        foreach (var bloc in polity.PoliticalBlocs)
        {
            var categoryWeight = PoliticalBlocCatalog.GetCategoryWeight(bloc.Source, definition.Category);
            if (categoryWeight == 0)
            {
                continue;
            }

            modifier += ((categoryWeight - 4) * bloc.Influence) / 18;
            modifier += ((100 - bloc.Satisfaction) * categoryWeight) / 45;
        }

        if (definition.Id is GovernanceLawCatalog.SharedGovernanceId or GovernanceLawCatalog.LocalStoreAutonomyId)
        {
            modifier += Math.Max(0, 55 - context.Governance.Legitimacy) / 5;
        }

        if (definition.Id is GovernanceLawCatalog.StrengthenCentralAuthorityId or GovernanceLawCatalog.CentralizeStoresId)
        {
            modifier += context.Pressures.Threat.EffectiveValue / 10;
        }

        if (definition.Id is GovernanceLawCatalog.CrisisMovementRestrictionId or GovernanceLawCatalog.StrengthenCentralAuthorityId or GovernanceLawCatalog.FrontierIntegrationId)
        {
            modifier += context.ExternalPressure.Threat / 8;
            modifier += context.ExternalPressure.FrontierFriction / 10;
        }

        if (definition.Id == GovernanceLawCatalog.OpenMovementId)
        {
            modifier += context.ExternalPressure.Cooperation / 10;
            modifier -= context.ExternalPressure.RaidPressure / 12;
        }

        return Math.Clamp(modifier, -10, 35);
    }

    private static PopulationGroup BuildAggregateGroup(PolityContext context)
    {
        return new PopulationGroup
        {
            Id = context.Polity.Id,
            Name = context.Polity.Name,
            SpeciesId = context.SpeciesId,
            PolityId = context.Polity.Id,
            CurrentRegionId = context.CurrentRegionId,
            OriginRegionId = context.OriginRegionId,
            Population = context.TotalPopulation,
            StoredFood = context.TotalStoredFood,
            FoodAccounting = context.FoodAccounting.Clone(),
            HungerPressure = context.LeadGroup?.HungerPressure ?? 0.0f,
            ShortageMonths = context.LeadGroup?.ShortageMonths ?? 0,
            FoodStressState = context.LeadGroup?.FoodStressState ?? Species.Domain.Enums.FoodStressState.FedStable,
            SubsistencePreference = context.LeadGroup?.SubsistencePreference ?? Species.Domain.Enums.SubsistencePreference.Mixed,
            SubsistenceMode = context.LeadGroup?.SubsistenceMode ?? SubsistenceMode.Mixed,
            Pressures = context.Pressures.Clone(),
            LastRegionId = context.LeadGroup?.LastRegionId ?? string.Empty,
            MonthsSinceLastMove = context.LeadGroup?.MonthsSinceLastMove ?? 0,
            KnownRegionIds = new HashSet<string>(context.KnownRegionIds, StringComparer.Ordinal),
            KnownDiscoveryIds = new HashSet<string>(context.KnownDiscoveryIds, StringComparer.Ordinal),
            DiscoveryEvidence = context.LeadGroup?.DiscoveryEvidence.Clone() ?? new DiscoveryEvidenceState(),
            LearnedAdvancementIds = new HashSet<string>(context.LearnedAdvancementIds, StringComparer.Ordinal),
            AdvancementEvidence = context.LeadGroup?.AdvancementEvidence.Clone() ?? new AdvancementEvidenceState()
        };
    }

    private static int ResolveBlocBackingWeight(PoliticalBloc? bloc)
    {
        if (bloc is null)
        {
            return 0;
        }

        return (bloc.Influence / 5) + Math.Max(0, 55 - bloc.Satisfaction) / 8;
    }

    private static int ResolveExternalPressureModifier(PolityContext context, string definitionId)
    {
        return definitionId switch
        {
            GovernanceLawCatalog.CentralizeStoresId => context.ExternalPressure.RaidPressure / 8 + context.ExternalPressure.Threat / 10,
            GovernanceLawCatalog.LocalStoreAutonomyId => context.ExternalPressure.FrontierFriction / 10 - context.ExternalPressure.RaidPressure / 14,
            GovernanceLawCatalog.ExtractionObligationId => context.ExternalPressure.Threat / 8 + context.ExternalPressure.HostileNeighborCount * 4,
            GovernanceLawCatalog.EaseExtractionBurdenId => context.ExternalPressure.Cooperation / 12,
            GovernanceLawCatalog.CrisisMovementRestrictionId => context.ExternalPressure.Threat / 7 + context.ExternalPressure.RaidPressure / 8,
            GovernanceLawCatalog.OpenMovementId => context.ExternalPressure.Cooperation / 8 - context.ExternalPressure.RaidPressure / 10,
            GovernanceLawCatalog.StrengthenCentralAuthorityId => context.ExternalPressure.Threat / 7 + context.ExternalPressure.HostileNeighborCount * 5,
            GovernanceLawCatalog.SharedGovernanceId => context.ExternalPressure.Cooperation / 10,
            GovernanceLawCatalog.FrontierIntegrationId => context.ExternalPressure.FrontierFriction / 7 + context.ExternalPressure.HostileNeighborCount * 4,
            _ => 0
        };
    }

    private static string BuildReasonSummary(string definitionId, PolityContext context)
    {
        return definitionId switch
        {
            GovernanceLawCatalog.CentralizeStoresId when context.ExternalPressure.RaidPressure >= 30 => "Outside raids are pushing the core toward tighter control over stores.",
            GovernanceLawCatalog.CentralizeStoresId => "Shortages and weak storage are pushing the core toward tighter distribution.",
            GovernanceLawCatalog.LocalStoreAutonomyId when context.ExternalPressure.FrontierFriction >= 25 => "Frontier strain is pushing settlements to keep greater local control.",
            GovernanceLawCatalog.LocalStoreAutonomyId => "Peripheral strain is pushing settlements to keep greater local control.",
            GovernanceLawCatalog.ExtractionObligationId when context.ExternalPressure.Threat >= 35 => "Hostile outside pressure is pushing a harder extraction burden.",
            GovernanceLawCatalog.ExtractionObligationId => "Material deficits and pressure for tools are pushing a harder extraction burden.",
            GovernanceLawCatalog.EaseExtractionBurdenId => "Falling legitimacy and frontier strain are pushing relief from extraction demands.",
            GovernanceLawCatalog.CrisisMovementRestrictionId when context.ExternalPressure.RaidPressure >= 30 => "Raids and outside danger are pushing the polity toward tighter movement control.",
            GovernanceLawCatalog.CrisisMovementRestrictionId => "Threat and migration pressure are pushing the polity toward tighter movement control.",
            GovernanceLawCatalog.OpenMovementId when context.ExternalPressure.Cooperation >= 25 => "Practical outside contact is pushing the polity to keep routes open.",
            GovernanceLawCatalog.OpenMovementId => "Frontier use and legitimacy strain are pushing the polity to keep routes open.",
            GovernanceLawCatalog.StrengthenCentralAuthorityId when context.ExternalPressure.Threat >= 35 => "Outside hostility is pushing authority back toward the core.",
            GovernanceLawCatalog.StrengthenCentralAuthorityId => "Unsteady command under pressure is pushing authority back toward the core.",
            GovernanceLawCatalog.SharedGovernanceId => "Legitimacy and cohesion are weak enough that broader buy-in is being demanded.",
            GovernanceLawCatalog.FrontierIntegrationId when context.ExternalPressure.FrontierFriction >= 25 => "Frontier friction is pushing the polity to bind peripheral sites more deliberately.",
            GovernanceLawCatalog.FrontierIntegrationId => "Peripheral strain is pushing the polity to bind frontier sites more deliberately.",
            _ => string.Empty
        };
    }

    private static World ReplacePolity(World world, Polity polity)
    {
        var updatedPolities = world.Polities
            .Select(item => string.Equals(item.Id, polity.Id, StringComparison.Ordinal) ? polity : item)
            .ToArray();
        return new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, world.PopulationGroups, world.Chronicle, updatedPolities, world.FocalPolityId);
    }

    private static bool HasTradition(PolityContext context, string traditionId)
    {
        return context.SocialIdentity.TraditionIds.Contains(traditionId, StringComparer.Ordinal);
    }
}
