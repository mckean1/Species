namespace Species.Domain.Simulation;

public sealed class AdvancementChange
{
    public required string GroupId { get; init; }

    public required string GroupName { get; init; }

    public required string RelevantDiscoveriesSummary { get; init; }

    public required string LearnedAdvancementsSummary { get; init; }

    public required string EvidenceSummary { get; init; }

    public required string CheckSummary { get; init; }

    public required string UnlockedAdvancementsSummary { get; init; }

    public required string PracticalEffectSummary { get; init; }

    public required string ChronicleLinesSummary { get; init; }
}
