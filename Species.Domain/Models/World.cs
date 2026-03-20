namespace Species.Domain.Models;

public sealed class World
{
    // World now carries both constituent groups and explicit polity actors.
    public int Seed { get; }
    public int CurrentYear { get; }
    public int CurrentMonth { get; }
    public IReadOnlyList<Region> Regions { get; }
    public IReadOnlyList<PopulationGroup> PopulationGroups { get; }
    public IReadOnlyList<Polity> Polities { get; }
    public Chronicle Chronicle { get; }
    public string FocalPolityId { get; }

    public World(
        int seed,
        int currentYear,
        int currentMonth,
        IReadOnlyList<Region> regions,
        IReadOnlyList<PopulationGroup>? populationGroups = null,
        Chronicle? chronicle = null,
        IReadOnlyList<Polity>? polities = null,
        string focalPolityId = "")
    {
        Seed = seed;
        CurrentYear = currentYear;
        CurrentMonth = currentMonth;
        Regions = regions;
        PopulationGroups = populationGroups ?? Array.Empty<PopulationGroup>();
        Chronicle = chronicle ?? new Chronicle();
        Polities = polities ?? Array.Empty<Polity>();
        FocalPolityId = focalPolityId;
    }
}
