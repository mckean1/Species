using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class RegionsViewModel
{
    public RegionsViewModel(
        string polityName,
        string currentDate,
        bool isSimulationRunning,
        IReadOnlyList<RegionSummary> regions,
        RegionSummary? selectedRegion,
        int selectedIndex)
    {
        PolityName = polityName;
        CurrentDate = currentDate;
        IsSimulationRunning = isSimulationRunning;
        Regions = regions;
        SelectedRegion = selectedRegion;
        SelectedIndex = selectedIndex;
    }

    public string PolityName { get; }

    public string CurrentDate { get; }

    public bool IsSimulationRunning { get; }

    public IReadOnlyList<RegionSummary> Regions { get; }

    public RegionSummary? SelectedRegion { get; }

    public int SelectedIndex { get; }
}
