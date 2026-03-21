namespace Species.Domain.Simulation;

public sealed class GroupSurvivalChange
{
    public required string GroupId { get; init; }

    public required string GroupName { get; init; }

    public required string CurrentRegionId { get; init; }

    public required string CurrentRegionName { get; init; }

    public required string SubsistenceMode { get; init; }

    public required string SubsistencePreference { get; init; }

    public required string ExtractionPlan { get; init; }

    public required int StartingPopulation { get; init; }

    public required int MonthlyFoodNeed { get; init; }

    public required long KnownGatheringSupport { get; init; }

    public required long KnownHuntingSupport { get; init; }

    public required string PrimaryAction { get; init; }

    public required int PrimaryFoodGained { get; init; }

    public required string PrimarySummary { get; init; }

    public required IReadOnlyDictionary<string, int> PrimaryConsumedSourceUnits { get; init; }

    public required string FallbackAction { get; init; }

    public required int FallbackFoodGained { get; init; }

    public required string FallbackSummary { get; init; }

    public required IReadOnlyDictionary<string, int> FallbackConsumedSourceUnits { get; init; }

    public required int TotalFoodAcquired { get; init; }

    public required int StartingReserveFood { get; init; }

    public required int StartingTotalFoodStores { get; init; }

    public required int StoredFoodBefore { get; init; }

    public required int StoredFoodAfter { get; init; }

    public required int SettlementFoodUsed { get; init; }

    public required int SettlementFoodConsumedRaw { get; init; }

    public required int EndingReserveFood { get; init; }

    public required int EndingTotalFoodStores { get; init; }

    public required int FoodConsumption { get; init; }

    public required int FoodLosses { get; init; }

    public required int NetFoodChange { get; init; }

    public required int UsableFoodConsumed { get; init; }

    public required int FoodPressureEffective { get; init; }

    public required int WaterPressureEffective { get; init; }

    public required string HardshipSeverityLabel { get; init; }

    public required float HungerPressure { get; init; }

    public required int ShortageMonths { get; init; }

    public required string FoodStressState { get; init; }

    public required int Shortage { get; init; }

    public required int Births { get; init; }

    public required int NaturalDeaths { get; init; }

    public required int HardshipDeaths { get; init; }

    public required int WaterStressDeaths { get; init; }

    public required int StarvationLoss { get; init; }

    public required int TotalDeaths { get; init; }

    public required int NetPopulationChange { get; init; }

    public required int FinalPopulation { get; init; }

    public required string Outcome { get; init; }

    public required string PopulationChangeSummary { get; init; }

    public required string SurvivalReason { get; init; }
}
