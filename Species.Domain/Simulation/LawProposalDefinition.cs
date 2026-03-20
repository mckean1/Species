using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class LawProposalDefinition
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required LawProposalCategory Category { get; init; }

    public required LawConflictGroup ConflictGroup { get; init; }

    public string ConflictSlot { get; init; } = string.Empty;

    public required int ImpactScale { get; init; }

    public required GovernmentForm GovernmentForm { get; init; }

    public IReadOnlyList<string> ConflictingDefinitionIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RequiredActiveDefinitionIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RepealsDefinitionIds { get; init; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, int> RelatedLawScoreModifiers { get; init; } = new Dictionary<string, int>(StringComparer.Ordinal);

    public required Func<PopulationGroup, Region, int> Score { get; init; }
}
