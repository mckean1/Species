namespace Species.Domain.Constants;

public static class FaunaSimulationConstants
{
    // These values tune the causal fauna survival loop:
    // feeding success -> stress accumulation -> reproduction / mortality -> migration relief.
    // Preferred diet links are the main support path. Fallback links only cover unmet intake
    // and are intentionally weaker so migration/genesis viability does not overvalue emergency diets.
    public const float UnsupportedWaterHabitatSupport = 0.20f;
    public const float CoreBiomeHabitatSupport = 1.00f;
    public const float NonCoreBiomeHabitatSupport = 0.62f;
    public const float MinimumFeedingEfficiency = 0.45f;
    public const float FloraConversionEfficiency = 0.80f;
    public const float PreyConversionEfficiency = 0.58f;
    public const float ScavengeConversionEfficiency = 0.36f;
    public const float ScavengeAccessibleShareMultiplier = 0.45f;
    public const float ScavengeSupportShareMultiplier = 0.20f;
    public const float PreySupportAccessMultiplier = 0.66f;
    public const float FallbackDietPenalty = 0.25f;
    public const float EncounterFrictionBase = 0.17f;
    public const float PreyRefugeBase = 0.30f;
    public const float PreyAccessiblePopulationFloor = 0.04f;
    public const float FeedingMomentumRiseRate = 0.32f;
    public const float FeedingMomentumDecayRate = 0.14f;
    public const float HungerRiseRate = 0.50f;
    public const float HungerDecayRate = 0.28f;
    public const float SevereShortageMortalityWeight = 0.04f;
    public const float StarvationMortalityWeight = 0.12f;
    public const float NoFoodMortalityBonus = 0.08f;
    public const float ReproductionFedThreshold = 0.62f;
    public const float ReproductionHabitatThreshold = 0.40f;
    public const float MortalityHungerThreshold = 0.45f;
    public const float ReproductionWeight = 0.13f;
    public const float HungerMortalityWeight = 0.14f;
    public const float ShortageMortalityWeight = 0.03f;
    public const float MigrationHungerThreshold = 0.48f;
    public const int MigrationShortageMonthsThreshold = 4;
    public const float MigrationShareWeight = 0.08f;
    public const float MigrationShareCap = 0.18f;
    public const float MigrationScoreDeltaRequired = 0.08f;
    public const float MigrationPressureHungerWeight = 0.44f;
    public const float MigrationPressureShortageWeight = 0.08f;
    public const float MigrationPressureFoodStressBonus = 0.14f;
    public const int MinimumMigrationPopulation = 4;
    public const float PredatorRiskPenaltyWeight = 0.14f;
    public const int ExtinctionThresholdPopulation = 1;
}
