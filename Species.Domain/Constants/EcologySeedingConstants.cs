namespace Species.Domain.Constants;

public static class EcologySeedingConstants
{
    public const int PopulationScale = 100;
    public const float MinimumSeededPopulation = 0.05f;
    public const float FloraBiomeMatchBonus = 0.35f;
    public const float FloraWaterSupportMultiplier = 1.00f;
    public const float FloraNonCoreBiomeMultiplier = 0.35f;
    public const float FloraRandomVarianceRange = 0.12f;
    public const float FaunaBiomeMatchBonus = 0.25f;
    public const float FaunaWaterSupportMultiplier = 1.00f;
    public const float FaunaNonCoreBiomeMultiplier = 0.30f;
    public const float FaunaRandomVarianceRange = 0.10f;
    public const float HerbivoreFloraSupportMultiplier = 0.85f;
    public const float OmnivoreFloraSupportMultiplier = 0.45f;
    public const float OmnivorePreySupportMultiplier = 0.25f;
    public const float CarnivorePreySupportMultiplier = 0.75f;
    public const float WeakFloraSupportThreshold = 0.20f;
    public const float WeakPreySupportThreshold = 0.12f;
}
