using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class LawsViewModel
{
    public LawsViewModel(
        string polityName,
        string currentDate,
        bool isSimulationRunning,
        IReadOnlyList<LawScreenItem> laws,
        IReadOnlyList<LawScreenItem> pendingDecisions,
        IReadOnlyList<LawScreenItem> recentDecisions,
        LawScreenItem? selectedLaw,
        int selectedIndex,
        bool hasActiveProposal,
        bool hasSelectedPendingDecision,
        bool isActionMenuOpen,
        int selectedActionIndex,
        IReadOnlyList<string> governanceSummary,
        IReadOnlyList<EnactedLawScreenItem> enactedLaws,
        IReadOnlyList<string> notes,
        IReadOnlyList<string> emptyStateNotes)
    {
        PolityName = polityName;
        CurrentDate = currentDate;
        IsSimulationRunning = isSimulationRunning;
        Laws = laws;
        PendingDecisions = pendingDecisions;
        RecentDecisions = recentDecisions;
        SelectedLaw = selectedLaw;
        SelectedIndex = selectedIndex;
        HasActiveProposal = hasActiveProposal;
        HasSelectedPendingDecision = hasSelectedPendingDecision;
        IsActionMenuOpen = isActionMenuOpen;
        SelectedActionIndex = selectedActionIndex;
        GovernanceSummary = governanceSummary;
        EnactedLaws = enactedLaws;
        Notes = notes;
        EmptyStateNotes = emptyStateNotes;
    }

    public string PolityName { get; }

    public string CurrentDate { get; }

    public bool IsSimulationRunning { get; }

    public IReadOnlyList<LawScreenItem> Laws { get; }

    public IReadOnlyList<LawScreenItem> PendingDecisions { get; }

    public IReadOnlyList<LawScreenItem> RecentDecisions { get; }

    public LawScreenItem? SelectedLaw { get; }

    public int SelectedIndex { get; }

    public bool HasActiveProposal { get; }

    public bool HasSelectedPendingDecision { get; }

    public bool IsActionMenuOpen { get; }

    public int SelectedActionIndex { get; }

    public IReadOnlyList<string> GovernanceSummary { get; }

    public IReadOnlyList<EnactedLawScreenItem> EnactedLaws { get; }

    public IReadOnlyList<string> Notes { get; }

    public IReadOnlyList<string> EmptyStateNotes { get; }
}
