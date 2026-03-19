using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class ChronicleEntry
{
    public required int EventYear { get; init; }

    public required int EventMonth { get; init; }

    public required int RecordSequence { get; init; }

    public required int RevealAfterYear { get; init; }

    public required int RevealAfterMonth { get; init; }

    public int? RevealedYear { get; init; }

    public int? RevealedMonth { get; init; }

    public int? RevealSequence { get; init; }

    public required string GroupId { get; init; }

    public required string GroupName { get; init; }

    public required ChronicleEventCategory Category { get; init; }

    public required string Message { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public bool IsRevealed => RevealSequence.HasValue;

    public ChronicleEntry Reveal(int revealedYear, int revealedMonth, int revealSequence)
    {
        return new ChronicleEntry
        {
            EventYear = EventYear,
            EventMonth = EventMonth,
            RecordSequence = RecordSequence,
            RevealAfterYear = RevealAfterYear,
            RevealAfterMonth = RevealAfterMonth,
            RevealedYear = revealedYear,
            RevealedMonth = revealedMonth,
            RevealSequence = revealSequence,
            GroupId = GroupId,
            GroupName = GroupName,
            Category = Category,
            Message = Message,
            Tags = Tags.ToArray()
        };
    }
}
