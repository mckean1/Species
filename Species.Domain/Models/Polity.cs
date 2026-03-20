using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class Polity
{
    // Polity owns the player-facing political layer: identity, government form,
    // laws, and blocs. Population groups remain the demographic constituents.
    public string Id { get; init; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public GovernmentForm GovernmentForm { get; set; }

    public List<string> MemberGroupIds { get; init; } = [];

    public LawProposal? ActiveLawProposal { get; set; }

    public List<LawProposal> LawProposalHistory { get; init; } = [];

    public List<EnactedLaw> EnactedLaws { get; init; } = [];

    public List<PoliticalBloc> PoliticalBlocs { get; init; } = [];

    public Polity Clone()
    {
        return new Polity
        {
            Id = Id,
            Name = Name,
            GovernmentForm = GovernmentForm,
            MemberGroupIds = [.. MemberGroupIds],
            ActiveLawProposal = ActiveLawProposal?.Clone(),
            LawProposalHistory = LawProposalHistory.Select(proposal => proposal.Clone()).ToList(),
            EnactedLaws = EnactedLaws.Select(law => law.Clone()).ToList(),
            PoliticalBlocs = PoliticalBlocs.Select(bloc => bloc.Clone()).ToList()
        };
    }
}
