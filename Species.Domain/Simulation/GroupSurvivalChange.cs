namespace Species.Domain.Simulation;

public sealed class GroupSurvivalChange
{
    public required string GroupId { get; init; }

    public required string GroupName { get; init; }

    public required string CurrentRegionId { get; init; }

    public required string CurrentRegionName { get; init; }

    public required string SubsistenceMode { get; init; }

    public required int StartingPopulation { get; init; }

    public required int MonthlyFoodNeed { get; init; }

    public required string PrimaryAction { get; init; }

    public required int PrimaryFoodGained { get; init; }

    public required string PrimarySummary { get; init; }

    public required string FallbackAction { get; init; }

    public required int FallbackFoodGained { get; init; }

    public required string FallbackSummary { get; init; }

    public required int TotalFoodAcquired { get; init; }

    public required int StoredFoodBefore { get; init; }

    public required int StoredFoodAfter { get; init; }

    public required int SettlementFoodUsed { get; init; }

    public required int Shortage { get; init; }

    public required int StarvationLoss { get; init; }

    public required int FinalPopulation { get; init; }

    public required string Outcome { get; init; }

    public required string SurvivalReason { get; init; }
}
