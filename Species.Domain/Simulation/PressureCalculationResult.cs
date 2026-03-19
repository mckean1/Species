using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class PressureCalculationResult
{
    public PressureCalculationResult(World world, IReadOnlyList<GroupPressureChange> changes)
    {
        World = world;
        Changes = changes;
    }

    public World World { get; }

    public IReadOnlyList<GroupPressureChange> Changes { get; }
}
