namespace Species.Domain.Models;

public sealed class World
{
    public int Seed { get; }
    public int CurrentYear { get; }
    public int CurrentMonth { get; }
    public IReadOnlyList<Region> Regions { get; }
    public IReadOnlyList<PopulationGroup> PopulationGroups { get; }
    public Chronicle Chronicle { get; }

    public World(
        int seed,
        int currentYear,
        int currentMonth,
        IReadOnlyList<Region> regions,
        IReadOnlyList<PopulationGroup>? populationGroups = null,
        Chronicle? chronicle = null)
    {
        Seed = seed;
        CurrentYear = currentYear;
        CurrentMonth = currentMonth;
        Regions = regions;
        PopulationGroups = populationGroups ?? Array.Empty<PopulationGroup>();
        Chronicle = chronicle ?? new Chronicle();
    }
}
