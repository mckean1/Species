using Species.Domain.Models;

public static class PlayerScreenShell
{
    private const string Reset = "\u001b[0m";
    private const string Dim = "\u001b[38;5;245m";
    private const string PaneTitle = "\u001b[38;5;222m";
    private const string Blue = "\u001b[38;5;111m";
    private const string Green = "\u001b[38;5;114m";
    private const string Yellow = "\u001b[38;5;221m";
    private const string White = "\u001b[97m";

    public static IReadOnlyList<string> BuildHeader(
        string screenName,
        string polityName,
        string currentDate,
        bool isSimulationRunning,
        int innerWidth)
    {
        var stateText = isSimulationRunning ? $"{Green}Running{Reset}" : $"{Yellow}Paused{Reset}";
        return
        [
            HorizontalBorder(innerWidth),
            BorderLine(PadBetween($"{PaneTitle}{screenName}{Reset}", $"{White}{currentDate}{Reset}", innerWidth), innerWidth),
            BorderLine(PadBetween($"{Dim}Polity:{Reset} {Blue}{polityName}{Reset}", $"{Dim}State:{Reset} {stateText}", innerWidth), innerWidth),
            HorizontalBorder(innerWidth)
        ];
    }

    public static string BuildFooter(int innerWidth, string? selectionLabel = null)
    {
        return BorderLine(PlayerScreenNavigation.BuildFooterText(innerWidth, Dim, Reset, selectionLabel), innerWidth);
    }

    public static string ResolvePolityName(World world, string focalPolityId)
    {
        return PlayerFocus.Resolve(world, focalPolityId)?.Name ?? "Unknown polity";
    }

    public static string HorizontalBorder(int innerWidth)
    {
        return "+" + new string('-', Math.Max(0, innerWidth + 2)) + "+";
    }

    public static string BorderLine(string content, int innerWidth)
    {
        return $"| {PadVisible(content, Math.Max(0, innerWidth))} |";
    }

    public static string PadBetween(string left, string right, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var leftLength = VisibleLength(left);
        var rightLength = VisibleLength(right);
        var spaces = Math.Max(1, width - leftLength - rightLength);
        return left + new string(' ', spaces) + right;
    }

    public static string PadVisible(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var visibleLength = VisibleLength(text);
        if (visibleLength >= width)
        {
            return text;
        }

        return text + new string(' ', width - visibleLength);
    }

    public static int VisibleLength(string text)
    {
        var length = 0;

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\u001b')
            {
                while (index < text.Length && text[index] != 'm')
                {
                    index++;
                }

                continue;
            }

            length++;
        }

        return length;
    }
}
