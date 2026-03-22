using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class KnownPolitiesViewModel
{
    public KnownPolitiesViewModel(
        string polityName,
        string currentDate,
        bool isSimulationRunning,
        IReadOnlyList<KnownPolitySummary> polities,
        KnownPolitySummary? selectedPolity,
        int selectedIndex)
    {
        PolityName = polityName;
        CurrentDate = currentDate;
        IsSimulationRunning = isSimulationRunning;
        Polities = polities;
        SelectedPolity = selectedPolity;
        SelectedIndex = selectedIndex;
    }

    public string PolityName { get; }

    public string CurrentDate { get; }

    public bool IsSimulationRunning { get; }

    public IReadOnlyList<KnownPolitySummary> Polities { get; }

    public KnownPolitySummary? SelectedPolity { get; }

    public int SelectedIndex { get; }
}
