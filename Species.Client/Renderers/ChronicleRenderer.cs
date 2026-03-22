using Species.Client.Enums;
using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Client.Renderers;

public static class ChronicleRenderer
{
    private const string Reset = "\u001b[0m";
    private const string Dim = "\u001b[38;5;245m";
    private const string PaneTitle = "\u001b[38;5;222m";
    private const string Blue = "\u001b[38;5;111m";
    private const string Teal = "\u001b[38;5;80m";
    private const string Cyan = "\u001b[38;5;117m";
    private const string Purple = "\u001b[38;5;141m";
    private const string Orange = "\u001b[38;5;215m";
    private const string Yellow = "\u001b[38;5;221m";
    private const string Green = "\u001b[38;5;114m";
    private const string Red = "\u001b[38;5;210m";
    private const string HighlightBackground = "\u001b[48;5;236m";

    public static string Render(ChronicleViewModel data, TerminalViewport viewport)
    {
        var innerWidth = Math.Max(84, viewport.Width - 4);
        var headerTitle = $"Chronicle [{DescribeMode(data.Mode)}]";
        var urgentHeight = Math.Clamp(4 + Math.Max(1, data.UrgentItems.Count), 5, 7);
        var bodyHeight = Math.Max(10, viewport.Height - 12 - urgentHeight);
        var leftWidth = Math.Max(38, ((innerWidth - 3) * 11) / 20);
        var rightWidth = Math.Max(28, innerWidth - leftWidth - 3);

        var lines = new List<string>();
        lines.AddRange(PlayerScreenShell.BuildHeader(headerTitle, data.PolityName, data.CurrentDate, data.IsSimulationRunning, innerWidth));
        lines.Add(PlayerScreenShell.BorderLine(PlayerScreenShell.FitVisible($"{PaneTitle}Urgent{Reset}", innerWidth), innerWidth));
        lines.AddRange(BuildUrgentSection(data, innerWidth, urgentHeight).Select(line => PlayerScreenShell.BorderLine(PlayerScreenShell.FitVisible(line, innerWidth), innerWidth)));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        lines.Add(PlayerScreenShell.BorderLine(PlayerScreenShell.FitVisible($"{PlayerScreenShell.FitVisible($"{PaneTitle}{DescribeMode(data.Mode)}{Reset}", leftWidth)} | {PlayerScreenShell.FitVisible($"{PaneTitle}Selected / Current State{Reset}", rightWidth)}", innerWidth), innerWidth));

        var leftLines = BuildEntryPane(data, leftWidth, bodyHeight);
        var rightLines = BuildDetailPane(data, rightWidth, bodyHeight);
        for (var row = 0; row < bodyHeight; row++)
        {
            var left = row < leftLines.Count ? leftLines[row] : string.Empty;
            var right = row < rightLines.Count ? rightLines[row] : string.Empty;
            lines.Add(PlayerScreenShell.BorderLine(PlayerScreenShell.FitVisible($"{PlayerScreenShell.FitVisible(left, leftWidth)} | {PlayerScreenShell.FitVisible(right, rightWidth)}", innerWidth), innerWidth));
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

    private static IReadOnlyList<string> BuildUrgentSection(ChronicleViewModel data, int width, int height)
    {
        var lines = new List<string>
        {
            PlayerScreenShell.FitVisible($"{Dim}Current alerts stay visible in every Chronicle mode.{Reset}", width)
        };

        if (data.UrgentItems.Count == 0)
        {
            lines.AddRange(RendererTextWrap.WrapText($"{Dim}No urgent developments need immediate action.{Reset}", width, returnEmptyLineWhenBlank: true));
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
                    text = $"{HighlightBackground}{PlayerScreenShell.FitVisible(text, Math.Max(0, width - 2))}{Reset}";
                    lines.Add(text);
                    continue;
                }

                lines.AddRange(RendererTextWrap.WrapText(text, width, returnEmptyLineWhenBlank: true));
            }
        }

        while (lines.Count < height)
        {
            lines.Add(string.Empty);
        }

        return lines.Take(height).ToArray();
    }

    private static IReadOnlyList<string> BuildEntryPane(ChronicleViewModel data, int width, int height)
    {
        var lines = new List<string>
        {
            $"{Dim}{DescribeModeSubtitle(data.Mode)}{Reset}",
            $"{Dim}{new string('-', Math.Max(0, width))}{Reset}"
        };

        if (data.Entries.Count == 0)
        {
            lines.AddRange(RendererTextWrap.WrapText($"{Dim}No visible entries are available in this mode yet.{Reset}", width, returnEmptyLineWhenBlank: true));
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
                var row = $"{milestoneBadge}{Dim}{entry.DateText}{Reset} {RenderTokens(entry.HeadlineTokens)}";
                if (index == selectedIndex && data.SelectedArea == ChronicleSelectionArea.Entries)
                {
                    lines.Add($"{prefix}{PlayerScreenShell.FitVisible(row, Math.Max(0, width - 2))}{Reset}");
                }
                else
                {
                    lines.Add(PlayerScreenShell.FitVisible($"{prefix}{row}", width));
                }
            }
        }

