namespace Species.Domain.Models;

public sealed class Chronicle
{
    public Chronicle(
        IReadOnlyList<ChronicleEntry>? entries = null,
        IReadOnlyList<ChronicleAlert>? alerts = null,
        IReadOnlyList<ChronicleEventMemory>? eventMemory = null,
        int nextRecordSequence = 1,
        int nextRevealSequence = 1,
        int nextAlertSequence = 1)
    {
        Entries = entries ?? Array.Empty<ChronicleEntry>();
        Alerts = alerts ?? Array.Empty<ChronicleAlert>();
        EventMemory = eventMemory ?? Array.Empty<ChronicleEventMemory>();
        NextRecordSequence = nextRecordSequence;
        NextRevealSequence = nextRevealSequence;
        NextAlertSequence = nextAlertSequence;
    }

    public IReadOnlyList<ChronicleEntry> Entries { get; }

    public IReadOnlyList<ChronicleAlert> Alerts { get; }

    public IReadOnlyList<ChronicleEventMemory> EventMemory { get; }

    public int NextRecordSequence { get; }

    public int NextRevealSequence { get; }

    public int NextAlertSequence { get; }

    public IReadOnlyList<ChronicleEntry> GetVisibleFeedEntries()
    {
        return Entries
            .Where(entry => entry.IsRevealed)
            .OrderByDescending(entry => entry.RevealSequence)
            .ToArray();
    }

    public IReadOnlyList<ChronicleAlert> GetCurrentAlerts()
    {
        return Alerts
            .OrderByDescending(alert => alert.Sequence)
            .ToArray();
    }

    public IReadOnlyList<ChronicleAlert> GetCurrentAlerts(int currentYear, int currentMonth, int recentMonths)
    {
        return Alerts
            .Where(alert => (((currentYear - alert.EventYear) * 12) + (currentMonth - alert.EventMonth)) <= recentMonths)
            .OrderByDescending(alert => alert.Sequence)
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
