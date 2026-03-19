namespace Species.Domain.Models;

public sealed class World
{
    public int Seed { get; }
    public int CurrentYear { get; }
    public int CurrentMonth { get; }
    public IReadOnlyList<Region> Regions { get; }

    public World(int seed, int currentYear, int currentMonth, IReadOnlyList<Region> regions)
    {
        Seed = seed;
        CurrentYear = currentYear;
        CurrentMonth = currentMonth;
        Regions = regions;
    }
}
