namespace Species.Domain.Models;

public sealed class SocialMemoryState
{
    public int SettlementContinuityMonths { get; set; }

    public int SeasonalMobilityMonths { get; set; }

    public int HardshipMonths { get; set; }

    public int SurplusMonths { get; set; }

    public int CoordinatedGovernanceMonths { get; set; }

    public int PeripheralStrainMonths { get; set; }

    public int RiverSettlementMonths { get; set; }

    public int FrontierExposureMonths { get; set; }

    public SocialMemoryState Clone()
    {
        return new SocialMemoryState
        {
            SettlementContinuityMonths = SettlementContinuityMonths,
            SeasonalMobilityMonths = SeasonalMobilityMonths,
            HardshipMonths = HardshipMonths,
            SurplusMonths = SurplusMonths,
            CoordinatedGovernanceMonths = CoordinatedGovernanceMonths,
            PeripheralStrainMonths = PeripheralStrainMonths,
            RiverSettlementMonths = RiverSettlementMonths,
            FrontierExposureMonths = FrontierExposureMonths
        };
    }
}
