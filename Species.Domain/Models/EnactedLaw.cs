using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class EnactedLaw
{
    public string DefinitionId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string IntentSummary { get; init; } = string.Empty;

    public string TradeoffSummary { get; init; } = string.Empty;

    public LawProposalCategory Category { get; init; }

    public LawConflictGroup ConflictGroup { get; init; }

    public string ConflictSlot { get; init; } = string.Empty;

    public int ImpactScale { get; init; }

    public int EnactedOnYear { get; init; }

    public int EnactedOnMonth { get; init; }

    public int EnforcementStrength { get; set; } = 50;

    public int ComplianceLevel { get; set; } = 50;

    public int CoreEffectiveness { get; set; } = 50;

    public int PeripheralEffectiveness { get; set; } = 50;

    public int ResistanceLevel { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public EnactedLaw Clone()
    {
        return new EnactedLaw
        {
            DefinitionId = DefinitionId,
            Title = Title,
            Summary = Summary,
            IntentSummary = IntentSummary,
            TradeoffSummary = TradeoffSummary,
            Category = Category,
            ConflictGroup = ConflictGroup,
            ConflictSlot = ConflictSlot,
            ImpactScale = ImpactScale,
            EnactedOnYear = EnactedOnYear,
            EnactedOnMonth = EnactedOnMonth,
            EnforcementStrength = EnforcementStrength,
            ComplianceLevel = ComplianceLevel,
            CoreEffectiveness = CoreEffectiveness,
            PeripheralEffectiveness = PeripheralEffectiveness,
            ResistanceLevel = ResistanceLevel,
            IsActive = IsActive
        };
    }
}
