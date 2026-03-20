using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public static class PoliticalBlocCatalog
{
    private static readonly IReadOnlyDictionary<ProposalBackingSource, IReadOnlyDictionary<LawProposalCategory, int>> CategoryWeights =
        new Dictionary<ProposalBackingSource, IReadOnlyDictionary<LawProposalCategory, int>>
        {
            [ProposalBackingSource.Elders] = new Dictionary<LawProposalCategory, int>
            {
                [LawProposalCategory.Custom] = 10,
                [LawProposalCategory.Order] = 8,
                [LawProposalCategory.Symbolic] = 4,
                [LawProposalCategory.Food] = 3
            },
            [ProposalBackingSource.Priests] = new Dictionary<LawProposalCategory, int>
            {
                [LawProposalCategory.Faith] = 10,
                [LawProposalCategory.Symbolic] = 8,
                [LawProposalCategory.Punishment] = 7
            },
            [ProposalBackingSource.Warriors] = new Dictionary<LawProposalCategory, int>
            {
                [LawProposalCategory.Military] = 10,
                [LawProposalCategory.Order] = 8,
                [LawProposalCategory.Movement] = 7,
                [LawProposalCategory.Punishment] = 4
            },
            [ProposalBackingSource.Merchants] = new Dictionary<LawProposalCategory, int>
            {
                [LawProposalCategory.Trade] = 10,
                [LawProposalCategory.Movement] = 7,
                [LawProposalCategory.Food] = 6
            },
            [ProposalBackingSource.CommonFolk] = new Dictionary<LawProposalCategory, int>
            {
                [LawProposalCategory.Food] = 10,
                [LawProposalCategory.Custom] = 7,
                [LawProposalCategory.Order] = 4
            },
            [ProposalBackingSource.FrontierSettlers] = new Dictionary<LawProposalCategory, int>
            {
                [LawProposalCategory.Movement] = 10,
                [LawProposalCategory.Military] = 8,
                [LawProposalCategory.Food] = 6,
                [LawProposalCategory.Order] = 4
            }
        };

    public static IReadOnlyList<PoliticalBloc> CreateInitialBlocs(GovernmentForm governmentForm)
    {
        var behavior = GovernmentFormLawBehaviorCatalog.Get(governmentForm);
        return Enum.GetValues<ProposalBackingSource>()
            .Select(source =>
            {
                var affinity = behavior.GetBackingSourceWeight(source);
                return new PoliticalBloc
                {
                    Source = source,
                    Influence = Math.Clamp(18 + (affinity * 6), 10, 90),
                    Satisfaction = Math.Clamp(42 + (affinity * 3), 20, 80)
                };
            })
            .OrderByDescending(bloc => bloc.Influence)
            .ThenBy(bloc => bloc.Source)
            .ToArray();
    }

    public static int GetCategoryWeight(ProposalBackingSource source, LawProposalCategory category)
    {
        if (!CategoryWeights.TryGetValue(source, out var weights))
        {
            return 0;
        }

        return weights.TryGetValue(category, out var weight) ? weight : 0;
    }
}
