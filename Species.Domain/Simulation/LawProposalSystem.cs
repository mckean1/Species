using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

// MVP law proposals stay intentionally narrow: one active proposal, one rules table,
// and a compact proposal pool driven by current pressures plus government form.
public sealed class LawProposalSystem
{
    private static readonly IReadOnlyDictionary<GovernmentForm, GovernmentFormProposalBehavior> Behaviors =
        GovernmentFormLawBehaviorCatalog.Behaviors;

    private static readonly IReadOnlyList<LawProposalDefinition> Definitions =
    [
        new()
        {
            Id = "ban-hunting",
            Title = "Ban Hunting",
            Summary = "The polity would halt hunting for a time to preserve nearby game.",
            Category = LawProposalCategory.Food,
            ConflictGroup = LawConflictGroup.Food,
            ConflictSlot = "hunting-access",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.TribalClanRule,
            Score = (group, _) => ScoreWhen(
                group.StoredFood > group.Population &&
                group.Pressures.ThreatPressure < 45,
                35 + group.Pressures.FoodPressure / 2 + group.Pressures.MigrationPressure / 3)
        },
        new()
        {
            Id = "require-warrior-oaths",
            Title = "Require Warrior Oaths",
            Summary = "Warriors would be bound to formal oath-taking before any campaign or feud.",
            Category = LawProposalCategory.Military,
            ConflictGroup = LawConflictGroup.Military,
            ConflictSlot = "warrior-discipline",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.TribalClanRule,
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure >= 45,
                30 + group.Pressures.ThreatPressure / 2 + group.Pressures.MigrationPressure / 5)
        },
        new()
        {
            Id = "forbid-blood-feuds",
            Title = "Forbid Blood Feuds",
            Summary = "Clan vengeance would be curbed to reduce spiraling retaliation.",
            Category = LawProposalCategory.Order,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "blood-feud-policy",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.TribalClanRule,
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure >= 35 || group.Pressures.OvercrowdingPressure >= 45,
                25 + group.Pressures.ThreatPressure / 3 + group.Pressures.OvercrowdingPressure / 2)
        },
        new()
        {
            Id = "reserve-sacred-grounds",
            Title = "Reserve Sacred Grounds",
            Summary = "A protected sacred place would be set aside from ordinary use.",
            Category = LawProposalCategory.Faith,
            ConflictGroup = LawConflictGroup.Faith,
            ConflictSlot = "sacred-grounds-policy",
            ImpactScale = 1,
            GovernmentForm = GovernmentForm.TribalClanRule,
            Score = (group, region) => ScoreWhen(
                region.WaterAvailability == WaterAvailability.High || group.Pressures.MigrationPressure >= 45,
                20 + group.Pressures.MigrationPressure / 2 + group.Pressures.OvercrowdingPressure / 4)
        },
        new()
        {
            Id = "grant-market-rights",
            Title = "Grant Market Rights",
            Summary = "Regular market exchange would be formally recognized and protected.",
            Category = LawProposalCategory.Trade,
            ConflictGroup = LawConflictGroup.Trade,
            ConflictSlot = "market-rights",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.CouncilRule,
            ConflictingDefinitionIds = ["close-city-gates"],
            Score = (group, _) => ScoreWhen(
                group.StoredFood > group.Population &&
                group.Pressures.ThreatPressure < 55,
                25 + group.Pressures.OvercrowdingPressure / 3 + group.Pressures.MigrationPressure / 4)
        },
        new()
        {
            Id = "open-grain-stores",
            Title = "Open Grain Stores",
            Summary = "Stored food would be released to ease local scarcity.",
            Category = LawProposalCategory.Food,
            ConflictGroup = LawConflictGroup.Food,
            ConflictSlot = "grain-release",
            ImpactScale = 3,
            GovernmentForm = GovernmentForm.CouncilRule,
            Score = (group, _) => ScoreWhen(
                group.Pressures.FoodPressure >= 45 && group.StoredFood > Math.Max(1, group.Population / 3),
                35 + group.Pressures.FoodPressure / 2)
        },
        new()
        {
            Id = "bind-common-defense",
            Title = "Bind Common Defense",
            Summary = "Member communities would be bound to answer common defense calls.",
            Category = LawProposalCategory.Military,
            ConflictGroup = LawConflictGroup.Military,
            ConflictSlot = "common-defense-pact",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.Confederation,
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure >= 40 || group.Pressures.MigrationPressure >= 45,
                28 + group.Pressures.ThreatPressure / 2 + group.Pressures.MigrationPressure / 4)
        },
        new()
        {
            Id = "affirm-local-autonomy",
            Title = "Affirm Local Autonomy",
            Summary = "Local communities would keep broad control over their own obligations.",
            Category = LawProposalCategory.Custom,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "local-autonomy",
            ImpactScale = 1,
            GovernmentForm = GovernmentForm.Confederation,
            Score = (group, _) => ScoreWhen(
                group.Pressures.OvercrowdingPressure >= 35 || group.Pressures.MigrationPressure >= 35,
                24 + group.Pressures.OvercrowdingPressure / 3 + group.Pressures.MigrationPressure / 3)
        },
        new()
        {
            Id = "restrict-private-retainers",
            Title = "Restrict Private Retainers",
            Summary = "Private armed followers would be limited to reduce elite coercion.",
            Category = LawProposalCategory.Order,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "private-retainers",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.CouncilRule,
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure >= 35 || group.Pressures.OvercrowdingPressure >= 40,
                25 + group.Pressures.ThreatPressure / 3 + group.Pressures.OvercrowdingPressure / 3)
        },
        new()
        {
            Id = "expand-council-seats",
            Title = "Expand Council Seats",
            Summary = "More seats would be added so rising voices can enter deliberation.",
            Category = LawProposalCategory.Custom,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "council-size",
            ImpactScale = 1,
            GovernmentForm = GovernmentForm.CouncilRule,
            Score = (group, _) => ScoreWhen(
                group.Pressures.MigrationPressure >= 35 || group.Pressures.OvercrowdingPressure >= 35,
                20 + group.Pressures.MigrationPressure / 3 + group.Pressures.OvercrowdingPressure / 3)
        },
        new()
        {
            Id = "standardize-weights",
            Title = "Standardize Weights",
            Summary = "Trade measures would be standardized to steady exchange and pricing.",
            Category = LawProposalCategory.Trade,
            ConflictGroup = LawConflictGroup.Trade,
            ConflictSlot = "trade-standards",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.MerchantRule,
            Score = (group, _) => ScoreWhen(
                group.StoredFood > Math.Max(1, group.Population / 2) || group.Pressures.MigrationPressure >= 35,
                26 + group.Pressures.MigrationPressure / 3 + Math.Max(0, 50 - group.Pressures.ThreatPressure) / 4)
        },
        new()
        {
            Id = "protect-caravan-routes",
            Title = "Protect Caravan Routes",
            Summary = "Routes would be guarded to keep trade and movement flowing.",
            Category = LawProposalCategory.Movement,
            ConflictGroup = LawConflictGroup.Movement,
            ConflictSlot = "route-protection",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.MerchantRule,
            Score = (group, _) => ScoreWhen(
                group.Pressures.MigrationPressure >= 40 || group.Pressures.ThreatPressure >= 35,
                24 + group.Pressures.MigrationPressure / 3 + group.Pressures.ThreatPressure / 4)
        },
        new()
        {
            Id = "call-feudal-levy",
            Title = "Call Feudal Levy",
            Summary = "Local lords would be required to furnish armed service and retainers.",
            Category = LawProposalCategory.Military,
            ConflictGroup = LawConflictGroup.Military,
            ConflictSlot = "feudal-levy",
            ImpactScale = 3,
            GovernmentForm = GovernmentForm.FeudalRule,
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure >= 45,
                30 + group.Pressures.ThreatPressure / 2 + group.Pressures.OvercrowdingPressure / 4)
        },
        new()
        {
            Id = "reaffirm-lordly-privilege",
            Title = "Reaffirm Lordly Privilege",
            Summary = "Old privileges over land, duty, and rank would be formally reaffirmed.",
            Category = LawProposalCategory.Custom,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "lordly-privilege",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.FeudalRule,
            Score = (group, _) => ScoreWhen(
                group.Pressures.OvercrowdingPressure >= 35 || group.Pressures.ThreatPressure >= 35,
                22 + group.Pressures.OvercrowdingPressure / 3 + group.Pressures.ThreatPressure / 4)
        },
        new()
        {
            Id = "conduct-census",
            Title = "Conduct Census",
            Summary = "Households and obligations would be counted for clearer administration.",
            Category = LawProposalCategory.Order,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "population-census",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.ImperialBureaucracy,
            Score = (group, _) => ScoreWhen(
                group.Pressures.OvercrowdingPressure >= 35 || group.Pressures.MigrationPressure >= 35,
                26 + group.Pressures.OvercrowdingPressure / 3 + group.Pressures.MigrationPressure / 3)
        },
        new()
        {
            Id = "standardize-tax-rolls",
            Title = "Standardize Tax Rolls",
            Summary = "Dues and records would be standardized across the polity.",
            Category = LawProposalCategory.Order,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "tax-rolls",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.ImperialBureaucracy,
            Score = (group, _) => ScoreWhen(
                group.StoredFood > Math.Max(1, group.Population / 2) || group.Pressures.OvercrowdingPressure >= 40,
                25 + group.Pressures.OvercrowdingPressure / 3 + Math.Max(0, 50 - group.Pressures.FoodPressure) / 4)
        },
        new()
        {
            Id = "expand-civic-assembly",
            Title = "Expand Civic Assembly",
            Summary = "More civic voices would be admitted into formal deliberation.",
            Category = LawProposalCategory.Custom,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "civic-assembly",
            ImpactScale = 1,
            GovernmentForm = GovernmentForm.Republic,
            Score = (group, _) => ScoreWhen(
                group.Pressures.OvercrowdingPressure >= 35 || group.Pressures.MigrationPressure >= 35,
                24 + group.Pressures.OvercrowdingPressure / 3 + group.Pressures.MigrationPressure / 3)
        },
        new()
        {
            Id = "limit-emergency-decrees",
            Title = "Limit Emergency Decrees",
            Summary = "Emergency orders would face tighter limits and shorter duration.",
            Category = LawProposalCategory.Order,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "emergency-decrees",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.Republic,
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure < 55 && group.Pressures.MigrationPressure >= 30,
                22 + group.Pressures.MigrationPressure / 3 + (100 - group.Pressures.ThreatPressure) / 5)
        },
        new()
        {
            Id = "initiate-curfew",
            Title = "Initiate Curfew",
            Summary = "Movement after dark would be restricted to tighten local order.",
            Category = LawProposalCategory.Order,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "curfew-policy",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.AbsoluteRule,
            RelatedLawScoreModifiers = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["close-city-gates"] = 10
            },
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure >= 45 || group.Pressures.OvercrowdingPressure >= 45,
                30 + group.Pressures.ThreatPressure / 2 + group.Pressures.OvercrowdingPressure / 4)
        },
        new()
        {
            Id = "authorize-secret-arrests",
            Title = "Authorize Secret Arrests",
            Summary = "Suspected enemies could be seized quietly without public process.",
            Category = LawProposalCategory.Punishment,
            ConflictGroup = LawConflictGroup.Punishment,
            ConflictSlot = "secret-arrests",
            ImpactScale = 3,
            GovernmentForm = GovernmentForm.DespoticRule,
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure >= 50 || group.Pressures.MigrationPressure >= 50,
                32 + group.Pressures.ThreatPressure / 2 + group.Pressures.MigrationPressure / 4)
        },
        new()
        {
            Id = "impose-emergency-rule",
            Title = "Impose Emergency Rule",
            Summary = "Normal restraints would be suspended to force immediate order.",
            Category = LawProposalCategory.Order,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "emergency-rule",
            ImpactScale = 3,
            GovernmentForm = GovernmentForm.DespoticRule,
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure >= 55 || group.Pressures.OvercrowdingPressure >= 55,
                34 + group.Pressures.ThreatPressure / 2 + group.Pressures.OvercrowdingPressure / 3)
        },
        new()
        {
            Id = "raise-war-levy",
            Title = "Raise War Levy",
            Summary = "More people and goods would be claimed for defense or war.",
            Category = LawProposalCategory.Military,
            ConflictGroup = LawConflictGroup.Military,
            ConflictSlot = "war-levy",
            ImpactScale = 3,
            GovernmentForm = GovernmentForm.AbsoluteRule,
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure >= 55,
                35 + group.Pressures.ThreatPressure / 2 + group.Pressures.MigrationPressure / 5)
        },
        new()
        {
            Id = "establish-public-executions",
            Title = "Establish Public Executions",
            Summary = "Punishment would be made public to harden deterrence.",
            Category = LawProposalCategory.Punishment,
            ConflictGroup = LawConflictGroup.Punishment,
            ConflictSlot = "execution-policy",
            ImpactScale = 3,
            GovernmentForm = GovernmentForm.AbsoluteRule,
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure >= 60 || group.Pressures.OvercrowdingPressure >= 60,
                20 + group.Pressures.ThreatPressure / 2 + group.Pressures.OvercrowdingPressure / 3)
        },
        new()
        {
            Id = "close-city-gates",
            Title = "Close City Gates",
            Summary = "Entry and exit would be narrowed to control danger and movement.",
            Category = LawProposalCategory.Movement,
            ConflictGroup = LawConflictGroup.Movement,
            ConflictSlot = "gate-access",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.AbsoluteRule,
            ConflictingDefinitionIds = ["grant-market-rights"],
            RelatedLawScoreModifiers = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["initiate-curfew"] = 8
            },
            Score = (group, _) => ScoreWhen(
                group.Pressures.MigrationPressure >= 45 || group.Pressures.ThreatPressure >= 45,
                30 + group.Pressures.MigrationPressure / 2 + group.Pressures.ThreatPressure / 4)
        },
        new()
        {
            Id = "forbid-foreign-worship",
            Title = "Forbid Foreign Worship",
            Summary = "Foreign rites would be barred to defend the local sacred order.",
            Category = LawProposalCategory.Faith,
            ConflictGroup = LawConflictGroup.Faith,
            ConflictSlot = "foreign-worship",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.Theocracy,
            RelatedLawScoreModifiers = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["mandate-holy-rites"] = 8,
                ["burn-heretical-texts"] = 6
            },
            Score = (group, _) => ScoreWhen(
                group.Pressures.MigrationPressure >= 40 || group.Pressures.OvercrowdingPressure >= 40,
                25 + group.Pressures.MigrationPressure / 2 + group.Pressures.OvercrowdingPressure / 4)
        },
        new()
        {
            Id = "mandate-holy-rites",
            Title = "Mandate Holy Rites",
            Summary = "Common rites would be made compulsory in public life.",
            Category = LawProposalCategory.Faith,
            ConflictGroup = LawConflictGroup.Faith,
            ConflictSlot = "holy-rites",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.Theocracy,
            RelatedLawScoreModifiers = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["forbid-foreign-worship"] = 10
            },
            Score = (group, _) => ScoreWhen(
                group.Pressures.WaterPressure >= 35 || group.Pressures.FoodPressure >= 35,
                20 + group.Pressures.WaterPressure / 3 + group.Pressures.FoodPressure / 3)
        },
        new()
        {
            Id = "burn-heretical-texts",
            Title = "Burn Heretical Texts",
            Summary = "Texts judged dangerous to doctrine would be destroyed.",
            Category = LawProposalCategory.Punishment,
            ConflictGroup = LawConflictGroup.Punishment,
            ConflictSlot = "heresy-texts",
            ImpactScale = 2,
            GovernmentForm = GovernmentForm.Theocracy,
            RelatedLawScoreModifiers = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["forbid-foreign-worship"] = 8
            },
            Score = (group, _) => ScoreWhen(
                group.KnownDiscoveryIds.Count + group.LearnedAdvancementIds.Count >= 2 &&
                (group.Pressures.ThreatPressure >= 35 || group.Pressures.MigrationPressure >= 35),
                15 + group.Pressures.ThreatPressure / 3 + group.Pressures.MigrationPressure / 3)
        },
        new()
        {
            Id = "ban-funeral-excess",
            Title = "Ban Funeral Excess",
            Summary = "Funeral spending and display would be limited during strain.",
            Category = LawProposalCategory.Custom,
            ConflictGroup = LawConflictGroup.Food,
            ConflictSlot = "funeral-excess",
            ImpactScale = 1,
            GovernmentForm = GovernmentForm.Theocracy,
            Score = (group, _) => ScoreWhen(
                group.Pressures.FoodPressure >= 45 || group.StoredFood < Math.Max(1, group.Population / 2),
                25 + group.Pressures.FoodPressure / 2)
        },
        new()
        {
            Id = "end-curfew",
            Title = "End Curfew",
            Summary = "The curfew would be lifted as the polity eases direct control.",
            Category = LawProposalCategory.Order,
            ConflictGroup = LawConflictGroup.Order,
            ConflictSlot = "curfew-policy",
            ImpactScale = 1,
            GovernmentForm = GovernmentForm.AbsoluteRule,
            RequiredActiveDefinitionIds = ["initiate-curfew"],
            RepealsDefinitionIds = ["initiate-curfew"],
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure < 45 || group.Pressures.MigrationPressure >= 55,
                20 + (100 - group.Pressures.ThreatPressure) / 3 + group.Pressures.MigrationPressure / 3)
        },
        new()
        {
            Id = "reopen-city-gates",
            Title = "Reopen City Gates",
            Summary = "Gate controls would be relaxed so passage can resume.",
            Category = LawProposalCategory.Movement,
            ConflictGroup = LawConflictGroup.Movement,
            ConflictSlot = "gate-access",
            ImpactScale = 1,
            GovernmentForm = GovernmentForm.AbsoluteRule,
            RequiredActiveDefinitionIds = ["close-city-gates"],
            RepealsDefinitionIds = ["close-city-gates"],
            Score = (group, _) => ScoreWhen(
                group.Pressures.FoodPressure >= 45 || group.Pressures.ThreatPressure < 45,
                20 + group.Pressures.FoodPressure / 3 + (100 - group.Pressures.ThreatPressure) / 4)
        },
        new()
        {
            Id = "permit-foreign-worship",
            Title = "Permit Foreign Worship",
            Summary = "Foreign rites would be tolerated again to ease outside tension.",
            Category = LawProposalCategory.Faith,
            ConflictGroup = LawConflictGroup.Faith,
            ConflictSlot = "foreign-worship",
            ImpactScale = 1,
            GovernmentForm = GovernmentForm.Theocracy,
            RequiredActiveDefinitionIds = ["forbid-foreign-worship"],
            RepealsDefinitionIds = ["forbid-foreign-worship"],
            Score = (group, _) => ScoreWhen(
                group.Pressures.MigrationPressure >= 55 || group.Pressures.ThreatPressure < 45,
                20 + group.Pressures.MigrationPressure / 3 + (100 - group.Pressures.ThreatPressure) / 4)
        },
        new()
        {
            Id = "end-public-executions",
            Title = "End Public Executions",
            Summary = "Execution would no longer be used as a public spectacle.",
            Category = LawProposalCategory.Punishment,
            ConflictGroup = LawConflictGroup.Punishment,
            ConflictSlot = "execution-policy",
            ImpactScale = 1,
            GovernmentForm = GovernmentForm.AbsoluteRule,
            RequiredActiveDefinitionIds = ["establish-public-executions"],
            RepealsDefinitionIds = ["establish-public-executions"],
            Score = (group, _) => ScoreWhen(
                group.Pressures.ThreatPressure < 50 || group.Pressures.OvercrowdingPressure >= 55,
                18 + (100 - group.Pressures.ThreatPressure) / 4 + group.Pressures.OvercrowdingPressure / 3)
        }
    ];

    public (World World, IReadOnlyList<LawProposalChange> Changes) Run(World world, string playerPolityId)
    {
        if (string.IsNullOrWhiteSpace(playerPolityId))
        {
            return (world, Array.Empty<LawProposalChange>());
        }

        var focusPolity = PolityData.Resolve(world, playerPolityId);
        var context = focusPolity is null ? null : PolityData.BuildContext(world, focusPolity);
        if (focusPolity is null || context?.LeadGroup is null)
        {
            return (world, Array.Empty<LawProposalChange>());
        }

        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        if (!regionsById.TryGetValue(context.CurrentRegionId, out var region))
        {
            return (world, Array.Empty<LawProposalChange>());
        }

        PoliticalBlocSystem.EnsureBlocs(focusPolity);
        var aggregateGroup = BuildAggregateGroup(context);

        if (focusPolity.ActiveLawProposal is null)
        {
            var generated = TryGenerateProposal(focusPolity, aggregateGroup, region);
            if (generated is not null)
            {
                focusPolity.ActiveLawProposal = generated;
            }

            return (world, Array.Empty<LawProposalChange>());
        }

        var activeProposal = focusPolity.ActiveLawProposal.Clone();
        activeProposal.AgeInMonths++;
        activeProposal.IgnoredMonths++;

        var definition = Definitions.FirstOrDefault(item => string.Equals(item.Id, activeProposal.DefinitionId, StringComparison.Ordinal));
        if (definition is null)
        {
            focusPolity.ActiveLawProposal = activeProposal;
            return (world, Array.Empty<LawProposalChange>());
        }

        var behavior = GovernmentFormLawBehaviorCatalog.Get(focusPolity.GovernmentForm);
        var relevance = definition.Score(aggregateGroup, region);
        UpdateMomentum(activeProposal, behavior, relevance);

        var naturalStatus = ResolveIgnoredProposal(activeProposal, behavior, relevance);
        if (naturalStatus is null)
        {
            focusPolity.ActiveLawProposal = activeProposal;
            return (world, Array.Empty<LawProposalChange>());
        }

        return (world, FinalizeProposal(world, focusPolity, activeProposal, naturalStatus.Value));
    }

    public (World World, IReadOnlyList<LawProposalChange> Changes) ResolvePlayerDecision(
        World world,
        string playerPolityId,
        LawProposalStatus status)
    {
        var focusPolity = PolityData.Resolve(world, playerPolityId);
        if (focusPolity?.ActiveLawProposal is null)
        {
            return (world, Array.Empty<LawProposalChange>());
        }

        var resolvedProposal = focusPolity.ActiveLawProposal.Clone();
        var behavior = GovernmentFormLawBehaviorCatalog.Get(focusPolity.GovernmentForm);

        if (status == LawProposalStatus.Passed)
        {
            resolvedProposal.Support = Math.Clamp(resolvedProposal.Support + (behavior.PlayerDecisionStrength * 4), 0, 100);
        }
        else if (status == LawProposalStatus.Vetoed)
        {
            resolvedProposal.Opposition = Math.Clamp(resolvedProposal.Opposition + (behavior.PlayerDecisionStrength * 4), 0, 100);
        }

        return (world, FinalizeProposal(world, focusPolity, resolvedProposal, status));
    }

    private static LawProposal? TryGenerateProposal(Polity polity, PopulationGroup aggregateGroup, Region region)
    {
        var behavior = GovernmentFormLawBehaviorCatalog.Get(polity.GovernmentForm);
        var candidate = Definitions
            .Where(definition => definition.GovernmentForm == polity.GovernmentForm)
            .Select(definition =>
            {
                var relevance = definition.Score(aggregateGroup, region);
                if (relevance == 0 || !IsEligibleByLawState(polity, definition) || IsBlockedByEnactedLaw(polity, definition))
                {
                    return new { Definition = definition, Relevance = 0, WeightedScore = 0 };
                }

                var weightedScore = relevance +
                    (behavior.GetCategoryWeight(definition.Category) * 3) +
                    ResolveEnactedLawModifier(polity, definition) +
                    ResolveBlocProposalModifier(polity, aggregateGroup, definition) +
                    ((definition.ImpactScale - 1) * behavior.ExtremityAllowance * 2);
                return new { Definition = definition, Relevance = relevance, WeightedScore = weightedScore };
            })
            .Where(candidate => candidate.Relevance >= 25)
            .OrderByDescending(candidate => candidate.WeightedScore)
            .ThenByDescending(candidate => candidate.Relevance)
            .ThenBy(candidate => candidate.Definition.Title, StringComparer.Ordinal)
            .FirstOrDefault();

        if (candidate is null)
        {
            return null;
        }

        var support = Math.Clamp(
            30 + (candidate.Relevance / 2) + (behavior.AutoPassBias * 6) - (candidate.Definition.ImpactScale * 4),
            10,
            90);
        var opposition = Math.Clamp(
            20 + ((100 - candidate.Relevance) / 4) + (candidate.Definition.ImpactScale * 8) + (behavior.AutoVetoBias * 5),
            5,
            90);
        var backing = ResolveBackingSources(polity, aggregateGroup, behavior, candidate.Definition);
        var backingSupportShift = ResolveBackingSupportShift(polity, aggregateGroup, candidate.Definition, backing.Primary, backing.Secondary);
        var blocOppositionShift = ResolveBlocOppositionShift(polity, candidate.Definition, backing.Primary, backing.Secondary);
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
            Category = candidate.Definition.Category,
            Status = LawProposalStatus.Active,
            Support = Math.Clamp(support + backingSupportShift, 0, 100),
            Opposition = Math.Clamp(opposition - Math.Max(0, backingSupportShift / 2) + blocOppositionShift, 0, 100),
            Urgency = urgency,
            AgeInMonths = 0,
            IgnoredMonths = 0,
            ImpactScale = candidate.Definition.ImpactScale,
            GovernmentForm = polity.GovernmentForm,
            PrimaryBackingSource = backing.Primary,
            SecondaryBackingSource = backing.Secondary
        };
    }

    private static void UpdateMomentum(LawProposal proposal, GovernmentFormProposalBehavior behavior, int relevance)
    {
        proposal.Urgency = Math.Clamp(
            Math.Max(proposal.Urgency, relevance + (proposal.ImpactScale * 8)),
            0,
            100);

        if (proposal.IgnoredMonths < 24)
        {
            return;
        }

        var momentum = (relevance - 50) / 10;
        var indecision = 1 + ((proposal.IgnoredMonths - 24) / 12) * behavior.IndecisionPenaltyStrength;

        proposal.Support = Math.Clamp(
            proposal.Support + momentum + behavior.AutoPassBias - Math.Max(0, indecision / 3),
            0,
            100);
        proposal.Opposition = Math.Clamp(
            proposal.Opposition + Math.Max(0, -momentum) + behavior.AutoVetoBias + Math.Max(1, indecision / 2),
            0,
            100);
        proposal.Urgency = Math.Clamp(
            proposal.Urgency + Math.Max(-2, momentum) - behavior.IgnoreTolerance + indecision,
            0,
            100);
    }

    private static LawProposalStatus? ResolveIgnoredProposal(LawProposal proposal, GovernmentFormProposalBehavior behavior, int relevance)
    {
        if (proposal.IgnoredMonths < 60)
        {
            return null;
        }

        var timePressure = 1 + ((proposal.IgnoredMonths - 60) / 6);
        var supportMargin = proposal.Support - proposal.Opposition;
        var passScore = supportMargin + (behavior.AutoPassBias * 6) + (relevance / 4) + timePressure;
        var vetoScore = (-supportMargin) + (behavior.AutoVetoBias * 6) + ((100 - relevance) / 4) + timePressure;
        var abstainScore = (behavior.AbstainBias * 8) + Math.Max(0, timePressure - behavior.IgnoreTolerance) + Math.Abs(supportMargin / 4);

        if (passScore >= vetoScore && passScore >= abstainScore && passScore >= 30)
        {
            return LawProposalStatus.Passed;
        }

        if (vetoScore >= passScore && vetoScore >= abstainScore && vetoScore >= 30)
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
                Status = status
            }
        ];
    }

    private static int ScoreWhen(bool eligible, int score)
    {
        return eligible ? Math.Clamp(score, 0, 100) : 0;
    }

    private static bool IsBlockedByEnactedLaw(Polity polity, LawProposalDefinition definition)
    {
        if (polity.EnactedLaws.Any(law =>
                law.IsActive &&
                (string.Equals(law.DefinitionId, definition.Id, StringComparison.Ordinal) ||
                 string.Equals(law.Title, definition.Title, StringComparison.Ordinal) ||
                 (!string.IsNullOrWhiteSpace(definition.ConflictSlot) &&
                  string.Equals(law.ConflictSlot, definition.ConflictSlot, StringComparison.Ordinal) &&
                  !definition.RequiredActiveDefinitionIds.Contains(law.DefinitionId, StringComparer.Ordinal)) ||
                 definition.ConflictingDefinitionIds.Contains(law.DefinitionId, StringComparer.Ordinal))))
        {
            return true;
        }

        return false;
    }

    private static bool IsEligibleByLawState(Polity polity, LawProposalDefinition definition)
    {
        if (definition.RequiredActiveDefinitionIds.Count == 0)
        {
            return true;
        }

        return definition.RequiredActiveDefinitionIds.All(requiredId =>
            polity.EnactedLaws.Any(law =>
                law.IsActive &&
                string.Equals(law.DefinitionId, requiredId, StringComparison.Ordinal)));
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

    private static void ApplyPassedLaw(World world, Polity polity, LawProposal proposal)
    {
        var definition = Definitions.First(definition => string.Equals(definition.Id, proposal.DefinitionId, StringComparison.Ordinal));
        var behavior = GovernmentFormLawBehaviorCatalog.Get(polity.GovernmentForm);
        var enactedLaw = new EnactedLaw
        {
            DefinitionId = proposal.DefinitionId,
            Title = proposal.Title,
            Summary = proposal.Summary,
            Category = proposal.Category,
            ConflictGroup = definition.ConflictGroup,
            ConflictSlot = definition.ConflictSlot,
            ImpactScale = proposal.ImpactScale,
            EnactedOnYear = world.CurrentYear,
            EnactedOnMonth = world.CurrentMonth,
            EnforcementStrength = behavior.EnforcementTendency,
            ComplianceLevel = behavior.ComplianceTendency,
            IsActive = true
        };

        foreach (var existing in polity.EnactedLaws.Where(law =>
                     string.Equals(law.DefinitionId, proposal.DefinitionId, StringComparison.Ordinal) ||
                     string.Equals(law.Title, proposal.Title, StringComparison.Ordinal) ||
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
        PopulationGroup aggregateGroup,
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
                        GetSourceCategoryWeight(source, definition.Category) +
                        ResolveSourceStateWeight(aggregateGroup, source, definition.Category) +
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

    private static int GetSourceCategoryWeight(ProposalBackingSource source, LawProposalCategory category)
    {
        return PoliticalBlocCatalog.GetCategoryWeight(source, category);
    }

    private static int ResolveSourceStateWeight(PopulationGroup group, ProposalBackingSource source, LawProposalCategory category)
    {
        return source switch
        {
            ProposalBackingSource.Priests when category is LawProposalCategory.Faith or LawProposalCategory.Symbolic or LawProposalCategory.Punishment
                => group.Pressures.MigrationPressure / 10 + group.Pressures.ThreatPressure / 20,
            ProposalBackingSource.Warriors when category is LawProposalCategory.Military or LawProposalCategory.Order or LawProposalCategory.Movement
                => group.Pressures.ThreatPressure / 8 + group.Pressures.MigrationPressure / 12,
            ProposalBackingSource.Merchants when category is LawProposalCategory.Trade or LawProposalCategory.Movement or LawProposalCategory.Food
                => group.Pressures.MigrationPressure / 10 + Math.Max(0, 50 - group.Pressures.ThreatPressure) / 12,
            ProposalBackingSource.CommonFolk when category is LawProposalCategory.Food or LawProposalCategory.Custom
                => group.Pressures.FoodPressure / 8 + group.Pressures.OvercrowdingPressure / 16,
            ProposalBackingSource.FrontierSettlers when category is LawProposalCategory.Movement or LawProposalCategory.Military or LawProposalCategory.Food
                => group.Pressures.MigrationPressure / 8 + group.Pressures.ThreatPressure / 16,
            ProposalBackingSource.Elders when category is LawProposalCategory.Custom or LawProposalCategory.Order or LawProposalCategory.Symbolic
                => Math.Max(group.Pressures.ThreatPressure, group.Pressures.OvercrowdingPressure) / 12,
            _ => 0
        };
    }

    private static int ResolveBackingSupportShift(
        Polity polity,
        PopulationGroup aggregateGroup,
        LawProposalDefinition definition,
        ProposalBackingSource primary,
        ProposalBackingSource? secondary)
    {
        PoliticalBlocSystem.EnsureBlocs(polity);
        var blocsBySource = polity.PoliticalBlocs.ToDictionary(bloc => bloc.Source);

        var shift = ResolveSingleBlocSupportShift(aggregateGroup, definition, primary, blocsBySource.GetValueOrDefault(primary));
        if (secondary is not null)
        {
            shift += ResolveSingleBlocSupportShift(aggregateGroup, definition, secondary.Value, blocsBySource.GetValueOrDefault(secondary.Value)) / 2;
        }

        if (definition.Category == LawProposalCategory.Faith &&
            primary == ProposalBackingSource.Priests &&
            polity.GovernmentForm == GovernmentForm.Theocracy)
        {
            shift += 4;
        }

        return Math.Clamp(shift, -15, 20);
    }

    private static int ResolveSingleBlocSupportShift(
        PopulationGroup group,
        LawProposalDefinition definition,
        ProposalBackingSource source,
        PoliticalBloc? bloc)
    {
        var shift = source switch
        {
            ProposalBackingSource.Priests when definition.Category is LawProposalCategory.Faith or LawProposalCategory.Symbolic or LawProposalCategory.Punishment
                => group.Pressures.MigrationPressure >= 40 ? 6 : 3,
            ProposalBackingSource.Warriors when definition.Category is LawProposalCategory.Military or LawProposalCategory.Order or LawProposalCategory.Movement
                => group.Pressures.ThreatPressure >= 45 ? 7 : 3,
            ProposalBackingSource.Merchants when definition.Category is LawProposalCategory.Trade or LawProposalCategory.Movement or LawProposalCategory.Food
                => group.Pressures.FoodPressure >= 40 || group.Pressures.MigrationPressure >= 40 ? 5 : 2,
            ProposalBackingSource.CommonFolk when definition.Category is LawProposalCategory.Food or LawProposalCategory.Custom
                => group.Pressures.FoodPressure >= 45 ? 7 : 3,
            ProposalBackingSource.Elders when definition.Category is LawProposalCategory.Custom or LawProposalCategory.Order
                => group.Pressures.ThreatPressure >= 35 || group.Pressures.OvercrowdingPressure >= 35 ? 4 : 2,
            ProposalBackingSource.FrontierSettlers when definition.Category is LawProposalCategory.Movement or LawProposalCategory.Military or LawProposalCategory.Food
                => group.Pressures.MigrationPressure >= 45 ? 5 : 2,
            _ => 0
        };

        if (bloc is null)
        {
            return shift;
        }

        shift += (bloc.Influence - 50) / 8;
        shift += (bloc.Satisfaction - 50) / 6;
        shift += Math.Max(0, 60 - bloc.Satisfaction) / 10;
        return shift;
    }

    private static int ResolveBlocOppositionShift(
        Polity polity,
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

            var categoryWeight = GetSourceCategoryWeight(bloc.Source, definition.Category);
            if (categoryWeight >= 7)
            {
                continue;
            }

            opposition += Math.Max(0, (bloc.Influence - 45) / 10);
            opposition += Math.Max(0, (55 - bloc.Satisfaction) / 12);
            if (categoryWeight <= 2)
            {
                opposition += 2;
            }
        }

        return Math.Clamp(opposition, 0, 18);
    }

    private static int ResolveBlocProposalModifier(Polity polity, PopulationGroup aggregateGroup, LawProposalDefinition definition)
    {
        PoliticalBlocSystem.EnsureBlocs(polity);

        var modifier = 0;
        foreach (var bloc in polity.PoliticalBlocs)
        {
            var categoryWeight = GetSourceCategoryWeight(bloc.Source, definition.Category);
            if (categoryWeight == 0)
            {
                continue;
            }

            modifier += ((categoryWeight - 4) * bloc.Influence) / 18;
            modifier += ((100 - bloc.Satisfaction) * categoryWeight) / 40;
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
            SubsistenceMode = context.LeadGroup?.SubsistenceMode ?? SubsistenceMode.Mixed,
            Pressures = new PressureState
            {
                FoodPressure = context.Pressures.FoodPressure,
                WaterPressure = context.Pressures.WaterPressure,
                ThreatPressure = context.Pressures.ThreatPressure,
                OvercrowdingPressure = context.Pressures.OvercrowdingPressure,
                MigrationPressure = context.Pressures.MigrationPressure
            },
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
}
