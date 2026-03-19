using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class FloraSimulationResult
{
    public FloraSimulationResult(World world, IReadOnlyList<FloraPopulationChange> changes)
    {
        World = world;
        Changes = changes;
    }

    public World World { get; }

    public IReadOnlyList<FloraPopulationChange> Changes { get; }
}
