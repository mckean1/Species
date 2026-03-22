using Species.Client.Enums;

namespace Species.Client.Models;

public sealed class AdvancementScreenItem
{
    public AdvancementScreenItem(
        string id,
        string name,
        string category,
        AdvancementStatus status,
        string description,
        string capabilitySummary,
        string listHint,
        IReadOnlyList<string> requirements,
        IReadOnlyList<string> notes,
        string progressSummary,
        string statusSummary)
    {
        Id = id;
        Name = name;
        Category = category;
        Status = status;
        Description = description;
        CapabilitySummary = capabilitySummary;
        ListHint = listHint;
        Requirements = requirements;
        Notes = notes;
        ProgressSummary = progressSummary;
        StatusSummary = statusSummary;
    }

    public string Id { get; }

    public string Name { get; }

    public string Category { get; }

    public AdvancementStatus Status { get; }

    public string Description { get; }

    public string CapabilitySummary { get; }

    public string ListHint { get; }

    public IReadOnlyList<string> Requirements { get; }

    public IReadOnlyList<string> Notes { get; }

    public string ProgressSummary { get; }

    public string StatusSummary { get; }
}
