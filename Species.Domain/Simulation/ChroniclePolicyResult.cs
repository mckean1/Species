using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class ChroniclePolicyResult
{
    public ChroniclePolicyResult(
        IReadOnlyList<ChronicleEntry> chronicleEntries,
        IReadOnlyList<ChronicleAlert> alerts,
        IReadOnlyList<ChronicleEventCandidate> diagnostics,
        IReadOnlyList<ChronicleEventCandidate> suppressed)
    {
        ChronicleEntries = chronicleEntries;
        Alerts = alerts;
        Diagnostics = diagnostics;
        Suppressed = suppressed;
    }

    public IReadOnlyList<ChronicleEntry> ChronicleEntries { get; }

    public IReadOnlyList<ChronicleAlert> Alerts { get; }

    public IReadOnlyList<ChronicleEventCandidate> Diagnostics { get; }

    public IReadOnlyList<ChronicleEventCandidate> Suppressed { get; }
}
