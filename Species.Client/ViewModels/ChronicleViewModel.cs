using Species.Client.Enums;
using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class ChronicleViewModel
{
    public ChronicleViewModel(
        string currentDate,
        string polityName,
        bool isSimulationRunning,
        ChronicleMode mode,
        IReadOnlyList<ChronicleUrgentItem> urgentItems,
        IReadOnlyList<ChronicleListItem> entries,
        ChronicleUrgentItem? selectedUrgent,
        ChronicleListItem? selectedEntry,
        ChronicleSelectionArea selectedArea,
        IReadOnlyList<string> conditionSummary,
        IReadOnlyList<string> modeNotes)
    {
        CurrentDate = currentDate;
        PolityName = polityName;
        IsSimulationRunning = isSimulationRunning;
        Mode = mode;
        UrgentItems = urgentItems;
        Entries = entries;
        SelectedUrgent = selectedUrgent;
        SelectedEntry = selectedEntry;
        SelectedArea = selectedArea;
        ConditionSummary = conditionSummary;
        ModeNotes = modeNotes;
    }

    public string CurrentDate { get; }

    public string PolityName { get; }

    public bool IsSimulationRunning { get; }

    public ChronicleMode Mode { get; }

    public IReadOnlyList<ChronicleUrgentItem> UrgentItems { get; }

    public IReadOnlyList<ChronicleListItem> Entries { get; }

    public ChronicleUrgentItem? SelectedUrgent { get; }

    public ChronicleListItem? SelectedEntry { get; }

    public ChronicleSelectionArea SelectedArea { get; }

    public IReadOnlyList<string> ConditionSummary { get; }

    public IReadOnlyList<string> ModeNotes { get; }
}
