using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class SimulationTickResult
{
    public SimulationTickResult(World world, IReadOnlyList<FloraPopulationChange> floraChanges)
    {
        World = world;
        FloraChanges = floraChanges;
    }

    public World World { get; }

    public IReadOnlyList<FloraPopulationChange> FloraChanges { get; }
}
