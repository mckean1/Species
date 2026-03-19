namespace Species.Domain.Constants;

public static class FaunaSimulationConstants
{
    public const float UnsupportedWaterHabitatSupport = 0.20f;
    public const float CoreBiomeHabitatSupport = 1.00f;
    public const float NonCoreBiomeHabitatSupport = 0.55f;
    public const float FoodFulfillmentWeight = 0.60f;
    public const float HabitatSupportWeight = 0.35f;
    public const float ReproductionGrowthWeight = 0.25f;
    public const float MinimumAdjustmentRate = 0.12f;
    public const float ReproductionAdjustmentWeight = 0.45f;
    public const int ExtinctionThresholdPopulation = 1;
    public const float OmnivorePlantShare = 0.50f;
}
