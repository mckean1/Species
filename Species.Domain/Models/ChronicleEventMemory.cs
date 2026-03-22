using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class ChronicleEventMemory
{
    public required string DedupeKey { get; init; }

    public required string CooldownFamily { get; init; }

    public required string PolityId { get; init; }

    public required ChronicleOutputTarget OutputTarget { get; init; }

    public required int EventYear { get; init; }

    public required int EventMonth { get; init; }
}
