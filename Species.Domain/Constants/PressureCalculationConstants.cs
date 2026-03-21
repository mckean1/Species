namespace Species.Domain.Constants;

public static class PressureCalculationConstants
{
    public const float PressureScaleMaximum = 100.0f;
    public const float StoredFoodMonthsSafe = 2.0f;
    public const float LocalFoodSupportScale = 200.0f;
    public const float ThreatCarnivoreScale = 150.0f;
    public const float OvercrowdingSupportScale = 1.20f;
    public const float FoodReserveWeight = 24.0f;
    public const float FoodEcologyWeight = 30.0f;
    public const float FoodHungerCarryoverWeight = 18.0f;
    public const float FoodShortageMonthWeight = 4.0f;
    public const float FoodMonthlyRelief = 12.0f;
    public const float WaterMonthlyRelief = 22.0f;
    public const float WaterStableSupportThreshold = 72.0f;
    public const float WaterCriticalSupportThreshold = 55.0f;
    public const float WaterScarcityWeight = 1.10f;
    public const float WaterCriticalWeight = 1.45f;
    public const float ThreatMonthlyRelief = 18.0f;
    public const float OvercrowdingMonthlyRelief = 18.0f;
    public const float MigrationMonthlyRelief = 12.0f;
    public const float MigrationFoodWeight = 0.35f;
    public const float MigrationWaterWeight = 0.15f;
    public const float MigrationThreatWeight = 0.20f;
    public const float MigrationOvercrowdingWeight = 0.30f;
    public const int PersistentPressureDecayRate = 4;
    public const int MigrationPressureDecayRate = 5;
    public const int TransientPressureDecayRate = 6;
}
