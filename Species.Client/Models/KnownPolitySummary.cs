namespace Species.Client.Models;

public sealed class KnownPolitySummary
{
    public KnownPolitySummary(
        string id,
        string name,
        string governmentForm,
        string coreRegion,
        string currentRegion,
        string population,
        string relationship,
        string proximity,
        string pressureSummary,
        IReadOnlyList<string> traits,
        IReadOnlyList<string> risks,
        IReadOnlyList<string> notes,
        IReadOnlyList<string> knownLaws)
    {
        Id = id;
        Name = name;
        GovernmentForm = governmentForm;
        CoreRegion = coreRegion;
        CurrentRegion = currentRegion;
        Population = population;
        Relationship = relationship;
        Proximity = proximity;
        PressureSummary = pressureSummary;
        Traits = traits;
        Risks = risks;
        Notes = notes;
        KnownLaws = knownLaws;
    }

    public string Id { get; }

    public string Name { get; }

    public string GovernmentForm { get; }

    public string CoreRegion { get; }

    public string CurrentRegion { get; }

    public string Population { get; }

    public string Relationship { get; }

    public string Proximity { get; }

    public string PressureSummary { get; }

    public IReadOnlyList<string> Traits { get; }

    public IReadOnlyList<string> Risks { get; }

    public IReadOnlyList<string> Notes { get; }

    public IReadOnlyList<string> KnownLaws { get; }
}
