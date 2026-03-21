namespace Species.Domain.Constants;

public static class GroupSurvivalConstants
{
    public const float FoodNeedPerPopulationUnit = 0.50f;
    public const int FoodUnitScale = 10;
    public const float HungerRiseRate = 0.52f;
    public const float HungerDecayRate = 0.22f;
    public const float MinimumBirthRate = 0.0050f;
    public const float MaximumBirthRate = 0.0085f;
    public const float MinimumNaturalDeathRate = 0.0045f;
    public const float MaximumNaturalDeathRate = 0.0080f;
    public const float StableNaturalDeathReliefMultiplier = 0.92f;
    public const int HardshipLossPressureThreshold = 65;
    public const int ThreatLossPressureThreshold = 70;
    public const int OvercrowdingLossPressureThreshold = 80;
    public const float MaximumHardshipLossRate = 0.0060f;
    public const int SevereWaterLossPressureThreshold = 75;
    public const float MaximumWaterLossRate = 0.0100f;
    public const float SevereShortageLossSeverity = 0.06f;
    public const float StarvationLossSeverity = 0.15f;
    public const float NoUsableFoodStarvationBonus = 0.22f;
    public const float ShortageMonthStarvationRamp = 0.025f;
    public const float HungerPressureStarvationRamp = 0.09f;
    public const int ModerateHardshipPressureThreshold = 55;
    public const int SevereHardshipPressureThreshold = 75;
    public const int CriticalHardshipPressureThreshold = 90;
    public const float ModerateHardshipStarvationBonus = 0.05f;
    public const float SevereHardshipStarvationBonus = 0.10f;
    public const float CriticalHardshipStarvationBonus = 0.15f;
    public const float MaxStarvationLossSeverity = 0.85f;
}
