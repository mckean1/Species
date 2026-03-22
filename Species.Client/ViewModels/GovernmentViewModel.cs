namespace Species.Client.ViewModels;

public sealed class GovernmentViewModel
{
    public GovernmentViewModel(
        string polityName,
        string currentDate,
        string governmentForm,
        string capital,
        string founded)
    {
        PolityName = polityName;
        CurrentDate = currentDate;
        GovernmentForm = governmentForm;
        Capital = capital;
        Founded = founded;
    }

    public string PolityName { get; }

    public string CurrentDate { get; }

    public string GovernmentForm { get; }

    public string Capital { get; }

    public string Founded { get; }
}
