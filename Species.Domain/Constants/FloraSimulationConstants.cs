namespace Species.Domain.Constants;

public static class FloraSimulationConstants
{
    public const float UnsupportedWaterTargetMultiplier = 0.0f;
    public const float CoreBiomeFitMultiplier = 1.0f;
    public const float NonCoreBiomeFitMultiplier = 0.40f;
    public const float BaseTargetContribution = 0.15f;
    public const float GrowthRateTargetWeight = 0.45f;
    public const float FoodValueTargetWeight = 0.05f;
    public const float FertilityTargetWeight = 0.35f;
    public const float CoreBiomeTargetBonus = 0.20f;
    public const float MinimumMonthlyAdjustmentRate = 0.10f;
    public const float GrowthRateAdjustmentWeight = 0.45f;
    public const float UnsupportedWaterDeclineRate = 0.65f;
    public const int ExtinctionThresholdPopulation = 1;
    public const float FertilityFitFalloffRange = 0.40f;
}
