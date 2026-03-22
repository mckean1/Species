using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class AdvancementDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public AdvancementCategory Category { get; init; }

    public string PracticalEffectSummary { get; init; } = string.Empty;

    public string PrerequisiteSummary { get; init; } = string.Empty;

    public IReadOnlyList<string> RequiredDiscoveryIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RequiredAdvancementIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<FloraTag> RequiredFloraTags { get; init; } = Array.Empty<FloraTag>();

    public IReadOnlyList<FaunaTag> RequiredFaunaTags { get; init; } = Array.Empty<FaunaTag>();

    public IReadOnlyList<ResourceTag> RequiredResourceTags { get; init; } = Array.Empty<ResourceTag>();

    public bool RequiresCurrentAccess { get; init; }

    public bool RequiresPressureOrIncentive { get; init; }

    public bool RequiresContinuity { get; init; }

    public int OpportunityMonthsRequired { get; init; }
}
