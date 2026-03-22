using Species.Client.Enums;
using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;

namespace Species.Client.Renderers;

public static class AdvancementsRenderer
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

    public static string Render(AdvancementsViewModel data, TerminalViewport viewport)
    {
        var innerWidth = Math.Max(80, viewport.Width - 4);
        var listWidth = Math.Max(36, ((innerWidth - 3) * 10) / 21);
        var detailWidth = Math.Max(28, innerWidth - listWidth - 3);
        var bodyHeight = Math.Max(18, viewport.Height - 7);

        var listLines = BuildAdvancementList(data, listWidth, bodyHeight);
        var detailLines = BuildDetailPanel(data.SelectedItem, detailWidth, bodyHeight, data.IsSimulationRunning);

        var lines = new List<string>();
        lines.AddRange(PlayerScreenShell.BuildHeader("Advancements", data.PolityName, data.CurrentDate, data.IsSimulationRunning, innerWidth));

        for (var row = 0; row < bodyHeight; row++)
        {
            var left = row < listLines.Count ? listLines[row] : string.Empty;
            var right = row < detailLines.Count ? detailLines[row] : string.Empty;
            lines.Add(PlayerScreenShell.BorderLine(PlayerScreenShell.FitVisible($"{PlayerScreenShell.FitVisible(left, listWidth)} | {PlayerScreenShell.FitVisible(right, detailWidth)}", innerWidth), innerWidth));
        }

        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        lines.Add(PlayerScreenShell.BuildFooter(
            innerWidth,
            ["Tab: Screens", "Up/Down: Navigate", "Enter: Select", "Space: Pause/Run", "N: Next Tick"],
            ["Tab: Screens", "Up/Down: Navigate", "Enter: Select", "Space: Pause/Run"],
            ["Tab: Screens", "Up/Down: Navigate", "Enter: Select"]));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));

        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> BuildAdvancementList(AdvancementsViewModel data, int width, int bodyHeight)
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
                    var selectedRow = PlayerScreenShell.FitVisible(row, Math.Max(0, width - 2));
                    lines.Add($"{HighlightBackground}{Yellow}> {Reset}{HighlightBackground}{selectedRow}{Reset}");
                }
                else
                {
                    lines.Add(PlayerScreenShell.FitVisible($"  {row}", width));
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
        lines.AddRange(RendererTextWrap.WrapText(item.Description, width));
        lines.Add(string.Empty);
        lines.Add($"{PaneTitle}Advancements{Reset}");
        lines.AddRange(BuildBulletLines([item.CapabilitySummary], width, Green));
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Requirements{Reset}");
        lines.AddRange(BuildBulletLines(item.Requirements, width, Dim));
        lines.AddRange(BuildBulletLines(item.Notes, width, item.Status == AdvancementStatus.Locked ? Orange : Green));

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
            AdvancementStatus.Completed => "This capability is already part of the polity's toolkit.",
            AdvancementStatus.Available => "All known conditions are met. This advancement is ready to unlock.",
            _ => item.StatusSummary
        };
    }

    private static string GetStatusColor(AdvancementStatus status)
    {
        return status switch
        {
            AdvancementStatus.Completed => Green,
            AdvancementStatus.Available => Yellow,
            _ => Red
        };
    }

    private static IReadOnlyList<string> BuildBulletLines(IEnumerable<string> items, int width, string color)
    {
        return RendererTextWrap.BuildBulletLines(items, width, color, Reset, $"{Dim}None{Reset}");
    }

    private static string ColorStatusBadge(AdvancementStatus status)
    {
        return status switch
        {
            AdvancementStatus.Completed => $"{Green}[DONE]{Reset}",
            AdvancementStatus.Available => $"{Yellow}[OPEN]{Reset}",
            _ => $"{Red}[LOCK]{Reset}"
        };
    }

    private static string ColorStatusWord(AdvancementStatus status)
    {
        return status switch
        {
            AdvancementStatus.Completed => $"{Green}Completed{Reset}",
            AdvancementStatus.Available => $"{Yellow}Available{Reset}",
            _ => $"{Red}Locked{Reset}"
        };
    }

}
