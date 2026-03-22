using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class KnownSpeciesViewModel
{
    public KnownSpeciesViewModel(
        string polityName,
        string currentDate,
        bool isSimulationRunning,
        IReadOnlyList<KnownSpeciesSummary> species,
        KnownSpeciesSummary? selectedSpecies,
        int selectedIndex)
    {
        PolityName = polityName;
        CurrentDate = currentDate;
        IsSimulationRunning = isSimulationRunning;
        Species = species;
        SelectedSpecies = selectedSpecies;
        SelectedIndex = selectedIndex;
    }

    public string PolityName { get; }

    public string CurrentDate { get; }

    public bool IsSimulationRunning { get; }

    public IReadOnlyList<KnownSpeciesSummary> Species { get; }

    public KnownSpeciesSummary? SelectedSpecies { get; }

    public int SelectedIndex { get; }
}
