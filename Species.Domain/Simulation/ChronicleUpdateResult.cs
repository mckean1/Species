using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class ChronicleUpdateResult
{
    public ChronicleUpdateResult(
        World world,
        IReadOnlyList<ChronicleEntry> recordedEntries,
        IReadOnlyList<ChronicleEntry> revealedEntries)
    {
        World = world;
        RecordedEntries = recordedEntries;
        RevealedEntries = revealedEntries;
    }

    public World World { get; }

    public IReadOnlyList<ChronicleEntry> RecordedEntries { get; }

    public IReadOnlyList<ChronicleEntry> RevealedEntries { get; }
}
