using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class DiscoveryResult
{
    public DiscoveryResult(World world, IReadOnlyList<DiscoveryChange> changes)
    {
        World = world;
        Changes = changes;
    }

    public World World { get; }

    public IReadOnlyList<DiscoveryChange> Changes { get; }
}
