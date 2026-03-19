using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class FaunaSimulationResult
{
    public FaunaSimulationResult(World world, IReadOnlyList<FaunaPopulationChange> changes)
    {
        World = world;
        Changes = changes;
    }

    public World World { get; }

    public IReadOnlyList<FaunaPopulationChange> Changes { get; }
}
