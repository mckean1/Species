namespace Species.Client.Models;

public sealed class KnownSpeciesSummary
{
    public KnownSpeciesSummary(
        string id,
        string name,
        string kind,
        bool isPlayerSpecies,
        string presence,
        string status,
        string overview,
        IReadOnlyList<string> facts,
        IReadOnlyList<string> traits,
        IReadOnlyList<string> relevance)
    {
        Id = id;
        Name = name;
        Kind = kind;
        IsPlayerSpecies = isPlayerSpecies;
        Presence = presence;
        Status = status;
        Overview = overview;
        Facts = facts;
        Traits = traits;
        Relevance = relevance;
    }

    public string Id { get; }

    public string Name { get; }

    public string Kind { get; }

    public bool IsPlayerSpecies { get; }

    public string Presence { get; }

    public string Status { get; }

    public string Overview { get; }

    public IReadOnlyList<string> Facts { get; }

    public IReadOnlyList<string> Traits { get; }

    public IReadOnlyList<string> Relevance { get; }
}
