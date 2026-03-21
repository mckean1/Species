namespace Species.Domain.Constants;

public static class ProtoGenesisConstants
{
    // Readiness should require sustained strong substrate, not a brief spike.
    public const float FloraReadinessPressureThreshold = 0.78f;
    public const float FaunaReadinessPressureThreshold = 0.82f;
    public const float FloraSuitabilityThreshold = 0.60f;
    public const float FaunaSuitabilityThreshold = 0.58f;
    public const float FloraVacancyThreshold = 0.26f;
    public const float FaunaVacancyThreshold = 0.24f;
    public const float FaunaFoodSupportThreshold = 0.40f;
    public const float FaunaActiveFoodSupportThreshold = 0.20f;
    public const int FloraReadinessMonthsRequired = 8;
    public const int FaunaReadinessMonthsRequired = 10;

    // Trigger caps are intentionally low; readiness and stability should do most of the work.
    public const float FloraMonthlyTriggerChanceCap = 0.14f;
    public const float FaunaMonthlyTriggerChanceCap = 0.09f;

    // Post-attempt cooldowns and pressure drops are the main anti-spam guardrails.
    public const int FloraCooldownMonthsOnSuccess = 24;
    public const int FaunaCooldownMonthsOnSuccess = 30;
    public const int CooldownMonthsOnFailure = 10;
    public const float FloraPressureReductionOnSuccess = 0.32f;
    public const float FaunaPressureReductionOnSuccess = 0.36f;
    public const float PressureReductionOnFailure = 0.10f;
    public const float FullnessSuppressionStart = 0.54f;
    public const float FullnessSuppressionEnd = 0.84f;
}
