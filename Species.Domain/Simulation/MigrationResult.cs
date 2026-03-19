using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class MigrationResult
{
    public MigrationResult(World world, IReadOnlyList<MigrationChange> changes)
    {
        World = world;
        Changes = changes;
    }

    public World World { get; }

    public IReadOnlyList<MigrationChange> Changes { get; }
}
