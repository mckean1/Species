using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed record PoliticalScaleResult(
    World World,
    IReadOnlyList<PoliticalScaleChange> Changes);
