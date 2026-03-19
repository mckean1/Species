namespace Species.Domain.Models;

public sealed class Chronicle
{
    public Chronicle(
        IReadOnlyList<ChronicleEntry>? entries = null,
        int nextRecordSequence = 1,
        int nextRevealSequence = 1)
    {
        Entries = entries ?? Array.Empty<ChronicleEntry>();
        NextRecordSequence = nextRecordSequence;
        NextRevealSequence = nextRevealSequence;
    }

    public IReadOnlyList<ChronicleEntry> Entries { get; }

    public int NextRecordSequence { get; }

    public int NextRevealSequence { get; }

    public IReadOnlyList<ChronicleEntry> GetVisibleFeedEntries()
    {
        return Entries
            .Where(entry => entry.IsRevealed)
            .OrderByDescending(entry => entry.RevealSequence)
            .ToArray();
    }

    public IReadOnlyList<ChronicleEntry> GetPendingEntries()
    {
        return Entries
            .Where(entry => !entry.IsRevealed)
            .OrderBy(entry => entry.RecordSequence)
            .ToArray();
    }
}
