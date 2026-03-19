namespace Species.Domain.Models;

public sealed class DiscoveryEvidenceState
{
    public Dictionary<string, int> SuccessfulGatheringMonthsByRegionId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> SuccessfulHuntingMonthsByRegionId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> MonthsSpentByRegionId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> WaterExposureMonthsByRegionId { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> RouteTraversalCountsByRouteId { get; init; } = new(StringComparer.Ordinal);

    public DiscoveryEvidenceState Clone()
    {
        return new DiscoveryEvidenceState
        {
            SuccessfulGatheringMonthsByRegionId = new Dictionary<string, int>(SuccessfulGatheringMonthsByRegionId, StringComparer.Ordinal),
            SuccessfulHuntingMonthsByRegionId = new Dictionary<string, int>(SuccessfulHuntingMonthsByRegionId, StringComparer.Ordinal),
            MonthsSpentByRegionId = new Dictionary<string, int>(MonthsSpentByRegionId, StringComparer.Ordinal),
            WaterExposureMonthsByRegionId = new Dictionary<string, int>(WaterExposureMonthsByRegionId, StringComparer.Ordinal),
            RouteTraversalCountsByRouteId = new Dictionary<string, int>(RouteTraversalCountsByRouteId, StringComparer.Ordinal)
        };
    }
}
