using Species.Domain.Models;

namespace Species.Client.Presentation;

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
            BorderLine(PadBetween($"{White}Polity:{Reset} {Blue}{polityName}{Reset}", $"{White}State:{Reset} {stateText}", innerWidth), innerWidth),
            HorizontalBorder(innerWidth)
        ];
    }

    public static string BuildFooter(
        int innerWidth,
        IReadOnlyList<string> fullControls,
        IReadOnlyList<string>? mediumControls = null,
        IReadOnlyList<string>? smallControls = null)
    {
        var rendered = RenderFooter(innerWidth, fullControls, mediumControls ?? fullControls, smallControls ?? mediumControls ?? fullControls);
        return BorderLine(rendered, innerWidth);
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

    public static string FitVisible(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        var visibleLength = 0;
        var index = 0;

        while (index < text.Length && visibleLength < width)
        {
            if (text[index] == '\u001b')
            {
                var start = index;
                while (index < text.Length && text[index] != 'm')
                {
                    index++;
                }

                if (index < text.Length)
                {
                    index++;
                }

                builder.Append(text[start..index]);
                continue;
            }

            builder.Append(text[index]);
            index++;
            visibleLength++;
        }

        if (visibleLength < width)
        {
            builder.Append(' ', width - visibleLength);
        }

        return builder.ToString();
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

    private static string RenderFooter(
        int innerWidth,
        IReadOnlyList<string> fullControls,
        IReadOnlyList<string> mediumControls,
        IReadOnlyList<string> smallControls)
    {
        var full = string.Join("   ", fullControls);
        if (VisibleLength(full) <= innerWidth)
        {
            return full;
        }

        var medium = string.Join("   ", mediumControls);
        if (VisibleLength(medium) <= innerWidth)
        {
            return medium;
        }

        var small = string.Join("   ", smallControls);
        if (VisibleLength(small) <= innerWidth)
        {
            return small;
        }

        var fallback = new List<string>();
        foreach (var control in smallControls)
        {
            var candidate = fallback.Count == 0
                ? control
                : $"{string.Join("   ", fallback)}   {control}";
            if (VisibleLength(candidate) > innerWidth)
            {
                break;
            }

            fallback.Add(control);
        }

        return fallback.Count > 0 ? string.Join("   ", fallback) : string.Empty;
    }
}
