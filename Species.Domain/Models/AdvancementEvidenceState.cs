namespace Species.Domain.Models;

public sealed class AdvancementEvidenceState
{
    public int SuccessfulGatheringWithKnowledgeMonths { get; set; }

    public int SuccessfulHuntingWithKnowledgeMonths { get; set; }

    public int SurplusStoredFoodMonths { get; set; }

    public int KnownRouteTravelMonths { get; set; }

    public int SuccessfulResidenceWithRegionKnowledgeMonths { get; set; }

    public int MaterialPracticeMonths { get; set; }

    public int StoragePressureMonths { get; set; }

    public int ShelterReadinessMonths { get; set; }

    public int StabilityMonths { get; set; }

    public int ContactLearningMonths { get; set; }

    public Dictionary<string, float> AdvancementProgressById { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, float> AdoptionProgressById { get; init; } = new(StringComparer.Ordinal);

    public AdvancementEvidenceState Clone()
    {
        return new AdvancementEvidenceState
        {
            SuccessfulGatheringWithKnowledgeMonths = SuccessfulGatheringWithKnowledgeMonths,
            SuccessfulHuntingWithKnowledgeMonths = SuccessfulHuntingWithKnowledgeMonths,
            SurplusStoredFoodMonths = SurplusStoredFoodMonths,
            KnownRouteTravelMonths = KnownRouteTravelMonths,
            SuccessfulResidenceWithRegionKnowledgeMonths = SuccessfulResidenceWithRegionKnowledgeMonths,
            MaterialPracticeMonths = MaterialPracticeMonths,
            StoragePressureMonths = StoragePressureMonths,
            ShelterReadinessMonths = ShelterReadinessMonths,
            StabilityMonths = StabilityMonths,
            ContactLearningMonths = ContactLearningMonths,
            AdvancementProgressById = new Dictionary<string, float>(AdvancementProgressById, StringComparer.Ordinal),
            AdoptionProgressById = new Dictionary<string, float>(AdoptionProgressById, StringComparer.Ordinal)
        };
    }
}
