using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;

namespace Species.Client.Renderers;

public static class KnownSpeciesRenderer
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

    public static string Render(KnownSpeciesViewModel data, TerminalViewport viewport)
    {
        var innerWidth = Math.Max(80, viewport.Width - 4);
        var listWidth = Math.Max(34, ((innerWidth - 3) * 9) / 20);
        var detailWidth = Math.Max(28, innerWidth - listWidth - 3);
        var bodyHeight = Math.Max(18, viewport.Height - 7);

        var listLines = BuildSpeciesList(data, listWidth, bodyHeight);
        var detailLines = BuildDetailPanel(data.SelectedSpecies, detailWidth, bodyHeight, data.IsSimulationRunning);

        var lines = new List<string>();
        lines.AddRange(PlayerScreenShell.BuildHeader("Known Species", data.PolityName, data.CurrentDate, data.IsSimulationRunning, innerWidth));

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

    private static IReadOnlyList<string> BuildSpeciesList(KnownSpeciesViewModel data, int width, int bodyHeight)
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
        lines.AddRange(RendererTextWrap.WrapText(species.Overview, width));
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Facts / Range{Reset}");
        lines.AddRange(BuildBulletLines(species.Facts, width, Blue));
        lines.Add($"{PaneTitle}Traits{Reset}");
        lines.AddRange(BuildBulletLines(species.Traits, width, Green));
        lines.Add($"{PaneTitle}Purpose{Reset}");
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
        return RendererTextWrap.BuildBulletLines(items, width, color, Reset, $"{Dim}No notable traits recorded.{Reset}");
    }
}
