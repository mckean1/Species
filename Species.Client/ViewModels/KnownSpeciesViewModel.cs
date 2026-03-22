using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class KnownSpeciesViewModel
{
    public KnownSpeciesViewModel(
        string polityName,
        string currentDate,
        bool isSimulationRunning,
        IReadOnlyList<KnownSpeciesSectionSummary> sections,
        IReadOnlyList<KnownSpeciesSummary> selectableSpecies,
        KnownSpeciesSummary? selectedSpecies,
        int selectedIndex)
    {
        PolityName = polityName;
        CurrentDate = currentDate;
        IsSimulationRunning = isSimulationRunning;
        Sections = sections;
        SelectableSpecies = selectableSpecies;
        SelectedSpecies = selectedSpecies;
        SelectedIndex = selectedIndex;
    }

    public string PolityName { get; }

    public string CurrentDate { get; }

    public bool IsSimulationRunning { get; }

    public IReadOnlyList<KnownSpeciesSectionSummary> Sections { get; }

    public IReadOnlyList<KnownSpeciesSummary> SelectableSpecies { get; }

    public KnownSpeciesSummary? SelectedSpecies { get; }

    public int SelectedIndex { get; }
}
