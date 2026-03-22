using Species.Client.Models;

namespace Species.Client.Presentation;

public sealed class ChronicleSelectionInfo
{
    public ChronicleSelectionInfo(int urgentCount, int entryCount, ChronicleUrgentItem? selectedUrgent)
    {
        UrgentCount = urgentCount;
        EntryCount = entryCount;
        SelectedUrgent = selectedUrgent;
    }

    public int UrgentCount { get; }

    public int EntryCount { get; }

    public ChronicleUrgentItem? SelectedUrgent { get; }
}
