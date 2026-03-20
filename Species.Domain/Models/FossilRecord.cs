namespace Species.Domain.Models;

public sealed class FossilRecord
{
    public string Id { get; init; } = string.Empty;

    public string RegionId { get; init; } = string.Empty;

    public string SpeciesId { get; init; } = string.Empty;

    public string ParentSpeciesId { get; init; } = string.Empty;

    public string FormName { get; init; } = string.Empty;

    public string TraitSummary { get; init; } = string.Empty;

    public int RecordedYear { get; init; }

    public int RecordedMonth { get; init; }

    public string CauseSummary { get; init; } = string.Empty;

    public string Significance { get; init; } = string.Empty;
}
