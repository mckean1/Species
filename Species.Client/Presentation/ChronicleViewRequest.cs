using Species.Client.Enums;

namespace Species.Client.Presentation;

public sealed class ChronicleViewRequest
{
    public ChronicleViewRequest(
        ChronicleMode mode,
        ChronicleSelectionArea selectedArea,
        int selectedUrgentIndex,
        int selectedLiveEntryIndex,
        int selectedArchiveEntryIndex,
        int selectedMilestoneEntryIndex)
    {
        Mode = mode;
        SelectedArea = selectedArea;
        SelectedUrgentIndex = selectedUrgentIndex;
        SelectedLiveEntryIndex = selectedLiveEntryIndex;
        SelectedArchiveEntryIndex = selectedArchiveEntryIndex;
        SelectedMilestoneEntryIndex = selectedMilestoneEntryIndex;
    }

    public ChronicleMode Mode { get; }

    public ChronicleSelectionArea SelectedArea { get; }

    public int SelectedUrgentIndex { get; }

    public int SelectedLiveEntryIndex { get; }

    public int SelectedArchiveEntryIndex { get; }

    public int SelectedMilestoneEntryIndex { get; }
}
