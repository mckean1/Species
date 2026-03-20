namespace Species.Domain.Constants;

public static class GroupSurvivalConstants
{
    public const float FoodNeedPerPopulationUnit = 0.50f;
    public const int FoodUnitScale = 10;
    public const float StarvationLossSeverity = 0.65f;
    public const int ModerateHardshipPressureThreshold = 55;
    public const int SevereHardshipPressureThreshold = 75;
    public const int CriticalHardshipPressureThreshold = 90;
    public const float ModerateHardshipStarvationBonus = 0.05f;
    public const float SevereHardshipStarvationBonus = 0.10f;
    public const float CriticalHardshipStarvationBonus = 0.15f;
    public const float MaxStarvationLossSeverity = 0.85f;
}
