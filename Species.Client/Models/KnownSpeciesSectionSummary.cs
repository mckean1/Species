namespace Species.Client.Models;

public sealed class KnownSpeciesSectionSummary
{
    public KnownSpeciesSectionSummary(
        string title,
        string emptyState,
        IReadOnlyList<string> columns,
        IReadOnlyList<KnownSpeciesSummary> species)
    {
        Title = title;
        EmptyState = emptyState;
        Columns = columns;
        Species = species;
    }

    public string Title { get; }

    public string EmptyState { get; }

    public IReadOnlyList<string> Columns { get; }

    public IReadOnlyList<KnownSpeciesSummary> Species { get; }
}
