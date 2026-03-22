using Species.Client.Models;

namespace Species.Client.ViewModels;

public sealed class PolityViewModel
{
    public PolityViewModel(
        string polityName,
        string currentDate,
        bool isSimulationRunning,
        string governmentForm,
        string population,
        string settlementCount,
        string foodStores,
        string pressureState,
        IReadOnlyList<PolityPressureItem> topPressures,
        string strength,
        string concern)
    {
        PolityName = polityName;
        CurrentDate = currentDate;
        IsSimulationRunning = isSimulationRunning;
        GovernmentForm = governmentForm;
        Population = population;
        SettlementCount = settlementCount;
        FoodStores = foodStores;
        PressureState = pressureState;
        TopPressures = topPressures;
        Strength = strength;
        Concern = concern;
    }

    public string PolityName { get; }

    public string CurrentDate { get; }

    public bool IsSimulationRunning { get; }

    public string GovernmentForm { get; }

    public string Population { get; }

    public string SettlementCount { get; }

    public string FoodStores { get; }

    public string PressureState { get; }

    public IReadOnlyList<PolityPressureItem> TopPressures { get; }

    public string Strength { get; }

    public string Concern { get; }
}
