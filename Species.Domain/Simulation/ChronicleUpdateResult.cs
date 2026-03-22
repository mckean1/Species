using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class ChronicleUpdateResult
{
    public ChronicleUpdateResult(
        World world,
        IReadOnlyList<ChronicleEntry> recordedEntries,
        IReadOnlyList<ChronicleEntry> revealedEntries,
        IReadOnlyList<ChronicleAlert>? recordedAlerts = null)
    {
        World = world;
        RecordedEntries = recordedEntries;
        RevealedEntries = revealedEntries;
        RecordedAlerts = recordedAlerts ?? Array.Empty<ChronicleAlert>();
    }

    public World World { get; }

    public IReadOnlyList<ChronicleEntry> RecordedEntries { get; }

    public IReadOnlyList<ChronicleEntry> RevealedEntries { get; }

    public IReadOnlyList<ChronicleAlert> RecordedAlerts { get; }
}
