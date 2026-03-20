namespace Species.Domain.Constants;

public static class MigrationConstants
{
    public const int MigrationPressureTrigger = 62;
    public const int ExtremeMigrationPressureTrigger = 82;
    public const int SevereShortageTrigger = 10;
    public const int SevereStarvationTrigger = 5;
    public const float LowStoredFoodPerPopulationUnit = 0.15f;
    public const int RecentMoveCooldownMonths = 2;
    public const float MinimumMoveMargin = 10.0f;
    public const float RecentMoveMarginPenalty = 6.0f;
    public const float ReturnMoveMarginPenalty = 4.0f;
    public const float FloraSupportScale = 120.0f;
    public const float FaunaSupportScale = 120.0f;
    public const float ThreatSupportScale = 100.0f;
    public const float GathererFloraWeight = 0.45f;
    public const float GathererFaunaWeight = 0.15f;
    public const float GathererWaterWeight = 0.25f;
    public const float GathererThreatWeight = 0.15f;
    public const float HunterFloraWeight = 0.10f;
    public const float HunterFaunaWeight = 0.45f;
    public const float HunterWaterWeight = 0.15f;
    public const float HunterThreatWeight = 0.30f;
    public const float MixedFloraWeight = 0.30f;
    public const float MixedFaunaWeight = 0.25f;
    public const float MixedWaterWeight = 0.20f;
    public const float MixedThreatWeight = 0.25f;
    public const float KnownRegionBonus = 6.0f;
    public const float UnknownRegionPenalty = 4.0f;
    public const float ReturnToLastRegionPenalty = 12.0f;
}
