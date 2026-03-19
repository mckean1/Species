namespace Species.Domain.Simulation;

public sealed class FloraPopulationChange
{
    public required string RegionId { get; init; }

    public required string RegionName { get; init; }

    public required string FloraSpeciesId { get; init; }

    public required string FloraSpeciesName { get; init; }

    public required int PreviousPopulation { get; init; }

    public required int TargetPopulation { get; init; }

    public required int NewPopulation { get; init; }

    public required string Outcome { get; init; }

    public required bool WaterSupported { get; init; }

    public required bool CoreBiomeFit { get; init; }

    public required float FertilityFit { get; init; }

    public required string PrimaryCause { get; init; }
}
