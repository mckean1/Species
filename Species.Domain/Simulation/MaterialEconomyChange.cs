using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class MaterialEconomyChange
{
    public required string PolityId { get; init; }

    public required string PolityName { get; init; }

    public string SettlementId { get; init; } = string.Empty;

    public string SettlementName { get; init; } = string.Empty;

    public string RegionId { get; init; } = string.Empty;

    public string RegionName { get; init; } = string.Empty;

    public required MaterialEconomyChangeKind Kind { get; init; }

    public required string Message { get; init; }

    public MaterialStockpile Extracted { get; init; } = new();

    public MaterialProductionState Production { get; init; } = new();
}

public enum MaterialEconomyChangeKind
{
    ExtractionSecured,
    StorageImproved,
    Stabilized,
    Shortage,
    Contraction
}
