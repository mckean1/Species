using Species.Domain.Enums;

namespace Species.Domain.Simulation;

public sealed record GovernmentFormProposalBehavior(
    GovernmentForm GovernmentForm,
    IReadOnlyDictionary<LawProposalCategory, int> CategoryWeighting,
    IReadOnlyDictionary<ProposalBackingSource, int> BackingSourceWeighting,
    int PlayerDecisionStrength,
    int IgnoreTolerance,
    int AbstainBias,
    int AutoPassBias,
    int AutoVetoBias,
    int IndecisionPenaltyStrength,
    int EnforcementTendency,
    int ComplianceTendency,
    int ExtremityAllowance)
{
    public int GetCategoryWeight(LawProposalCategory category)
    {
        return CategoryWeighting.TryGetValue(category, out var weight) ? weight : 0;
    }

    public int GetBackingSourceWeight(ProposalBackingSource source)
    {
        return BackingSourceWeighting.TryGetValue(source, out var weight) ? weight : 0;
    }
}
