using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;

namespace Species.Client.Renderers;

public static class KnownPolitiesRenderer
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

    public static string Render(KnownPolitiesViewModel data, TerminalViewport viewport)
    {
        var innerWidth = Math.Max(76, viewport.Width - 4);
        var listWidth = Math.Max(34, ((innerWidth - 3) * 11) / 20);
        var detailWidth = Math.Max(24, innerWidth - listWidth - 3);
        var bodyHeight = Math.Max(16, viewport.Height - 7);

        var listLines = BuildPolityList(data, listWidth, bodyHeight);
        var detailLines = BuildDetailPanel(data.SelectedPolity, detailWidth, bodyHeight);

        var lines = new List<string>();
        lines.AddRange(PlayerScreenShell.BuildHeader("Known Polities", data.PolityName, data.CurrentDate, data.IsSimulationRunning, innerWidth));

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

    private static IReadOnlyList<string> BuildPolityList(KnownPolitiesViewModel data, int width, int bodyHeight)
    {
        var lines = new List<string> { $"{PaneTitle}Polities{Reset}" };
        var columnWidths = new[] { 24, 18, 14, 12 };
        lines.Add(
            PlayerScreenShell.FitVisible("Name", columnWidths[0]) + " | " +
            PlayerScreenShell.FitVisible("Region", columnWidths[1]) + " | " +
            PlayerScreenShell.FitVisible("Population", columnWidths[2]) + " | " +
            PlayerScreenShell.FitVisible("Stance", columnWidths[3]));

        if (data.Polities.Count == 0)
        {
            lines.Add($"{Dim}No known polities yet.{Reset}");
        }
        else
        {
            var visibleRows = Math.Max(1, bodyHeight - 2);
            var startIndex = Math.Clamp(data.SelectedIndex - (visibleRows / 2), 0, Math.Max(0, data.Polities.Count - visibleRows));
            var endIndex = Math.Min(data.Polities.Count, startIndex + visibleRows);

            for (var index = startIndex; index < endIndex; index++)
            {
                var polity = data.Polities[index];
                var row = PlayerScreenShell.FitVisible(polity.Name, columnWidths[0]) + " | " +
                          PlayerScreenShell.FitVisible(polity.CoreRegion, columnWidths[1]) + " | " +
                          PlayerScreenShell.FitVisible(polity.Population, columnWidths[2]) + " | " +
                          PlayerScreenShell.FitVisible(ColorRelationship(polity.Relationship), columnWidths[3]);

                row = index == data.SelectedIndex
                    ? $"{HighlightBackground}{Blue}> {Reset}{HighlightBackground}{row}{Reset}"
                    : $"  {row}";

                lines.Add(PlayerScreenShell.FitVisible(row, width));
            }
        }

        while (lines.Count < bodyHeight)
        {
            lines.Add(string.Empty);
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildDetailPanel(KnownPolitySummary? polity, int width, int bodyHeight)
    {
        var lines = new List<string>();

        if (polity is null)
        {
            lines.Add($"{PaneTitle}Polity Details{Reset}");
            lines.Add($"{Dim}No known contact yet.{Reset}");
            while (lines.Count < bodyHeight)
            {
                lines.Add(string.Empty);
            }

            return lines;
        }

        lines.Add($"{PaneTitle}Selected Polity{Reset}");
        lines.Add($"{Blue}{polity.Name}{Reset}");
        lines.Add($"Government: {polity.GovernmentForm}");
        lines.Add($"Core Region: {polity.CoreRegion}");
        lines.Add($"Current Region: {polity.CurrentRegion}");
        lines.Add($"Approx. Population: {polity.Population}");
        lines.Add($"Relationship: {ColorRelationship(polity.Relationship)}");
        lines.Add($"Proximity: {polity.Proximity}");
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Visible Conditions{Reset}");
        lines.AddRange(BuildBulletLines([polity.PressureSummary], width, Yellow));
        lines.AddRange(BuildBulletLines(polity.Traits, width, Green));
        lines.AddRange(BuildBulletLines(polity.Risks, width, Orange));
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Known Notes{Reset}");
        lines.AddRange(BuildBulletLines(polity.Notes, width, Purple));
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Known Laws{Reset}");
        lines.AddRange(BuildBulletLines(polity.KnownLaws, width, Dim));

        while (lines.Count < bodyHeight)
        {
            lines.Add(string.Empty);
        }

        return lines.Take(bodyHeight).ToArray();
    }

    private static IReadOnlyList<string> BuildBulletLines(IEnumerable<string> items, int width, string color)
    {
        return RendererTextWrap.BuildBulletLines(items, width, color, Reset, $"{Dim}None{Reset}");
    }

    private static string ColorRelationship(string relationship)
    {
        var color = relationship switch
        {
            "Cooperative" or "Shared homeland" => Green,
            "Neutral contact" or "Known contact" => Yellow,
            "Wary" or "Cautious" or "Uneasy peace" => Orange,
            "Rival" or "Competing nearby" or "Hostile" or "Raiding conflict" or "Open conflict" => Red,
            _ => Dim
        };

        return $"{color}{relationship}{Reset}";
    }

}
