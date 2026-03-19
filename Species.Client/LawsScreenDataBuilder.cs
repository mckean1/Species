using Species.Domain.Models;

public static class LawsScreenDataBuilder
{
    public static LawsScreenData Build(World world, string focalGroupId, int selectedIndex)
    {
        var focusGroup = PlayerFocus.Resolve(world, focalGroupId);
        var polityName = focusGroup?.Name ?? "Unknown polity";
        var items = Array.Empty<LawScreenItem>();

        return new LawsScreenData(
            polityName,
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            items,
            null,
            0,
            [
                "No laws established yet",
                "This polity has not adopted formal laws yet"
            ],
            [
                "No draft laws available",
                "When law data is introduced, active and draft laws will appear here"
            ]);
    }

    private static string FormatMonthYear(int month, int year)
    {
        var monthText = month switch
        {
            1 => "Jan",
            2 => "Feb",
            3 => "Mar",
            4 => "Apr",
            5 => "May",
            6 => "Jun",
            7 => "Jul",
            8 => "Aug",
            9 => "Sep",
            10 => "Oct",
            11 => "Nov",
            12 => "Dec",
            _ => "Jan"
        };

        return $"{monthText} {year:D3}";
    }
}

public sealed record LawsScreenData(
    string PolityName,
    string CurrentDate,
    IReadOnlyList<LawScreenItem> Laws,
    LawScreenItem? SelectedLaw,
    int SelectedIndex,
    IReadOnlyList<string> EmptyStateNotes,
    IReadOnlyList<string> DraftNotes);

public sealed record LawScreenItem(
    string Id,
    string Name,
    LawScreenStatus Status,
    string Category,
    bool IsQuestionable,
    string Description,
    string WhyItMatters,
    IReadOnlyList<string> Benefits,
    IReadOnlyList<string> Risks);

public enum LawScreenStatus
{
    Active,
    Draft,
    Inactive
}
