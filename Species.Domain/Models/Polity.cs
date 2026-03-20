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

    public string HomeRegionId { get; set; } = string.Empty;

    public string CoreRegionId { get; set; } = string.Empty;

    public string PrimarySettlementId { get; set; } = string.Empty;

    public PolityAnchoringKind AnchoringKind { get; set; }

    public LawProposal? ActiveLawProposal { get; set; }

    public List<LawProposal> LawProposalHistory { get; init; } = [];

    public List<EnactedLaw> EnactedLaws { get; init; } = [];

    public List<PoliticalBloc> PoliticalBlocs { get; init; } = [];

    public List<Settlement> Settlements { get; init; } = [];

    public List<PolityRegionalPresence> RegionalPresences { get; init; } = [];

    public Polity Clone()
    {
        return new Polity
        {
            Id = Id,
            Name = Name,
            GovernmentForm = GovernmentForm,
            MemberGroupIds = [.. MemberGroupIds],
            HomeRegionId = HomeRegionId,
            CoreRegionId = CoreRegionId,
            PrimarySettlementId = PrimarySettlementId,
            AnchoringKind = AnchoringKind,
            ActiveLawProposal = ActiveLawProposal?.Clone(),
            LawProposalHistory = LawProposalHistory.Select(proposal => proposal.Clone()).ToList(),
            EnactedLaws = EnactedLaws.Select(law => law.Clone()).ToList(),
            PoliticalBlocs = PoliticalBlocs.Select(bloc => bloc.Clone()).ToList(),
            Settlements = Settlements.Select(settlement => settlement.Clone()).ToList(),
            RegionalPresences = RegionalPresences.Select(presence => presence.Clone()).ToList()
        };
    }
}
