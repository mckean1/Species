using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class ChronicleTextToken
{
    public required ChronicleTextTokenType Type { get; init; }

    public required string Text { get; init; }
}
