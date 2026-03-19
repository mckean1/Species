namespace Species.Domain.Models;

public sealed class World
{
    public int Seed { get; }
    public IReadOnlyList<Region> Regions { get; }

    public World(int seed, IReadOnlyList<Region> regions)
    {
        Seed = seed;
        Regions = regions;
    }
}
