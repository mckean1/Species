using Species.Domain.Enums;

namespace Species.Client.Models;

public sealed class KnownSpeciesSummary
{
    public KnownSpeciesSummary(
        string id,
        string name,
        SpeciesClass speciesClass,
        bool isPlayerSpecies,
        string stateLabel,
        IReadOnlyList<string> cells,
        string overview,
        IReadOnlyList<string> details,
        IReadOnlyList<string> traits,
        IReadOnlyList<string> context)
    {
        Id = id;
        Name = name;
        SpeciesClass = speciesClass;
        IsPlayerSpecies = isPlayerSpecies;
        StateLabel = stateLabel;
        Cells = cells;
        Overview = overview;
        Details = details;
        Traits = traits;
        Context = context;
    }

    public string Id { get; }

    public string Name { get; }

    public SpeciesClass SpeciesClass { get; }

    public bool IsPlayerSpecies { get; }

    public string StateLabel { get; }

    public IReadOnlyList<string> Cells { get; }

    public string Overview { get; }

    public IReadOnlyList<string> Details { get; }

    public IReadOnlyList<string> Traits { get; }

    public IReadOnlyList<string> Context { get; }
}
