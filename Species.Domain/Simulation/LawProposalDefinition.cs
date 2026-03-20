using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class LawProposalDefinition
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required string IntentSummary { get; init; }

    public required string TradeoffSummary { get; init; }

    public required LawProposalCategory Category { get; init; }

    public required LawConflictGroup ConflictGroup { get; init; }

    public string ConflictSlot { get; init; } = string.Empty;

    public required int ImpactScale { get; init; }

    public IReadOnlySet<GovernmentForm> GovernmentForms { get; init; } = new HashSet<GovernmentForm>();

    public IReadOnlyList<string> ConflictingDefinitionIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RequiredActiveDefinitionIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RepealsDefinitionIds { get; init; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, int> RelatedLawScoreModifiers { get; init; } = new Dictionary<string, int>(StringComparer.Ordinal);

    public required Func<PopulationGroup, Region, PolityContext, int> Score { get; init; }
}
