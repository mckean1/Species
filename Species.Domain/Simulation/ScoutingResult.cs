using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class ScoutingResult
{
    public ScoutingResult(World world, IReadOnlyList<DiscoveryChange> changes)
    {
        World = world;
        Changes = changes;
    }

    public World World { get; }

    public IReadOnlyList<DiscoveryChange> Changes { get; }
}
