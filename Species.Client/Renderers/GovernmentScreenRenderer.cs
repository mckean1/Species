using Species.Domain.Models;
using Species.Client.DataBuilders;
using Species.Client.Presentation;

namespace Species.Client.Renderers;

public static class GovernmentScreenRenderer
{
    private const string Reset = "\u001b[0m";
    private const string Dim = "\u001b[38;5;245m";
    private const string PaneTitle = "\u001b[38;5;222m";
    private const string Blue = "\u001b[38;5;111m";
    private const string Yellow = "\u001b[38;5;221m";
    private const string Green = "\u001b[38;5;114m";
    private const string Orange = "\u001b[38;5;215m";
    private const string Red = "\u001b[38;5;210m";

    public static string Render(
        World world,
        string focalPolityId,
        bool isSimulationRunning,
        TerminalViewport viewport)
    {
        var data = GovernmentScreenDataBuilder.Build(world, focalPolityId);
        var innerWidth = Math.Max(76, viewport.Width - 4);
        var bodyHeight = Math.Max(16, viewport.Height - 7);
        var bodyLines = BuildBodyLines(data, innerWidth);

        if (bodyLines.Count > bodyHeight)
        {
            bodyLines = bodyLines.Take(bodyHeight).ToList();
            bodyLines[^1] = PlayerScreenShell.FitVisible($"{Dim}... additional government details truncated ...{Reset}", innerWidth);
        }

        while (bodyLines.Count < bodyHeight)
        {
            bodyLines.Add(string.Empty);
        }

        var lines = new List<string>(bodyHeight + 8);
        lines.AddRange(PlayerScreenShell.BuildHeader("Government", data.PolityName, data.CurrentDate, isSimulationRunning, innerWidth));
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

    private static List<string> BuildBodyLines(GovernmentScreenData data, int width)
    {
        var lines = new List<string>();

        lines.Add($"{PaneTitle}Government{Reset}");
        lines.AddRange(CompactStatRowRenderer.RenderRows(
            width,
            3,
            new CompactStatItem($"{Dim}Form{Reset}", $"{Blue}{data.GovernmentForm}{Reset}"),
            new CompactStatItem($"{Dim}Capital{Reset}", ColorCapital(data.Capital)),
            new CompactStatItem($"{Dim}Founded{Reset}", ColorFounded(data.Founded))));

        return lines;
    }

    private static string ColorCapital(string value)
    {
        return value == "Not Established"
            ? $"{Red}{value}{Reset}"
            : $"{Blue}{value}{Reset}";
    }

    private static string ColorFounded(string value)
    {
        return value == "Not Recorded"
            ? $"{Yellow}{value}{Reset}"
            : $"{Green}{value}{Reset}";
    }
}
