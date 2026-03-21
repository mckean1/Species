namespace Species.Domain.Constants;

public static class PolityConditionConstants
{
    // Pressure bands are soft severity thresholds used for material condition.
    // The retention buffer reduces month-to-month jitter near the edge of a band.
    public const int StablePressureThreshold = 35;
    public const int StrainedPressureThreshold = 58;
    public const int CriticalPressureThreshold = 78;
    public const int CollapsePressureThreshold = 96;
    public const int SeverityRetentionBuffer = 6;

    // Integrity bands are the main polity-stability thresholds.
    public const int CoherentIntegrityThreshold = 70;
    public const int StrainedIntegrityThreshold = 50;
    public const int UnstableIntegrityThreshold = 30;
    public const int IntegrityRetentionBuffer = 5;

    // Governance bands are softer than hard-form gates and should move proportionally.
    public const int FunctionalGovernanceThreshold = 68;
    public const int StrainedGovernanceThreshold = 48;
    public const int FailingGovernanceThreshold = 30;
    public const int GovernanceRetentionBuffer = 4;

    // Spatial thresholds combine with settlement/core presence checks.
    public const int ElevatedMigrationPressureThreshold = 70;
    public const int ExtremeMigrationPressureThreshold = 88;
    public const int CriticalFoodWaterThreshold = 78;
    public const int SustainedShortageMonthsThreshold = 2;
    public const int SustainedCollapseMonthsThreshold = 4;
    public const int RecentCorePresenceMonthsThreshold = 2;
    public const int SeatLossGraceMonthsThreshold = 2;

    // Government-form gates. Retention buffers apply only when holding an existing form.
    public const int SeatRequiredPopulationThreshold = 90;
    public const int FormRetentionPopulationBuffer = 12;
    public const int FormRetentionGovernanceBuffer = 6;
    public const int FormRetentionIntegrityBuffer = 8;
    public const int FormRetentionScaleMonths = 3;

    // UI prioritization should emphasize crisis before softer positives.
    public const int MaxDisplayedIssues = 3;
    public const int MaxDisplayedStrengths = 2;
    public const int MaxDisplayedProblems = 3;
}
