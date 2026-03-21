using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class FaunaDietLink
{
    public FaunaDietTargetKind TargetKind { get; init; }

    public string TargetSpeciesId { get; init; } = string.Empty;

    public float Weight { get; init; }

    public bool IsFallback { get; init; }
}