        while (lines.Count < height)
        {
            lines.Add(string.Empty);
        }

        return lines.Take(height).ToArray();
    }

    private static IReadOnlyList<string> BuildDetailPane(ChronicleViewModel data, int width, int height)
    {
        var lines = new List<string>();

        if (data.SelectedArea == ChronicleSelectionArea.Urgent && data.SelectedUrgent is not null)
        {
            lines.Add($"{PaneTitle}Selected Urgent Item{Reset}");
            lines.AddRange(RendererTextWrap.WrapText($"{Red}{data.SelectedUrgent.Text}{Reset}", width, returnEmptyLineWhenBlank: true));
            if (!string.IsNullOrWhiteSpace(data.SelectedUrgent.Cause))
            {
                lines.Add($"{Dim}{new string('-', width)}{Reset}");
                lines.AddRange(RendererTextWrap.WrapText($"Cause: {data.SelectedUrgent.Cause}", width, returnEmptyLineWhenBlank: true));
            }

            if (!string.IsNullOrWhiteSpace(data.SelectedUrgent.Impact))
            {
                lines.AddRange(RendererTextWrap.WrapText($"Why it matters: {data.SelectedUrgent.Impact}", width, returnEmptyLineWhenBlank: true));
            }

            if (data.SelectedUrgent.TargetScreen == PlayerScreen.Laws)
            {
                lines.Add($"{Dim}{new string('-', width)}{Reset}");
                lines.AddRange(RendererTextWrap.WrapText($"Enter opens Laws and selects the pending decision.", width, returnEmptyLineWhenBlank: true));
            }
        }
        else if (data.SelectedEntry is not null)
        {
            lines.Add($"{PaneTitle}Selected Entry{Reset}");
            lines.AddRange(RendererTextWrap.WrapText(RenderTokens(data.SelectedEntry.HeadlineTokens), width, returnEmptyLineWhenBlank: true));
            lines.AddRange(RendererTextWrap.WrapText($"{Dim}{data.SelectedEntry.DateText}{Reset}  {Purple}{data.SelectedEntry.Category}{Reset}", width, returnEmptyLineWhenBlank: true));
            lines.Add($"{Dim}{new string('-', width)}{Reset}");
            lines.AddRange(RendererTextWrap.WrapText(data.SelectedEntry.Impact, width, returnEmptyLineWhenBlank: true));
        }
        else
        {
            lines.Add($"{PaneTitle}Selected Entry{Reset}");
            lines.AddRange(RendererTextWrap.WrapText($"{Dim}Nothing is selected yet.{Reset}", width, returnEmptyLineWhenBlank: true));
        }

        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Current State{Reset}");
        foreach (var line in data.ConditionSummary)
        {
            lines.AddRange(RendererTextWrap.WrapText($"{Yellow}* {Reset}{line}", width, returnEmptyLineWhenBlank: true));
        }

        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Mode Notes{Reset}");
        foreach (var line in data.ModeNotes)
        {
            lines.AddRange(RendererTextWrap.WrapText($"{Dim}* {line}{Reset}", width, returnEmptyLineWhenBlank: true));
        }

        while (lines.Count < height)
        {
            lines.Add(string.Empty);
        }

        return lines.Take(height).ToArray();
    }

    private static int ResolveSelectedEntryIndex(ChronicleViewModel data)
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

    private static string RenderTokens(IReadOnlyList<ChronicleTextToken> tokens)
    {
        if (tokens.Count == 0)
        {
            return string.Empty;
        }

        return string.Concat(tokens.Select(RenderToken));
    }

    private static string RenderToken(ChronicleTextToken token)
    {
        var color = token.Type switch
        {
            ChronicleTextTokenType.Polity => Blue,
            ChronicleTextTokenType.Region => Teal,
            ChronicleTextTokenType.Discovery => Purple,
            ChronicleTextTokenType.Advancement => Yellow,
            ChronicleTextTokenType.Sapient => Cyan,
            ChronicleTextTokenType.Settlement => Green,
            ChronicleTextTokenType.GovernmentForm => Orange,
            ChronicleTextTokenType.PositiveKeyword => Green,
            ChronicleTextTokenType.NegativeKeyword => Red,
            _ => string.Empty
        };

        return string.IsNullOrEmpty(color)
            ? token.Text
            : $"{color}{token.Text}{Reset}";
    }

}
