using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class AdvancementsViewModel
{
    public AdvancementsViewModel(
        string polityName,
        string currentDate,
        bool isSimulationRunning,
        IReadOnlyList<AdvancementScreenItem> items,
        AdvancementScreenItem? selectedItem,
        int selectedIndex,
        int completedCount,
        int availableCount,
        int lockedCount)
    {
        PolityName = polityName;
        CurrentDate = currentDate;
        IsSimulationRunning = isSimulationRunning;
        Items = items;
        SelectedItem = selectedItem;
        SelectedIndex = selectedIndex;
        CompletedCount = completedCount;
        AvailableCount = availableCount;
        LockedCount = lockedCount;
    }

    public string PolityName { get; }

    public string CurrentDate { get; }

    public bool IsSimulationRunning { get; }

    public IReadOnlyList<AdvancementScreenItem> Items { get; }

    public AdvancementScreenItem? SelectedItem { get; }

    public int SelectedIndex { get; }

    public int CompletedCount { get; }

    public int AvailableCount { get; }

    public int LockedCount { get; }
}
