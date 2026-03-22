namespace Species.Domain.Constants;

public static class ChronicleConstants
{
    public const int RevealDelayMonths = 1;
    public const int MaxRevealPerTick = 3;
    public const int MaxVisibleFeedEntries = 12;
    public const int MeaningfulShortageThreshold = 10;
    public const int MeaningfulDeclineThreshold = 5;
    public const int HardshipWarningDisplayThreshold = 78;
    public const int PressureConcernDisplayThreshold = 60;
    public const int StablePressureDisplayThreshold = 45;
    public const int PressureTransitionDisplayThreshold = 60;
    public const int MeaningfulPressureDisplayDelta = 10;
    public const int ActiveAlertVisibilityMonths = 2;
}
