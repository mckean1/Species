namespace Species.Client.Models;

public sealed class ChronicleListItem
{
    public ChronicleListItem(
        string id,
        int eventYear,
        int eventMonth,
        string dateText,
        string headline,
        string category,
        string impact,
        bool isMilestone)
    {
        Id = id;
        EventYear = eventYear;
        EventMonth = eventMonth;
        DateText = dateText;
        Headline = headline;
        Category = category;
        Impact = impact;
        IsMilestone = isMilestone;
    }

    public string Id { get; }

    public int EventYear { get; }

    public int EventMonth { get; }

    public string DateText { get; }

    public string Headline { get; }

    public string Category { get; }

    public string Impact { get; }

    public bool IsMilestone { get; }
}
