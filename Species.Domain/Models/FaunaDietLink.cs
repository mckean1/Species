using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class FaunaDietLink
{
    // Diet links are explicit food-web relationships.
    // Non-fallback links are the normal intended diet; fallback links only help cover
    // remaining unmet intake when preferred foods cannot satisfy demand.
    public FaunaDietTargetKind TargetKind { get; init; }

    public string TargetSpeciesId { get; init; } = string.Empty;

    public float Weight { get; init; }

    public bool IsFallback { get; init; }
}
