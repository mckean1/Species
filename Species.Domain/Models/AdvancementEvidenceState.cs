namespace Species.Domain.Models;

public sealed class AdvancementEvidenceState
{
    public int SuccessfulGatheringWithKnowledgeMonths { get; set; }

    public int SuccessfulHuntingWithKnowledgeMonths { get; set; }

    public int SurplusStoredFoodMonths { get; set; }

    public int KnownRouteTravelMonths { get; set; }

    public int SuccessfulResidenceWithRegionKnowledgeMonths { get; set; }

    public AdvancementEvidenceState Clone()
    {
        return new AdvancementEvidenceState
        {
            SuccessfulGatheringWithKnowledgeMonths = SuccessfulGatheringWithKnowledgeMonths,
            SuccessfulHuntingWithKnowledgeMonths = SuccessfulHuntingWithKnowledgeMonths,
            SurplusStoredFoodMonths = SurplusStoredFoodMonths,
            KnownRouteTravelMonths = KnownRouteTravelMonths,
            SuccessfulResidenceWithRegionKnowledgeMonths = SuccessfulResidenceWithRegionKnowledgeMonths
        };
    }
}
