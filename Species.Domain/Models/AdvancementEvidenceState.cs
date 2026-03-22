namespace Species.Domain.Models;

public sealed class AdvancementEvidenceState
{
    public int ForagingOpportunityMonths { get; set; }

    public int SmallPreyOpportunityMonths { get; set; }

    public int LargePreyOpportunityMonths { get; set; }

    public int AquaticOpportunityMonths { get; set; }

    public int SurplusOpportunityMonths { get; set; }

    public int SpoilagePressureMonths { get; set; }

    public int StoneAccessMonths { get; set; }

    public int HideAccessMonths { get; set; }

    public int FiberAccessMonths { get; set; }

    public int FoodPressureMonths { get; set; }

    public int MaterialNeedMonths { get; set; }

    public int StabilityMonths { get; set; }

    public int AnchoredContinuityMonths { get; set; }

    public int OrganizationalReadinessMonths { get; set; }

    public Dictionary<string, float> AdvancementProgressById { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, float> AdoptionProgressById { get; init; } = new(StringComparer.Ordinal);

    public AdvancementEvidenceState Clone()
    {
        return new AdvancementEvidenceState
        {
            ForagingOpportunityMonths = ForagingOpportunityMonths,
            SmallPreyOpportunityMonths = SmallPreyOpportunityMonths,
            LargePreyOpportunityMonths = LargePreyOpportunityMonths,
            AquaticOpportunityMonths = AquaticOpportunityMonths,
            SurplusOpportunityMonths = SurplusOpportunityMonths,
            SpoilagePressureMonths = SpoilagePressureMonths,
            StoneAccessMonths = StoneAccessMonths,
            HideAccessMonths = HideAccessMonths,
            FiberAccessMonths = FiberAccessMonths,
            FoodPressureMonths = FoodPressureMonths,
            MaterialNeedMonths = MaterialNeedMonths,
            StabilityMonths = StabilityMonths,
            AnchoredContinuityMonths = AnchoredContinuityMonths,
            OrganizationalReadinessMonths = OrganizationalReadinessMonths,
            AdvancementProgressById = new Dictionary<string, float>(AdvancementProgressById, StringComparer.Ordinal),
            AdoptionProgressById = new Dictionary<string, float>(AdoptionProgressById, StringComparer.Ordinal)
        };
    }
}
