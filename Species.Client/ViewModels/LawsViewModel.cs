using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class LawsViewModel
{
    public LawsViewModel(
        string polityName,
        string currentDate,
        IReadOnlyList<LawScreenItem> laws,
        IReadOnlyList<LawScreenItem> pendingDecisions,
        IReadOnlyList<LawScreenItem> recentDecisions,
        LawScreenItem? selectedLaw,
        int selectedIndex,
        bool hasActiveProposal,
        bool hasSelectedPendingDecision,
        IReadOnlyList<string> governanceSummary,
        IReadOnlyList<EnactedLawScreenItem> enactedLaws,
        IReadOnlyList<string> notes,
        IReadOnlyList<string> emptyStateNotes)
    {
        PolityName = polityName;
        CurrentDate = currentDate;
        Laws = laws;
        PendingDecisions = pendingDecisions;
        RecentDecisions = recentDecisions;
        SelectedLaw = selectedLaw;
        SelectedIndex = selectedIndex;
        HasActiveProposal = hasActiveProposal;
        HasSelectedPendingDecision = hasSelectedPendingDecision;
        GovernanceSummary = governanceSummary;
        EnactedLaws = enactedLaws;
        Notes = notes;
        EmptyStateNotes = emptyStateNotes;
    }

    public string PolityName { get; }

    public string CurrentDate { get; }

    public IReadOnlyList<LawScreenItem> Laws { get; }

    public IReadOnlyList<LawScreenItem> PendingDecisions { get; }

    public IReadOnlyList<LawScreenItem> RecentDecisions { get; }

    public LawScreenItem? SelectedLaw { get; }

    public int SelectedIndex { get; }

    public bool HasActiveProposal { get; }

    public bool HasSelectedPendingDecision { get; }

    public IReadOnlyList<string> GovernanceSummary { get; }

    public IReadOnlyList<EnactedLawScreenItem> EnactedLaws { get; }

    public IReadOnlyList<string> Notes { get; }

    public IReadOnlyList<string> EmptyStateNotes { get; }
}
