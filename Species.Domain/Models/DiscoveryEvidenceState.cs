namespace Species.Domain.Models;

public sealed class DiscoveryEvidenceState
{
    public Dictionary<string, int> SuccessfulGatheringMonthsByRegionId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> SuccessfulHuntingMonthsByRegionId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> MonthsSpentByRegionId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> WaterExposureMonthsByRegionId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> RouteTraversalCountsByRouteId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> MaterialExposureMonthsByResourceId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> MaterialUseMonthsByResourceId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> ContactMonthsByPolityId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> SharedExposureMonthsByDiscoveryId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, float> DiscoveryProgressByDiscoveryId { get; init; } = new(StringComparer.Ordinal);

    public int RecurringFoodPressureMonths { get; set; }

    public int RecurringThreatPressureMonths { get; set; }

    public int RecurringMaterialShortageMonths { get; set; }

    public int SettlementContinuityMonths { get; set; }

    public int SeasonalObservationMonths { get; set; }

    public DiscoveryEvidenceState Clone()
    {
        return new DiscoveryEvidenceState
        {
            SuccessfulGatheringMonthsByRegionId = new Dictionary<string, int>(SuccessfulGatheringMonthsByRegionId, StringComparer.Ordinal),
            SuccessfulHuntingMonthsByRegionId = new Dictionary<string, int>(SuccessfulHuntingMonthsByRegionId, StringComparer.Ordinal),
            MonthsSpentByRegionId = new Dictionary<string, int>(MonthsSpentByRegionId, StringComparer.Ordinal),
            WaterExposureMonthsByRegionId = new Dictionary<string, int>(WaterExposureMonthsByRegionId, StringComparer.Ordinal),
            RouteTraversalCountsByRouteId = new Dictionary<string, int>(RouteTraversalCountsByRouteId, StringComparer.Ordinal),
            MaterialExposureMonthsByResourceId = new Dictionary<string, int>(MaterialExposureMonthsByResourceId, StringComparer.Ordinal),
            MaterialUseMonthsByResourceId = new Dictionary<string, int>(MaterialUseMonthsByResourceId, StringComparer.Ordinal),
            ContactMonthsByPolityId = new Dictionary<string, int>(ContactMonthsByPolityId, StringComparer.Ordinal),
            SharedExposureMonthsByDiscoveryId = new Dictionary<string, int>(SharedExposureMonthsByDiscoveryId, StringComparer.Ordinal),
            DiscoveryProgressByDiscoveryId = new Dictionary<string, float>(DiscoveryProgressByDiscoveryId, StringComparer.Ordinal),
            RecurringFoodPressureMonths = RecurringFoodPressureMonths,
            RecurringThreatPressureMonths = RecurringThreatPressureMonths,
            RecurringMaterialShortageMonths = RecurringMaterialShortageMonths,
            SettlementContinuityMonths = SettlementContinuityMonths,
            SeasonalObservationMonths = SeasonalObservationMonths
        };
    }
}
