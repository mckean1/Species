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

    public List<InterPolityRelation> InterPolityRelations { get; init; } = [];

    public string ParentPolityId { get; set; } = string.Empty;

    public List<PoliticalAttachment> PoliticalAttachments { get; init; } = [];

    public List<PoliticalHistoryRecord> PoliticalHistory { get; init; } = [];

    public GovernanceState Governance { get; set; } = new();

    public ExternalPressureState ExternalPressure { get; set; } = new();

    public PoliticalScaleState ScaleState { get; set; } = new();

    public SocialMemoryState SocialMemory { get; set; } = new();

    public SocialIdentityState SocialIdentity { get; set; } = new();

    public MaterialStockpile MaterialStores { get; set; } = new();

    public MaterialProductionState MaterialProduction { get; set; } = new();

    public FoodAccountingSnapshot FoodAccounting { get; set; } = new();

    public List<PolitySpeciesAwarenessState> SpeciesAwareness { get; init; } = [];

    public int MaterialShortageMonths { get; set; }

    public int MaterialSurplusMonths { get; set; }

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
            RegionalPresences = RegionalPresences.Select(presence => presence.Clone()).ToList(),
            InterPolityRelations = InterPolityRelations.Select(relation => relation.Clone()).ToList(),
            ParentPolityId = ParentPolityId,
            PoliticalAttachments = PoliticalAttachments.Select(attachment => attachment.Clone()).ToList(),
            PoliticalHistory = PoliticalHistory.Select(record => record.Clone()).ToList(),
            Governance = Governance.Clone(),
            ExternalPressure = ExternalPressure.Clone(),
            ScaleState = ScaleState.Clone(),
            SocialMemory = SocialMemory.Clone(),
            SocialIdentity = SocialIdentity.Clone(),
            MaterialStores = MaterialStores.Clone(),
            MaterialProduction = MaterialProduction.Clone(),
            FoodAccounting = FoodAccounting.Clone(),
            SpeciesAwareness = SpeciesAwareness.Select(state => state.Clone()).ToList(),
            MaterialShortageMonths = MaterialShortageMonths,
            MaterialSurplusMonths = MaterialSurplusMonths
        };
    }
}
