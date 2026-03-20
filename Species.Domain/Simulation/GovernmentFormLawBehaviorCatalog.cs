using Species.Domain.Enums;

namespace Species.Domain.Simulation;

public static class GovernmentFormLawBehaviorCatalog
{
    public static IReadOnlyDictionary<GovernmentForm, GovernmentFormProposalBehavior> Behaviors { get; } =
        new Dictionary<GovernmentForm, GovernmentFormProposalBehavior>
        {
            [GovernmentForm.TribalClanRule] = new(
                GovernmentForm.TribalClanRule,
                new Dictionary<LawProposalCategory, int>
                {
                    [LawProposalCategory.Custom] = 10,
                    [LawProposalCategory.Military] = 9,
                    [LawProposalCategory.Faith] = 7,
                    [LawProposalCategory.Food] = 5,
                    [LawProposalCategory.Order] = 5,
                    [LawProposalCategory.Symbolic] = 4
                },
                new Dictionary<ProposalBackingSource, int>
                {
                    [ProposalBackingSource.Elders] = 10,
                    [ProposalBackingSource.Warriors] = 9,
                    [ProposalBackingSource.CommonFolk] = 8,
                    [ProposalBackingSource.FrontierSettlers] = 5,
                    [ProposalBackingSource.Priests] = 4,
                    [ProposalBackingSource.Merchants] = 2
                },
                7, 5, 2, 1, 1, 2, 52, 58, 3, 58, 62, 46, 35, 70),
            [GovernmentForm.Confederation] = new(
                GovernmentForm.Confederation,
                new Dictionary<LawProposalCategory, int>
                {
                    [LawProposalCategory.Custom] = 9,
                    [LawProposalCategory.Military] = 7,
                    [LawProposalCategory.Movement] = 7,
                    [LawProposalCategory.Order] = 6,
                    [LawProposalCategory.Food] = 5
                },
                new Dictionary<ProposalBackingSource, int>
                {
                    [ProposalBackingSource.Elders] = 9,
                    [ProposalBackingSource.FrontierSettlers] = 9,
                    [ProposalBackingSource.CommonFolk] = 8,
                    [ProposalBackingSource.Warriors] = 5,
                    [ProposalBackingSource.Merchants] = 3,
                    [ProposalBackingSource.Priests] = 2
                },
                4, 9, 5, 1, 1, 1, 42, 60, 2, 54, 58, 40, 28, 78),
            [GovernmentForm.CouncilRule] = new(
                GovernmentForm.CouncilRule,
                new Dictionary<LawProposalCategory, int>
                {
                    [LawProposalCategory.Trade] = 10,
                    [LawProposalCategory.Food] = 8,
                    [LawProposalCategory.Order] = 7,
                    [LawProposalCategory.Movement] = 5,
                    [LawProposalCategory.Custom] = 4
                },
                new Dictionary<ProposalBackingSource, int>
                {
                    [ProposalBackingSource.Elders] = 9,
                    [ProposalBackingSource.Merchants] = 9,
                    [ProposalBackingSource.CommonFolk] = 8,
                    [ProposalBackingSource.FrontierSettlers] = 4,
                    [ProposalBackingSource.Warriors] = 3,
                    [ProposalBackingSource.Priests] = 2
                },
                5, 8, 4, 1, 1, 1, 48, 52, 3, 56, 55, 48, 45, 65),
            [GovernmentForm.MerchantRule] = new(
                GovernmentForm.MerchantRule,
                new Dictionary<LawProposalCategory, int>
                {
                    [LawProposalCategory.Trade] = 10,
                    [LawProposalCategory.Movement] = 9,
                    [LawProposalCategory.Food] = 7,
                    [LawProposalCategory.Order] = 5,
                    [LawProposalCategory.Punishment] = 3
                },
                new Dictionary<ProposalBackingSource, int>
                {
                    [ProposalBackingSource.Merchants] = 10,
                    [ProposalBackingSource.Elders] = 7,
                    [ProposalBackingSource.CommonFolk] = 6,
                    [ProposalBackingSource.FrontierSettlers] = 5,
                    [ProposalBackingSource.Warriors] = 3,
                    [ProposalBackingSource.Priests] = 1
                },
                6, 7, 3, 2, 1, 1, 54, 50, 4, 52, 48, 56, 54, 52),
            [GovernmentForm.FeudalRule] = new(
                GovernmentForm.FeudalRule,
                new Dictionary<LawProposalCategory, int>
                {
                    [LawProposalCategory.Military] = 10,
                    [LawProposalCategory.Order] = 8,
                    [LawProposalCategory.Custom] = 7,
                    [LawProposalCategory.Movement] = 5,
                    [LawProposalCategory.Food] = 4
                },
                new Dictionary<ProposalBackingSource, int>
                {
                    [ProposalBackingSource.Warriors] = 10,
                    [ProposalBackingSource.Elders] = 8,
                    [ProposalBackingSource.FrontierSettlers] = 7,
                    [ProposalBackingSource.CommonFolk] = 4,
                    [ProposalBackingSource.Merchants] = 2,
                    [ProposalBackingSource.Priests] = 2
                },
                8, 5, 2, 2, 1, 2, 60, 46, 5, 49, 45, 66, 68, 34),
            [GovernmentForm.ImperialBureaucracy] = new(
                GovernmentForm.ImperialBureaucracy,
                new Dictionary<LawProposalCategory, int>
                {
                    [LawProposalCategory.Order] = 10,
                    [LawProposalCategory.Trade] = 7,
                    [LawProposalCategory.Movement] = 6,
                    [LawProposalCategory.Food] = 5,
                    [LawProposalCategory.Punishment] = 5
                },
                new Dictionary<ProposalBackingSource, int>
                {
                    [ProposalBackingSource.Elders] = 9,
                    [ProposalBackingSource.Merchants] = 8,
                    [ProposalBackingSource.Warriors] = 7,
                    [ProposalBackingSource.CommonFolk] = 4,
                    [ProposalBackingSource.FrontierSettlers] = 3,
                    [ProposalBackingSource.Priests] = 2
                },
                8, 6, 2, 2, 1, 2, 70, 56, 4, 57, 60, 68, 72, 38),
            [GovernmentForm.Republic] = new(
                GovernmentForm.Republic,
                new Dictionary<LawProposalCategory, int>
                {
                    [LawProposalCategory.Order] = 8,
                    [LawProposalCategory.Trade] = 8,
                    [LawProposalCategory.Custom] = 6,
                    [LawProposalCategory.Movement] = 6,
                    [LawProposalCategory.Food] = 5
                },
                new Dictionary<ProposalBackingSource, int>
                {
                    [ProposalBackingSource.Merchants] = 9,
                    [ProposalBackingSource.CommonFolk] = 9,
                    [ProposalBackingSource.Elders] = 7,
                    [ProposalBackingSource.Warriors] = 4,
                    [ProposalBackingSource.FrontierSettlers] = 3,
                    [ProposalBackingSource.Priests] = 2
                },
                6, 7, 4, 1, 1, 2, 52, 60, 3, 63, 61, 47, 42, 76),
            [GovernmentForm.AbsoluteRule] = new(
                GovernmentForm.AbsoluteRule,
                new Dictionary<LawProposalCategory, int>
                {
                    [LawProposalCategory.Order] = 10,
                    [LawProposalCategory.Military] = 9,
                    [LawProposalCategory.Punishment] = 8,
                    [LawProposalCategory.Movement] = 7
                },
                new Dictionary<ProposalBackingSource, int>
                {
                    [ProposalBackingSource.Warriors] = 10,
                    [ProposalBackingSource.Elders] = 8,
                    [ProposalBackingSource.FrontierSettlers] = 7,
                    [ProposalBackingSource.CommonFolk] = 4,
                    [ProposalBackingSource.Merchants] = 3,
                    [ProposalBackingSource.Priests] = 2
                },
                10, 3, 1, 3, 1, 3, 72, 44, 6, 45, 42, 78, 82, 22),
            [GovernmentForm.DespoticRule] = new(
                GovernmentForm.DespoticRule,
                new Dictionary<LawProposalCategory, int>
                {
                    [LawProposalCategory.Order] = 10,
                    [LawProposalCategory.Punishment] = 10,
                    [LawProposalCategory.Military] = 8,
                    [LawProposalCategory.Movement] = 7,
                    [LawProposalCategory.Food] = 3
                },
                new Dictionary<ProposalBackingSource, int>
                {
                    [ProposalBackingSource.Warriors] = 10,
                    [ProposalBackingSource.Elders] = 8,
                    [ProposalBackingSource.FrontierSettlers] = 7,
                    [ProposalBackingSource.CommonFolk] = 3,
                    [ProposalBackingSource.Merchants] = 2,
                    [ProposalBackingSource.Priests] = 1
                },
                11, 2, 1, 4, 1, 3, 78, 38, 7, 36, 34, 84, 88, 16),
            [GovernmentForm.Theocracy] = new(
                GovernmentForm.Theocracy,
                new Dictionary<LawProposalCategory, int>
                {
                    [LawProposalCategory.Faith] = 10,
                    [LawProposalCategory.Custom] = 7,
                    [LawProposalCategory.Symbolic] = 6,
                    [LawProposalCategory.Punishment] = 5,
                    [LawProposalCategory.Food] = 4
                },
                new Dictionary<ProposalBackingSource, int>
                {
                    [ProposalBackingSource.Priests] = 10,
                    [ProposalBackingSource.Elders] = 8,
                    [ProposalBackingSource.CommonFolk] = 7,
                    [ProposalBackingSource.Warriors] = 4,
                    [ProposalBackingSource.FrontierSettlers] = 3,
                    [ProposalBackingSource.Merchants] = 2
                },
                8, 4, 2, 2, 1, 2, 66, 54, 5, 60, 58, 64, 58, 44)
        };

    public static GovernmentFormProposalBehavior Get(GovernmentForm governmentForm)
    {
        return Behaviors[governmentForm];
    }
}
