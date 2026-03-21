using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class RegionalBiologicalProfile
{
    public string SpeciesId { get; init; } = string.Empty;

    public string RegionId { get; init; } = string.Empty;

    public BiologicalTraitProfile Traits { get; set; } = new();

    public BiologicalPressureMemory PressureMemory { get; set; } = new();

    public DivergenceStage DivergenceStage { get; set; }

    public int DivergenceScore { get; set; }

    public int ContinuityMonths { get; set; }

    public int SuccessfulMonths { get; set; }

    public int SpeciationProgress { get; set; }

    public int ViabilityScore { get; set; } = 50;

    public bool IsExtinct { get; set; }

    public int LastPopulation { get; set; }

    public float HungerPressure { get; set; }

    public int ShortageMonths { get; set; }

    public float FeedingMomentum { get; set; }

    public FoodStressState FoodStressState { get; set; }

    public int StableSupportMonths { get; set; }

    public int SocialCoordinationPressureMonths { get; set; }

    public int LearningPressureMonths { get; set; }

    public int SapienceProgress { get; set; }

    public RegionalBiologicalProfile Clone()
    {
        return new RegionalBiologicalProfile
        {
            SpeciesId = SpeciesId,
            RegionId = RegionId,
            Traits = Traits.Clone(),
            PressureMemory = PressureMemory.Clone(),
            DivergenceStage = DivergenceStage,
            DivergenceScore = DivergenceScore,
            ContinuityMonths = ContinuityMonths,
            SuccessfulMonths = SuccessfulMonths,
            SpeciationProgress = SpeciationProgress,
            ViabilityScore = ViabilityScore,
            IsExtinct = IsExtinct,
            LastPopulation = LastPopulation,
            HungerPressure = HungerPressure,
            ShortageMonths = ShortageMonths,
            FeedingMomentum = FeedingMomentum,
            FoodStressState = FoodStressState,
            StableSupportMonths = StableSupportMonths,
            SocialCoordinationPressureMonths = SocialCoordinationPressureMonths,
            LearningPressureMonths = LearningPressureMonths,
            SapienceProgress = SapienceProgress
        };
    }
}
