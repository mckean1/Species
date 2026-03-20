using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed record BiologicalEvolutionResult(
    World World,
    IReadOnlyList<BiologicalHistoryChange> Changes);
