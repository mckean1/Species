using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed record InterPolityResult(
    World World,
    IReadOnlyList<InterPolityChange> Changes);
