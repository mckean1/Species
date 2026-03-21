using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class ProtoPressureResult
{
    public ProtoPressureResult(World world, IReadOnlyList<ProtoPressureChange> changes)
    {
        World = world;
        Changes = changes;
    }

    public World World { get; }

    public IReadOnlyList<ProtoPressureChange> Changes { get; }
}
