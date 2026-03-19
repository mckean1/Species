namespace Species.Domain.Simulation;

public sealed class FaunaPopulationChange
{
    public required string RegionId { get; init; }

    public required string RegionName { get; init; }

    public required string FaunaSpeciesId { get; init; }

    public required string FaunaSpeciesName { get; init; }

    public required int PreviousPopulation { get; init; }

    public required int NewPopulation { get; init; }

    public required float FoodNeeded { get; init; }

    public required float FoodConsumed { get; init; }

    public required float FulfillmentRatio { get; init; }

    public required float HabitatSupport { get; init; }

    public required string Outcome { get; init; }

    public required string ConsumedFloraSummary { get; init; }

    public required string ConsumedFaunaSummary { get; init; }

    public required string PrimaryCause { get; init; }
}
