using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class LawProposal
{
    public string Id { get; init; } = string.Empty;

    public string DefinitionId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string ReasonSummary { get; init; } = string.Empty;

    public string TradeoffSummary { get; init; } = string.Empty;

    public LawProposalCategory Category { get; init; }

    public LawProposalStatus Status { get; set; }

    public int Support { get; set; }

    public int Opposition { get; set; }

    public int Urgency { get; set; }

    public int AgeInMonths { get; set; }

    public int IgnoredMonths { get; set; }

    public int ImpactScale { get; init; }

    public GovernmentForm GovernmentForm { get; init; }

    public ProposalBackingSource PrimaryBackingSource { get; init; }

    public ProposalBackingSource? SecondaryBackingSource { get; init; }

    public LawProposal Clone()
    {
        return new LawProposal
        {
            Id = Id,
            DefinitionId = DefinitionId,
            Title = Title,
            Summary = Summary,
            ReasonSummary = ReasonSummary,
            TradeoffSummary = TradeoffSummary,
            Category = Category,
            Status = Status,
            Support = Support,
            Opposition = Opposition,
            Urgency = Urgency,
            AgeInMonths = AgeInMonths,
            IgnoredMonths = IgnoredMonths,
            ImpactScale = ImpactScale,
            GovernmentForm = GovernmentForm,
            PrimaryBackingSource = PrimaryBackingSource,
            SecondaryBackingSource = SecondaryBackingSource
        };
    }
}
