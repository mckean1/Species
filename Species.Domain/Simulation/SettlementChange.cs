using Species.Domain.Enums;

namespace Species.Domain.Simulation;

public sealed class SettlementChange
{
    public string PolityId { get; init; } = string.Empty;

    public string PolityName { get; init; } = string.Empty;

    public string RegionId { get; init; } = string.Empty;

    public string RegionName { get; init; } = string.Empty;

    public string SettlementId { get; init; } = string.Empty;

    public string SettlementName { get; init; } = string.Empty;

    public SettlementChangeKind Kind { get; init; }

    public string Message { get; init; } = string.Empty;
}

public enum SettlementChangeKind
{
    Founded,
    Abandoned,
    Anchored,
    SeasonalPattern,
    PresenceLost
}
