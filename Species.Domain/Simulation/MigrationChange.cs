namespace Species.Domain.Simulation;

public sealed class MigrationChange
{
    public required string GroupId { get; init; }

    public required string GroupName { get; init; }

    public required string CurrentRegionId { get; init; }

    public required string CurrentRegionName { get; init; }

    public required int MigrationPressure { get; init; }

    public required int MigrationEffectivePressure { get; init; }

    public required string MigrationSeverityLabel { get; init; }

    public required int StoredFood { get; init; }

    public required bool ConsideredMigration { get; init; }

    public required float RequiredMoveMargin { get; init; }

    public required float CurrentRegionScore { get; init; }

    public required string NeighborScoresSummary { get; init; }

    public required string WinningRegionId { get; init; }

    public required string WinningRegionName { get; init; }

    public required float WinningRegionScore { get; init; }

    public required bool Moved { get; init; }

    public required string NewRegionId { get; init; }

    public required string NewRegionName { get; init; }

    public required string LastRegionId { get; init; }

    public required int MonthsSinceLastMove { get; init; }

    public required string DecisionReason { get; init; }
}
