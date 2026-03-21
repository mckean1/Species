namespace Species.Domain.Constants;

public static class FaunaSimulationConstants
{
    public const float UnsupportedWaterHabitatSupport = 0.20f;
    public const float CoreBiomeHabitatSupport = 1.00f;
    public const float NonCoreBiomeHabitatSupport = 0.55f;
    public const float MinimumFeedingEfficiency = 0.45f;
    public const float FloraConversionEfficiency = 0.80f;
    public const float PreyConversionEfficiency = 0.62f;
    public const float ScavengeConversionEfficiency = 0.36f;
    public const float FallbackDietPenalty = 0.22f;
    public const float EncounterFrictionBase = 0.18f;
    public const float PreyRefugeBase = 0.26f;
    public const float PreyAccessiblePopulationFloor = 0.04f;
    public const float FeedingMomentumRiseRate = 0.28f;
    public const float FeedingMomentumDecayRate = 0.20f;
    public const float HungerRiseRate = 0.50f;
    public const float HungerDecayRate = 0.28f;
    public const float SevereShortageMortalityWeight = 0.08f;
    public const float StarvationMortalityWeight = 0.20f;
    public const float NoFoodMortalityBonus = 0.18f;
    public const float ReproductionFedThreshold = 0.84f;
    public const float MortalityHungerThreshold = 0.35f;
    public const float ReproductionWeight = 0.15f;
    public const float HabitatGrowthWeight = 0.14f;
    public const float HungerMortalityWeight = 0.26f;
    public const float ShortageMortalityWeight = 0.06f;
    public const float MigrationHungerThreshold = 0.34f;
    public const int MigrationShortageMonthsThreshold = 2;
    public const float MigrationShareWeight = 0.16f;
    public const float MigrationShareCap = 0.28f;
    public const float MigrationScoreDeltaRequired = 0.05f;
    public const int MinimumMigrationPopulation = 2;
    public const float PredatorRiskPenaltyWeight = 0.22f;
    public const int ExtinctionThresholdPopulation = 1;
}
