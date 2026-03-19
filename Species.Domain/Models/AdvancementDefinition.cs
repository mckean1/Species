using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class AdvancementDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public AdvancementCategory Category { get; init; }

    public string PracticalEffectSummary { get; init; } = string.Empty;
}
