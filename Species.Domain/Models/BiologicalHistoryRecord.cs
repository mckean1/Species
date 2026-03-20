namespace Species.Domain.Models;

public sealed class BiologicalHistoryRecord
{
    public string Id { get; init; } = string.Empty;

    public string RegionId { get; init; } = string.Empty;

    public string SpeciesId { get; init; } = string.Empty;

    public string EventKind { get; init; } = string.Empty;

    public int Year { get; init; }

    public int Month { get; init; }

    public string Message { get; init; } = string.Empty;
}
