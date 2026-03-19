using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class AdvancementResult
{
    public AdvancementResult(World world, IReadOnlyList<AdvancementChange> changes)
    {
        World = world;
        Changes = changes;
    }

    public World World { get; }

    public IReadOnlyList<AdvancementChange> Changes { get; }
}
