namespace Species.Domain.Simulation;

public sealed class ProtoPressureChange
{
    public string RegionId { get; init; } = string.Empty;

    public string RegionName { get; init; } = string.Empty;

    public float PreviousProtoFloraPressure { get; init; }

    public float NewProtoFloraPressure { get; init; }

    public float PreviousProtoFaunaPressure { get; init; }

    public float NewProtoFaunaPressure { get; init; }

    public float FloraOccupancyDeficit { get; init; }

    public float FaunaOccupancyDeficit { get; init; }

    public float FloraSupportDeficit { get; init; }

    public float FaunaSupportDeficit { get; init; }

    public float EcologicalVacancy { get; init; }

    public float RecentCollapseOpening { get; init; }

    public string Summary { get; init; } = string.Empty;
}
