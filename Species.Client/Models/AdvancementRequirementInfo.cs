namespace Species.Client.Models;

public sealed class AdvancementRequirementInfo
{
    public AdvancementRequirementInfo(
        bool isSatisfied,
        string lockReason,
        IReadOnlyList<string> requirements,
        string progressSummary,
        string listHint,
        string statusSummary)
    {
        IsSatisfied = isSatisfied;
        LockReason = lockReason;
        Requirements = requirements;
        ProgressSummary = progressSummary;
        ListHint = listHint;
        StatusSummary = statusSummary;
    }

    public bool IsSatisfied { get; }

    public string LockReason { get; }

    public IReadOnlyList<string> Requirements { get; }

    public string ProgressSummary { get; }

    public string ListHint { get; }

    public string StatusSummary { get; }
}
