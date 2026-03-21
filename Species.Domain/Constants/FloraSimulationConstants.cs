namespace Species.Domain.Constants;

public static class FloraSimulationConstants
{
    // These weights shape flora as the ecological base layer rather than a passive abundance bucket.
    public const float CoreBiomeFitMultiplier = 1.00f;
    public const float NonCoreBiomeFitMultiplier = 0.42f;
    public const float SupportTargetWeight = 0.46f;
    public const float AbundanceTargetWeight = 0.24f;
    public const float ProtoPressureTargetWeight = 0.14f;
    public const float VacancyTargetWeight = 0.16f;
    public const float CollapseRecoveryTargetWeight = 0.08f;
    public const float MinimumGrowthRate = 0.06f;
    public const float GrowthRateWeight = 0.36f;
    public const float RecoveryRateWeight = 0.36f;
    public const float BiomassGrowthWeight = 0.10f;
    public const float RecoveryOccupancyWeight = 0.28f;
    public const float CollapseRecoveryGrowthWeight = 0.12f;
    public const float HarshnessDeclineWeight = 0.38f;
    public const float ConsumptionDeclineWeight = 0.42f;
    public const float PoorSupportDeclineWeight = 0.30f;
    public const float UnderTargetDeclineReliefWeight = 0.22f;
    public const float SpreadThreshold = 0.18f;
    public const float SpreadSourceOccupancyThreshold = 0.22f;
    public const float SpreadNeighborPressureWeight = 0.14f;
    public const float SpreadPopulationFactor = 0.12f;
    public const int MinimumSpreadPopulation = 4;
    public const int ExtinctionThresholdPopulation = 1;
    public const float FertilityFitFalloffRange = 0.40f;
}
