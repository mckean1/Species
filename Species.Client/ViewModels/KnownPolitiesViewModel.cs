using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class KnownPolitiesViewModel
{
    public KnownPolitiesViewModel(
        string currentDate,
        IReadOnlyList<KnownPolitySummary> polities,
        KnownPolitySummary? selectedPolity,
        int selectedIndex)
    {
        CurrentDate = currentDate;
        Polities = polities;
        SelectedPolity = selectedPolity;
        SelectedIndex = selectedIndex;
    }

    public string CurrentDate { get; }

    public IReadOnlyList<KnownPolitySummary> Polities { get; }

    public KnownPolitySummary? SelectedPolity { get; }

    public int SelectedIndex { get; }
}
