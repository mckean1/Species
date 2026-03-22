using Species.Domain.Enums;

namespace Species.Domain.Models;

// Structured Chronicle candidate emitted by simulation systems or compatibility adapters.
// Final player-facing formatting and routing are handled centrally by ChroniclePolicy.
public sealed class ChronicleEventCandidate
{
    public required int EventYear { get; init; }

    public required int EventMonth { get; init; }

    public required string PolityId { get; init; }

    public required string PolityName { get; init; }

    public required string EventType { get; init; }

    public required ChronicleCandidateCategory Category { get; init; }

    public required ChronicleEventSeverity Severity { get; init; }

    public required ChronicleTriggerKind TriggerKind { get; init; }

    public string? RegionId { get; init; }

    public string? RegionName { get; init; }

    public string? SettlementName { get; init; }

    public string? DiscoveryName { get; init; }

    public string? AdvancementName { get; init; }

    public string? LawName { get; init; }

    public string? OtherPartyName { get; init; }

    public string? GovernmentFormName { get; init; }

    public bool IsScoutSourced { get; init; }

    public required string DedupeKey { get; init; }

    public required string CooldownFamily { get; init; }

    public ChronicleOutputTarget PreferredOutputTarget { get; init; } = ChronicleOutputTarget.Chronicle;

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
