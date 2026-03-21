namespace Species.Domain.Constants;

public static class ProtoGenesisConstants
{
    // Readiness should require sustained strong substrate, not a brief spike.
    public const float FloraReadinessPressureThreshold = 0.80f;
    public const float FaunaReadinessPressureThreshold = 0.84f;
    public const float FloraSuitabilityThreshold = 0.60f;
    public const float FaunaSuitabilityThreshold = 0.60f;
    public const float FloraVacancyThreshold = 0.28f;
    public const float FaunaVacancyThreshold = 0.26f;
    public const float FaunaFoodSupportThreshold = 0.46f;
    public const float FaunaActiveFoodSupportThreshold = 0.26f;
    public const int FloraReadinessMonthsRequired = 10;
    public const int FaunaReadinessMonthsRequired = 12;
    public const int FloraCandidateMonthsRequired = 4;
    public const int FaunaCandidateMonthsRequired = 5;
    public const float FloraCandidatePressureMargin = 0.04f;
    public const float FaunaCandidatePressureMargin = 0.06f;
    public const float CandidateSuppressionFloor = 0.22f;
    public const int FloraMinimumEstablishmentPopulation = 4;
    public const int FaunaMinimumEstablishmentPopulation = 3;
    public const int FloraEstablishmentViabilityThreshold = 60;
    public const int FaunaEstablishmentViabilityThreshold = 64;

    // Trigger caps are intentionally low; readiness and stability should do most of the work.
    public const float FloraMonthlyTriggerChanceCap = 0.11f;
    public const float FaunaMonthlyTriggerChanceCap = 0.07f;

    // Post-attempt cooldowns and pressure drops are the main anti-spam guardrails.
    public const int FloraCooldownMonthsOnSuccess = 30;
    public const int FaunaCooldownMonthsOnSuccess = 36;
    public const int CooldownMonthsOnFailure = 12;
    public const float FloraPressureReductionOnSuccess = 0.36f;
    public const float FaunaPressureReductionOnSuccess = 0.40f;
    public const float PressureReductionOnFailure = 0.12f;
    public const float FullnessSuppressionStart = 0.50f;
    public const float FullnessSuppressionEnd = 0.80f;
}
