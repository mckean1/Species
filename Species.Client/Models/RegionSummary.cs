namespace Species.Client.Models;

public sealed class RegionSummary
{
    public RegionSummary(
        string id,
        string name,
        string familiarity,
        string biome,
        string waterAvailability,
        string fertility,
        int presencePopulation,
        string presenceText,
        IReadOnlyList<string> groupPresence,
        IReadOnlyList<string> flora,
        string faunaLabel,
        IReadOnlyList<string> fauna,
        IReadOnlyList<string> materials,
        IReadOnlyList<string> discoveries,
        IReadOnlyList<string> context,
        IReadOnlyList<string> opportunities,
        IReadOnlyList<string> risks,
        IReadOnlyList<string> biology,
        IReadOnlyList<string> fossils,
        string threatText,
        int threatScore)
    {
        Id = id;
        Name = name;
        Familiarity = familiarity;
        Biome = biome;
        WaterAvailability = waterAvailability;
        Fertility = fertility;
        PresencePopulation = presencePopulation;
        PresenceText = presenceText;
        GroupPresence = groupPresence;
        Flora = flora;
        FaunaLabel = faunaLabel;
        Fauna = fauna;
        Materials = materials;
        Discoveries = discoveries;
        Context = context;
        Opportunities = opportunities;
        Risks = risks;
        Biology = biology;
        Fossils = fossils;
        ThreatText = threatText;
        ThreatScore = threatScore;
    }

    public string Id { get; }

    public string Name { get; }

    public string Familiarity { get; }

    public string Biome { get; }

    public string WaterAvailability { get; }

    public string Fertility { get; }

    public int PresencePopulation { get; }

    public string PresenceText { get; }

    public IReadOnlyList<string> GroupPresence { get; }

    public IReadOnlyList<string> Flora { get; }

    public string FaunaLabel { get; }

    public IReadOnlyList<string> Fauna { get; }

    public IReadOnlyList<string> Materials { get; }

    public IReadOnlyList<string> Discoveries { get; }

    public IReadOnlyList<string> Context { get; }

    public IReadOnlyList<string> Opportunities { get; }

    public IReadOnlyList<string> Risks { get; }

    public IReadOnlyList<string> Biology { get; }

    public IReadOnlyList<string> Fossils { get; }

    public string ThreatText { get; }

    public int ThreatScore { get; }
}
