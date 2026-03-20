namespace Species.Domain.Simulation;

public sealed class BiologicalHistoryChange
{
    public required string RegionId { get; init; }

    public required string RegionName { get; init; }

    public required string SpeciesId { get; init; }

    public required string SpeciesName { get; init; }

    public required string Kind { get; init; }

    public required string Message { get; init; }
}
