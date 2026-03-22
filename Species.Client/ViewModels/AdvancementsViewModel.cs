using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class AdvancementsViewModel
{
    public AdvancementsViewModel(
        string currentDate,
        IReadOnlyList<AdvancementScreenItem> items,
        AdvancementScreenItem? selectedItem,
        int selectedIndex,
        int completedCount,
        int availableCount,
        int lockedCount)
    {
        CurrentDate = currentDate;
        Items = items;
        SelectedItem = selectedItem;
        SelectedIndex = selectedIndex;
        CompletedCount = completedCount;
        AvailableCount = availableCount;
        LockedCount = lockedCount;
    }

    public string CurrentDate { get; }

    public IReadOnlyList<AdvancementScreenItem> Items { get; }

    public AdvancementScreenItem? SelectedItem { get; }

    public int SelectedIndex { get; }

    public int CompletedCount { get; }

    public int AvailableCount { get; }

    public int LockedCount { get; }
}
