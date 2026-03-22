namespace Species.Domain.Discovery;

public sealed record RegionDiscoverySnapshot(
    string RegionId,
    string RegionName,
    bool IsKnownRegion,
    bool IsCurrentRegion,
    DiscoveryStage OverallStage,
    DiscoveryStage FloraStage,
    DiscoveryStage FaunaStage,
    DiscoveryStage WaterStage,
    DiscoveryStage RegionStage,
    DiscoveryStage RouteStage,
    float GatheringPotentialFood,
    float HuntingPotentialFood,
    float WaterSupport,
    float ThreatPressure,
    int MonthsSpent,
    int GatheringEvidenceMonths,
    int HuntingEvidenceMonths,
    int WaterEvidenceMonths);
