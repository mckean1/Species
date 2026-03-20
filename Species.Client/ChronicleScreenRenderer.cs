using System.Text;
using Species.Domain.Models;

public static class ChronicleScreenRenderer
{
    private const string Reset = "\u001b[0m";
    private const string Dim = "\u001b[38;5;245m";
    private const string PaneTitle = "\u001b[38;5;222m";
    private const string Blue = "\u001b[38;5;111m";
    private const string Cyan = "\u001b[38;5;117m";
    private const string Purple = "\u001b[38;5;141m";
    private const string Orange = "\u001b[38;5;215m";
    private const string Yellow = "\u001b[38;5;221m";
    private const string Green = "\u001b[38;5;114m";
    private const string Red = "\u001b[38;5;210m";
    private const string HighlightBackground = "\u001b[48;5;236m";

    public static string Render(World world, PlayerViewState viewState, TerminalViewport viewport)
    {
        var data = ChronicleScreenDataBuilder.Build(world, viewState.FocalPolityId, viewState);
        var innerWidth = Math.Max(84, viewport.Width - 4);
        var headerTitle = $"Chronicle [{DescribeMode(data.Mode)}]";
        var urgentHeight = Math.Clamp(4 + Math.Max(1, data.UrgentItems.Count), 5, 7);
        var bodyHeight = Math.Max(10, viewport.Height - 12 - urgentHeight);
        var leftWidth = Math.Max(38, ((innerWidth - 3) * 11) / 20);
        var rightWidth = Math.Max(28, innerWidth - leftWidth - 3);

        var lines = new List<string>();
        lines.AddRange(PlayerScreenShell.BuildHeader(headerTitle, data.PolityName, data.CurrentDate, viewState.IsSimulationRunning, innerWidth));
        lines.Add(BorderLine($"{PaneTitle}Urgent{Reset}", innerWidth));
        lines.AddRange(BuildUrgentSection(data, innerWidth, urgentHeight).Select(line => BorderLine(line, innerWidth)));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        lines.Add(BorderLine($"{FitVisible($"{PaneTitle}{DescribeMode(data.Mode)}{Reset}", leftWidth)} | {FitVisible($"{PaneTitle}Selected / Current State{Reset}", rightWidth)}", innerWidth));

        var leftLines = BuildEntryPane(data, leftWidth, bodyHeight);
        var rightLines = BuildDetailPane(data, rightWidth, bodyHeight);
        for (var row = 0; row < bodyHeight; row++)
        {
            var left = row < leftLines.Count ? leftLines[row] : string.Empty;
            var right = row < rightLines.Count ? rightLines[row] : string.Empty;
            lines.Add(BorderLine($"{FitVisible(left, leftWidth)} | {FitVisible(right, rightWidth)}", innerWidth));
        }

        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        lines.Add(PlayerScreenShell.BuildFooter(
            innerWidth,
            ["Tab: Screens", "Up/Down: Navigate", "Enter: Select", "Left/Right: Mode", "Space: Pause/Run", "N: Next Tick", "Backspace: Live"],
            ["Tab: Screens", "Up/Down: Navigate", "Enter: Select", "Left/Right: Mode", "Space: Pause/Run"],
            ["Tab: Screens", "Up/Down: Navigate", "Enter: Select"]));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> BuildUrgentSection(ChronicleScreenData data, int width, int height)
    {
        var lines = new List<string>
        {
            FitVisible($"{Dim}Current alerts stay visible in every Chronicle mode.{Reset}", width)
        };

        if (data.UrgentItems.Count == 0)
        {
            lines.AddRange(Wrap($"{Dim}No urgent developments need immediate action.{Reset}", width));
        }
        else
        {
            for (var index = 0; index < data.UrgentItems.Count; index++)
            {
                var item = data.UrgentItems[index];
                var text = $"{(index == 0 ? Red : Yellow)}* {Reset}{item.Text}";
                if (data.SelectedArea == ChronicleSelectionArea.Urgent &&
                    data.SelectedUrgent is not null &&
                    string.Equals(data.SelectedUrgent.Id, item.Id, StringComparison.Ordinal))
                {
                    text = $"{HighlightBackground}{FitVisible(text, Math.Max(0, width - 2))}{Reset}";
                    lines.Add(text);
                    continue;
                }

                lines.AddRange(Wrap(text, width));
            }
        }

        while (lines.Count < height)
        {
            lines.Add(string.Empty);
        }

        return lines.Take(height).ToArray();
    }

    private static IReadOnlyList<string> BuildEntryPane(ChronicleScreenData data, int width, int height)
    {
        var lines = new List<string>
        {
            $"{Dim}{DescribeModeSubtitle(data.Mode)}{Reset}",
            $"{Dim}{new string('-', Math.Max(0, width))}{Reset}"
        };

        if (data.Entries.Count == 0)
        {
            lines.AddRange(Wrap($"{Dim}No visible entries are available in this mode yet.{Reset}", width));
        }
        else
        {
            var selectedIndex = ResolveSelectedEntryIndex(data);
            var visibleRows = Math.Max(1, height - lines.Count);
            var startIndex = Math.Clamp(selectedIndex - (visibleRows / 2), 0, Math.Max(0, data.Entries.Count - visibleRows));
            var endIndex = Math.Min(data.Entries.Count, startIndex + visibleRows);

            for (var index = startIndex; index < endIndex; index++)
            {
                var entry = data.Entries[index];
                var prefix = index == selectedIndex && data.SelectedArea == ChronicleSelectionArea.Entries
                    ? $"{HighlightBackground}{Blue}> {Reset}{HighlightBackground}"
                    : "  ";
                var milestoneBadge = entry.IsMilestone ? $"{Orange}[!]{Reset} " : string.Empty;
                var row = $"{milestoneBadge}{Dim}{entry.DateText}{Reset} {entry.Headline}";
                if (index == selectedIndex && data.SelectedArea == ChronicleSelectionArea.Entries)
                {
                    lines.Add($"{prefix}{FitVisible(row, Math.Max(0, width - 2))}{Reset}");
                }
                else
                {
                    lines.Add(FitVisible($"{prefix}{row}", width));
                }
            }
        }

        while (lines.Count < height)
        {
            lines.Add(string.Empty);
        }

        return lines.Take(height).ToArray();
    }

    private static IReadOnlyList<string> BuildDetailPane(ChronicleScreenData data, int width, int height)
    {
        var lines = new List<string>();

        if (data.SelectedArea == ChronicleSelectionArea.Urgent && data.SelectedUrgent is not null)
        {
            lines.Add($"{PaneTitle}Selected Urgent Item{Reset}");
            lines.AddRange(Wrap($"{Red}{data.SelectedUrgent.Text}{Reset}", width));
            if (!string.IsNullOrWhiteSpace(data.SelectedUrgent.Cause))
            {
                lines.Add($"{Dim}{new string('-', width)}{Reset}");
                lines.AddRange(Wrap($"Cause: {data.SelectedUrgent.Cause}", width));
            }

            if (!string.IsNullOrWhiteSpace(data.SelectedUrgent.Impact))
            {
                lines.AddRange(Wrap($"Why it matters: {data.SelectedUrgent.Impact}", width));
            }

            if (data.SelectedUrgent.TargetScreen == PlayerScreen.Laws)
            {
                lines.Add($"{Dim}{new string('-', width)}{Reset}");
                lines.AddRange(Wrap($"Enter opens Laws and selects the pending decision.", width));
            }
        }
        else if (data.SelectedEntry is not null)
        {
            lines.Add($"{PaneTitle}Selected Entry{Reset}");
            lines.AddRange(Wrap($"{Blue}{data.SelectedEntry.Headline}{Reset}", width));
            lines.AddRange(Wrap($"{Dim}{data.SelectedEntry.DateText}{Reset}  {Purple}{data.SelectedEntry.Category}{Reset}", width));
            lines.Add($"{Dim}{new string('-', width)}{Reset}");
            lines.AddRange(Wrap(data.SelectedEntry.Impact, width));
        }
        else
        {
            lines.Add($"{PaneTitle}Selected Entry{Reset}");
            lines.AddRange(Wrap($"{Dim}Nothing is selected yet.{Reset}", width));
        }

        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Current State{Reset}");
        foreach (var line in data.ConditionSummary)
        {
            lines.AddRange(Wrap($"{Yellow}* {Reset}{line}", width));
        }

        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Mode Notes{Reset}");
        foreach (var line in data.ModeNotes)
        {
            lines.AddRange(Wrap($"{Dim}* {line}{Reset}", width));
        }

        while (lines.Count < height)
        {
            lines.Add(string.Empty);
        }

        return lines.Take(height).ToArray();
    }

    private static int ResolveSelectedEntryIndex(ChronicleScreenData data)
    {
        if (data.SelectedEntry is null)
        {
            return 0;
        }

        for (var index = 0; index < data.Entries.Count; index++)
        {
            if (string.Equals(data.Entries[index].Id, data.SelectedEntry.Id, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return 0;
    }

    private static IReadOnlyList<string> Wrap(string text, int width)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [string.Empty];
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = string.Empty;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
            if (VisibleLength(candidate) <= width)
            {
                current = candidate;
                continue;
            }

            lines.Add(FitVisible(current, width));
            current = word;
        }

        if (!string.IsNullOrEmpty(current))
        {
            lines.Add(FitVisible(current, width));
        }

        return lines;
    }

    private static string DescribeMode(ChronicleMode mode)
    {
        return mode switch
        {
            ChronicleMode.Live => "Live",
            ChronicleMode.Archive => "Archive",
            _ => "Milestones"
        };
    }

    private static string DescribeModeSubtitle(ChronicleMode mode)
    {
        return mode switch
        {
            ChronicleMode.Live => "Newest important developments first.",
            ChronicleMode.Archive => "Browse older visible entries without snapping back.",
            _ => "Only higher-value turning points are shown here."
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

    private static string BorderLine(string content, int innerWidth)
    {
        return $"| {FitVisible(content, innerWidth)} |";
    }
}
