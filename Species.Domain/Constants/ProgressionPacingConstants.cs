namespace Species.Domain.Constants;

public static class ProgressionPacingConstants
{
    public const float StageThreshold = 100.0f;

    public const float DiscoveryMonthlyGainCap = 14.0f;
    public const float DiscoveryMonthlyDecay = 1.25f;
    public const float DiscoveryMonthlyBudget = 20.0f;

    public const float AdvancementMonthlyGainCap = 10.0f;
    public const float AdvancementMonthlyDecay = 0.90f;
    public const float AdvancementMonthlyBudget = 16.0f;

    public const float AdoptionMonthlyGainCap = 7.0f;
    public const float AdoptionMonthlyDecay = 0.70f;
    public const float AdoptionMonthlyBudget = 10.0f;
}
