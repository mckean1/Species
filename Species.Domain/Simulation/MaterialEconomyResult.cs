using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class MaterialEconomyResult
{
    public MaterialEconomyResult(World world, IReadOnlyList<MaterialEconomyChange> changes)
    {
        World = world;
        Changes = changes;
    }

    public World World { get; }

    public IReadOnlyList<MaterialEconomyChange> Changes { get; }
}
