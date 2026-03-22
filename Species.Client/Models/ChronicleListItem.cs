using Species.Domain.Models;

namespace Species.Client.Models;

public sealed class ChronicleListItem
{
    public ChronicleListItem(
        string id,
        int eventYear,
        int eventMonth,
        string dateText,
        string headline,
        IReadOnlyList<ChronicleTextToken> headlineTokens,
        string category,
        string impact,
        bool isMilestone)
    {
        Id = id;
        EventYear = eventYear;
        EventMonth = eventMonth;
        DateText = dateText;
        Headline = headline;
        HeadlineTokens = headlineTokens;
        Category = category;
        Impact = impact;
        IsMilestone = isMilestone;
    }

    public string Id { get; }

    public int EventYear { get; }

    public int EventMonth { get; }

    public string DateText { get; }

    public string Headline { get; }

    public IReadOnlyList<ChronicleTextToken> HeadlineTokens { get; }

    public string Category { get; }

    public string Impact { get; }

    public bool IsMilestone { get; }
}
