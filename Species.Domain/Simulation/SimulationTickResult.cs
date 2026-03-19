using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class SimulationTickResult
{
    public SimulationTickResult(
        World world,
        IReadOnlyList<FloraPopulationChange> floraChanges,
        IReadOnlyList<FaunaPopulationChange> faunaChanges,
        IReadOnlyList<GroupPressureChange> groupPressureChanges)
    {
        World = world;
        FloraChanges = floraChanges;
        FaunaChanges = faunaChanges;
        GroupPressureChanges = groupPressureChanges;
    }

    public World World { get; }

    public IReadOnlyList<FloraPopulationChange> FloraChanges { get; }

    public IReadOnlyList<FaunaPopulationChange> FaunaChanges { get; }

    public IReadOnlyList<GroupPressureChange> GroupPressureChanges { get; }
}
