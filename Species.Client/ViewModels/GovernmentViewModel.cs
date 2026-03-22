namespace Species.Client.ViewModels;

public sealed class GovernmentViewModel
{
    public GovernmentViewModel(
        string polityName,
        string currentDate,
        bool isSimulationRunning,
        string governmentForm,
        string capital,
        string founded)
    {
        PolityName = polityName;
        CurrentDate = currentDate;
        IsSimulationRunning = isSimulationRunning;
        GovernmentForm = governmentForm;
        Capital = capital;
        Founded = founded;
    }

    public string PolityName { get; }

    public string CurrentDate { get; }

    public bool IsSimulationRunning { get; }

    public string GovernmentForm { get; }

    public string Capital { get; }

    public string Founded { get; }
}
