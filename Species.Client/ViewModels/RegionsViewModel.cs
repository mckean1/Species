using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class RegionsViewModel
{
    public RegionsViewModel(
        string currentDate,
        IReadOnlyList<RegionSummary> regions,
        RegionSummary? selectedRegion,
        int selectedIndex)
    {
        CurrentDate = currentDate;
        Regions = regions;
        SelectedRegion = selectedRegion;
        SelectedIndex = selectedIndex;
    }

    public string CurrentDate { get; }

    public IReadOnlyList<RegionSummary> Regions { get; }

    public RegionSummary? SelectedRegion { get; }

    public int SelectedIndex { get; }
}
