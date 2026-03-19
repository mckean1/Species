namespace Species.Domain.Simulation;

public sealed class DiscoveryChange
{
    public required string GroupId { get; init; }

    public required string GroupName { get; init; }

    public required string KnownDiscoveriesSummary { get; init; }

    public required string EvidenceSummary { get; init; }

    public required string CheckSummary { get; init; }

    public required string UnlockedDiscoveriesSummary { get; init; }

    public required string DecisionEffectSummary { get; init; }
}
