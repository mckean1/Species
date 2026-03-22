using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class ChronicleAlert
{
    public required int EventYear { get; init; }

    public required int EventMonth { get; init; }

    public required int Sequence { get; init; }

    public required string PolityId { get; init; }

    public required string PolityName { get; init; }

    public required ChronicleCandidateCategory Category { get; init; }

    public required ChronicleEventSeverity Severity { get; init; }

    public required ChronicleTriggerKind TriggerKind { get; init; }

    public required string Message { get; init; }

    public required string DedupeKey { get; init; }

    public required string CooldownFamily { get; init; }
}
