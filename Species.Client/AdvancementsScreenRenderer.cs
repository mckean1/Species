using System.Text;
using Species.Domain.Catalogs;
using Species.Domain.Models;

public static class AdvancementsScreenRenderer
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
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog,
        int selectedIndex,
        bool isSimulationRunning,
        TerminalViewport viewport)
    {
        var data = AdvancementsScreenDataBuilder.Build(world, discoveryCatalog, advancementCatalog, selectedIndex);
        var innerWidth = Math.Max(80, viewport.Width - 4);
        var listWidth = Math.Max(36, ((innerWidth - 3) * 10) / 21);
        var detailWidth = Math.Max(28, innerWidth - listWidth - 3);
        var bodyHeight = Math.Max(18, viewport.Height - 6);

        var listLines = BuildAdvancementList(data, listWidth, bodyHeight);
        var detailLines = BuildDetailPanel(data.SelectedItem, detailWidth, bodyHeight, isSimulationRunning);

        var lines = new List<string>
        {
            HorizontalBorder(innerWidth),
            BorderLine(PadBetween($"{PaneTitle}Advancements{Reset}", $"{Dim}{data.CurrentDate}{Reset}", innerWidth), innerWidth),
            HorizontalBorder(innerWidth)
        };

        for (var row = 0; row < bodyHeight; row++)
        {
            var left = row < listLines.Count ? listLines[row] : string.Empty;
            var right = row < detailLines.Count ? detailLines[row] : string.Empty;
            lines.Add(BorderLine($"{FitVisible(left, listWidth)} | {FitVisible(right, detailWidth)}", innerWidth));
        }

        lines.Add(HorizontalBorder(innerWidth));
        lines.Add(BorderLine(PlayerScreenNavigation.BuildFooterText(innerWidth, Dim, Reset, "Select advancement"), innerWidth));
        lines.Add(HorizontalBorder(innerWidth));

        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> BuildAdvancementList(AdvancementsScreenData data, int width, int bodyHeight)
    {
        var lines = new List<string>
        {
            $"{PaneTitle}Advancements{Reset}",
            $"{Green}Completed{Reset} {data.CompletedCount}   {Yellow}Available{Reset} {data.AvailableCount}   {Red}Locked{Reset} {data.LockedCount}",
            $"{Dim}Discoveries reveal the world; advancements are usable capabilities.{Reset}",
            $"{Dim}{new string('-', width)}{Reset}"
        };

        if (data.Items.Count == 0)
        {
            lines.Add($"{Dim}No advancements are defined yet.{Reset}");
        }
        else
        {
            var visibleRows = Math.Max(1, bodyHeight - lines.Count);
            var startIndex = Math.Clamp(data.SelectedIndex - (visibleRows / 2), 0, Math.Max(0, data.Items.Count - visibleRows));
            var endIndex = Math.Min(data.Items.Count, startIndex + visibleRows);

            for (var index = startIndex; index < endIndex; index++)
            {
                var item = data.Items[index];
                var row = $"{ColorStatusBadge(item.Status)} {Blue}{item.Name}{Reset}";

                if (!string.IsNullOrWhiteSpace(item.ListHint))
                {
                    row += $" {Dim}({item.ListHint}){Reset}";
                }

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
        AdvancementScreenItem? item,
        int width,
        int bodyHeight,
        bool isSimulationRunning)
    {
        var lines = new List<string>();

        if (item is null)
        {
            lines.Add($"{PaneTitle}Selected Advancement{Reset}");
            lines.Add($"{Dim}No advancement data is available.{Reset}");
            while (lines.Count < bodyHeight)
            {
                lines.Add(string.Empty);
            }

            return lines;
        }

        lines.Add($"{PaneTitle}Selected Advancement{Reset}");
        lines.Add($"{Blue}{item.Name}{Reset}");
        lines.Add($"Status: {ColorStatusWord(item.Status)}");
        lines.Add($"Category: {Purple}{item.Category}{Reset}");
        lines.Add($"Simulation: {(isSimulationRunning ? $"{Green}Running{Reset}" : $"{Dim}Paused{Reset}")}");
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.AddRange(WrapText(item.Description, width));
        lines.Add(string.Empty);
        lines.Add($"{PaneTitle}Capability{Reset}");
        lines.AddRange(BuildBulletLines([item.CapabilitySummary], width, Green));
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Requirements{Reset}");
        lines.AddRange(BuildBulletLines(item.Requirements, width, Dim));
        lines.AddRange(BuildBulletLines(item.Notes, width, item.Status == AdvancementScreenStatus.Locked ? Orange : Green));

        if (!string.IsNullOrWhiteSpace(item.ProgressSummary))
        {
            lines.AddRange(BuildBulletLines([item.ProgressSummary], width, Yellow));
        }

        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Unlock Context{Reset}");
        lines.AddRange(BuildBulletLines([GetStatusMessage(item)], width, GetStatusColor(item.Status)));

        while (lines.Count < bodyHeight)
        {
            lines.Add(string.Empty);
        }

        return lines.Take(bodyHeight).ToArray();
    }

    private static string GetStatusMessage(AdvancementScreenItem item)
    {
        return item.Status switch
        {
            AdvancementScreenStatus.Completed => "This capability is already part of the polity's toolkit.",
            AdvancementScreenStatus.Available => "All known conditions are met. This advancement is ready to unlock.",
            _ => item.StatusSummary
        };
    }

    private static string GetStatusColor(AdvancementScreenStatus status)
    {
        return status switch
        {
            AdvancementScreenStatus.Completed => Green,
            AdvancementScreenStatus.Available => Yellow,
            _ => Red
        };
    }

    private static IReadOnlyList<string> BuildBulletLines(IEnumerable<string> items, int width, string color)
    {
        var lines = new List<string>();

        foreach (var item in items)
        {
            lines.AddRange(WrapBullet(item, width, color));
        }

        if (lines.Count == 0)
        {
            lines.Add(FitVisible($"{Dim}None{Reset}", width));
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

    private static string ColorStatusBadge(AdvancementScreenStatus status)
    {
        return status switch
        {
            AdvancementScreenStatus.Completed => $"{Green}[DONE]{Reset}",
            AdvancementScreenStatus.Available => $"{Yellow}[OPEN]{Reset}",
            _ => $"{Red}[LOCK]{Reset}"
        };
    }

    private static string ColorStatusWord(AdvancementScreenStatus status)
    {
        return status switch
        {
            AdvancementScreenStatus.Completed => $"{Green}Completed{Reset}",
            AdvancementScreenStatus.Available => $"{Yellow}Available{Reset}",
            _ => $"{Red}Locked{Reset}"
        };
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
