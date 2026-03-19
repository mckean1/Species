using System.Text;
using Species.Domain.Catalogs;
using Species.Domain.Models;

public static class KnownSpeciesScreenRenderer
{
    private const string Reset = "\u001b[0m";
    private const string Dim = "\u001b[38;5;245m";
    private const string PaneTitle = "\u001b[38;5;222m";
    private const string Blue = "\u001b[38;5;111m";
    private const string Purple = "\u001b[38;5;141m";
    private const string Orange = "\u001b[38;5;215m";
    private const string Yellow = "\u001b[38;5;221m";
    private const string Green = "\u001b[38;5;114m";
    private const string Red = "\u001b[38;5;210m";
    private const string HighlightBackground = "\u001b[48;5;236m";

    public static string Render(
        World world,
        string focalGroupId,
        FaunaSpeciesCatalog faunaCatalog,
        int selectedIndex,
        bool isSimulationRunning,
        TerminalViewport viewport)
    {
        var data = KnownSpeciesScreenDataBuilder.Build(world, faunaCatalog, focalGroupId, selectedIndex);
        var innerWidth = Math.Max(80, viewport.Width - 4);
        var listWidth = Math.Max(34, ((innerWidth - 3) * 9) / 20);
        var detailWidth = Math.Max(28, innerWidth - listWidth - 3);
        var bodyHeight = Math.Max(18, viewport.Height - 6);

        var listLines = BuildSpeciesList(data, listWidth, bodyHeight);
        var detailLines = BuildDetailPanel(data.SelectedSpecies, detailWidth, bodyHeight, isSimulationRunning);

        var lines = new List<string>
        {
            HorizontalBorder(innerWidth),
            BorderLine(PadBetween($"{PaneTitle}Known Species{Reset}", $"{Dim}{data.CurrentDate}{Reset}", innerWidth), innerWidth),
            HorizontalBorder(innerWidth)
        };

        for (var row = 0; row < bodyHeight; row++)
        {
            var left = row < listLines.Count ? listLines[row] : string.Empty;
            var right = row < detailLines.Count ? detailLines[row] : string.Empty;
            lines.Add(BorderLine($"{FitVisible(left, listWidth)} | {FitVisible(right, detailWidth)}", innerWidth));
        }

        lines.Add(HorizontalBorder(innerWidth));
        lines.Add(BorderLine(PlayerScreenNavigation.BuildFooterText(innerWidth, Dim, Reset, "Select species"), innerWidth));
        lines.Add(HorizontalBorder(innerWidth));

        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> BuildSpeciesList(KnownSpeciesScreenData data, int width, int bodyHeight)
    {
        var lines = new List<string>
        {
            $"{PaneTitle}Known Species{Reset}"
        };

        if (data.Species.Count == 0)
        {
            lines.Add($"{Dim}No other species known yet.{Reset}");
        }
        else
        {
            var visibleRows = Math.Max(1, bodyHeight - 1);
            var startIndex = Math.Clamp(data.SelectedIndex - (visibleRows / 2), 0, Math.Max(0, data.Species.Count - visibleRows));
            var endIndex = Math.Min(data.Species.Count, startIndex + visibleRows);

            for (var index = startIndex; index < endIndex; index++)
            {
                var species = data.Species[index];
                var marker = species.IsPlayerSpecies ? $"{Green}[OURS]{Reset}" : $"{Dim}[SEEN]{Reset}";
                var row = $"{marker} {Blue}{species.Name}{Reset} {Dim}({species.Status}){Reset}";

                if (index == data.SelectedIndex)
                {
                    var selectedRow = FitVisible(row, Math.Max(0, width - 2));
                    lines.Add($"{HighlightBackground}{Yellow}> {Reset}{HighlightBackground}{selectedRow}{Reset}");
                }
                else
                {
                    lines.Add(FitVisible($"  {row}", width));
                }
            }
        }

        while (lines.Count < bodyHeight)
        {
            lines.Add(string.Empty);
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildDetailPanel(
        KnownSpeciesSummary? species,
        int width,
        int bodyHeight,
        bool isSimulationRunning)
    {
        var lines = new List<string>();

        if (species is null)
        {
            lines.Add($"{PaneTitle}Selected Species{Reset}");
            lines.Add($"{Dim}Only our own species is currently known.{Reset}");
            while (lines.Count < bodyHeight)
            {
                lines.Add(string.Empty);
            }

            return lines;
        }

        lines.Add($"{PaneTitle}Selected Species{Reset}");
        lines.Add($"{Blue}{species.Name}{Reset}");
        lines.Add($"Type: {species.Kind}");
        lines.Add($"Status: {ColorStatus(species.Status)}");
        lines.Add($"Presence: {Yellow}{species.Presence}{Reset}");
        lines.Add($"Runtime: {(isSimulationRunning ? $"{Green}Running{Reset}" : $"{Dim}Paused{Reset}")}");
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.AddRange(WrapText(species.Overview, width));
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Facts / Range{Reset}");
        lines.AddRange(BuildBulletLines(species.Facts, width, Blue));
        lines.Add($"{PaneTitle}Traits{Reset}");
        lines.AddRange(BuildBulletLines(species.Traits, width, Green));
        lines.Add($"{PaneTitle}Player Relevance{Reset}");
        lines.AddRange(BuildBulletLines(species.Relevance, width, species.IsPlayerSpecies ? Orange : Purple));

        while (lines.Count < bodyHeight)
        {
            lines.Add(string.Empty);
        }

        return lines.Take(bodyHeight).ToArray();
    }

    private static string ColorStatus(string status)
    {
        if (status.Contains("dangerous", StringComparison.OrdinalIgnoreCase))
        {
            return $"{Red}{status}{Reset}";
        }

        if (status.Contains("rare", StringComparison.OrdinalIgnoreCase))
        {
            return $"{Purple}{status}{Reset}";
        }

        if (status.Contains("pressured", StringComparison.OrdinalIgnoreCase))
        {
            return $"{Orange}{status}{Reset}";
        }

        return $"{Green}{status}{Reset}";
    }

    private static IReadOnlyList<string> BuildBulletLines(IReadOnlyList<string> items, int width, string color)
    {
        var lines = new List<string>();
        foreach (var item in items)
        {
            lines.AddRange(WrapBullet(item, width, color));
        }

        if (lines.Count == 0)
        {
            lines.Add(FitVisible($"{Dim}No notable traits recorded.{Reset}", width));
        }

        return lines;
    }

    private static IReadOnlyList<string> WrapText(string text, int width)
    {
        return Wrap(text, width, string.Empty, string.Empty);
    }

    private static IReadOnlyList<string> WrapBullet(string text, int width, string color)
    {
        return Wrap(text, width, $"{color}* {Reset}", "  ");
    }

    private static IReadOnlyList<string> Wrap(string text, int width, string firstPrefix, string nextPrefix)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return [FitVisible(firstPrefix, width)];
        }

        var lines = new List<string>();
        var current = firstPrefix;
        foreach (var word in words)
        {
            var candidate = BuildCandidate(current, word);
            if (VisibleLength(candidate) <= width)
            {
                current = candidate;
                continue;
            }

            lines.Add(FitVisible(current, width));
            current = string.IsNullOrEmpty(nextPrefix) ? word : $"{nextPrefix}{word}";
        }

        lines.Add(FitVisible(current, width));
        return lines;
    }

    private static string BuildCandidate(string current, string word)
    {
        if (string.IsNullOrEmpty(current))
        {
            return word;
        }

        return current.EndsWith(' ')
            ? $"{current}{word}"
            : $"{current} {word}";
    }

    private static string FitVisible(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
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

    private static int VisibleLength(string text)
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

    private static string PadBetween(string left, string right, int width)
    {
        var spaces = Math.Max(1, width - VisibleLength(left) - VisibleLength(right));
        return left + new string(' ', spaces) + right;
    }

    private static string HorizontalBorder(int innerWidth)
    {
        return "+" + new string('-', innerWidth + 2) + "+";
    }

    private static string BorderLine(string content, int innerWidth)
    {
        return $"| {FitVisible(content, innerWidth)} |";
    }
}
