using Species.Domain.Models;
using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModelFactories;
using Species.Client.ViewModels;

namespace Species.Client.Renderers;

public static class PolityRenderer
{
    private const string Reset = "\u001b[0m";
    private const string Dim = "\u001b[38;5;245m";
    private const string PaneTitle = "\u001b[38;5;222m";
    private const string Blue = "\u001b[38;5;111m";
    private const string Orange = "\u001b[38;5;215m";
    private const string Yellow = "\u001b[38;5;221m";
    private const string Green = "\u001b[38;5;114m";
    private const string Red = "\u001b[38;5;210m";

    public static string Render(
        World world,
        string focalPolityId,
        bool isSimulationRunning,
        TerminalViewport viewport)
    {
        var data = PolityViewModelFactory.Build(world, focalPolityId);
        var innerWidth = Math.Max(76, viewport.Width - 4);
        var bodyHeight = Math.Max(16, viewport.Height - 7);
        var bodyLines = BuildBodyLines(data, innerWidth);

        if (bodyLines.Count > bodyHeight)
        {
            bodyLines = bodyLines.Take(bodyHeight).ToList();
            bodyLines[^1] = PlayerScreenShell.FitVisible($"{Dim}... additional overview details truncated ...{Reset}", innerWidth);
        }

        while (bodyLines.Count < bodyHeight)
        {
            bodyLines.Add(string.Empty);
        }

        var lines = new List<string>(bodyHeight + 8);
        lines.AddRange(PlayerScreenShell.BuildHeader("Polity Overview", data.PolityName, data.CurrentDate, isSimulationRunning, innerWidth));
        lines.AddRange(bodyLines.Select(line => PlayerScreenShell.BorderLine(PlayerScreenShell.FitVisible(line, innerWidth), innerWidth)));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        lines.Add(PlayerScreenShell.BuildFooter(
            innerWidth,
            ["Tab: Screens", "Space: Pause/Run", "N: Next Tick"],
            ["Tab: Screens", "Space: Pause/Run"],
            ["Tab: Screens"]));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));

        return string.Join(Environment.NewLine, lines);
    }

    private static List<string> BuildBodyLines(PolityViewModel data, int width)
    {
        var lines = new List<string>();

        lines.Add($"{PaneTitle}Overview{Reset}");
        lines.AddRange(CompactStatRowLayout.RenderRows(
            width,
            2,
            new CompactStatItem($"{Dim}Government Form{Reset}", $"{Blue}{data.GovernmentForm}{Reset}"),
            new CompactStatItem($"{Dim}Population{Reset}", data.Population),
            new CompactStatItem($"{Dim}Settlements{Reset}", data.SettlementCount),
            new CompactStatItem($"{Dim}Food Stores{Reset}", data.FoodStores),
            new CompactStatItem($"{Dim}Pressure State{Reset}", ColorPressureState(data.PressureState))));

        if (data.TopPressures.Count > 0)
        {
            lines.Add(PlayerScreenShell.FitVisible($"{Blue}Top Pressures: {BuildTopPressureSummary(data.TopPressures)}{Reset}", width));
        }

        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Reading{Reset}");
        lines.AddRange(BuildWrappedLines($"Strength: {data.Strength}", width, Green, string.Empty, string.Empty));
        lines.AddRange(BuildWrappedLines($"Concern: {data.Concern}", width, Orange, string.Empty, string.Empty));

        return lines;
    }

    private static IReadOnlyList<string> BuildWrappedLines(string text, int width, string color, string firstPrefix, string continuationPrefix)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return [string.Empty];
        }

        var lines = new List<string>();
        var current = firstPrefix;

        foreach (var word in words)
        {
            var separator = current == firstPrefix || current == continuationPrefix ? string.Empty : " ";
            var candidate = current + separator + word;
            if (PlayerScreenShell.VisibleLength(candidate) <= width)
            {
                current = candidate;
                continue;
            }

            lines.Add(PlayerScreenShell.FitVisible($"{color}{current}{Reset}", width));
            current = continuationPrefix + word;
        }

        lines.Add(PlayerScreenShell.FitVisible($"{color}{current}{Reset}", width));
        return lines;
    }

    private static string BuildTopPressureSummary(IReadOnlyList<PolityPressureItem> pressures)
    {
        return string.Join(", ", pressures.Select(pressure => $"{pressure.Label} {pressure.Value} {pressure.SeverityLabel}"));
    }

    private static string ColorPressureState(string value)
    {
        var color = value switch
        {
            "Stable" => Green,
            "Rising" => Yellow,
            "Severe" => Orange,
            "Critical" => Red,
            _ => Blue
        };

        return $"{color}{value}{Reset}";
    }
}
