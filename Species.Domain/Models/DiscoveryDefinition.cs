using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class DiscoveryDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public DiscoveryCategory Category { get; init; }

    public string DecisionEffectSummary { get; init; } = string.Empty;
}
