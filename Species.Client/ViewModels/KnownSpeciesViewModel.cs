using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class KnownSpeciesViewModel
{
    public KnownSpeciesViewModel(
        string currentDate,
        IReadOnlyList<KnownSpeciesSummary> species,
        KnownSpeciesSummary? selectedSpecies,
        int selectedIndex)
    {
        CurrentDate = currentDate;
        Species = species;
        SelectedSpecies = selectedSpecies;
        SelectedIndex = selectedIndex;
    }

    public string CurrentDate { get; }

    public IReadOnlyList<KnownSpeciesSummary> Species { get; }

    public KnownSpeciesSummary? SelectedSpecies { get; }

    public int SelectedIndex { get; }
}
