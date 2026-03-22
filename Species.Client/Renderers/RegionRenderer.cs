using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;

namespace Species.Client.Renderers;

public static class RegionRenderer
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

    public static string Render(RegionsViewModel data, TerminalViewport viewport)
    {
        var innerWidth = Math.Max(76, viewport.Width - 4);
        var topListHeight = Math.Max(8, Math.Min(12, viewport.Height / 3));
        var lowerHeight = Math.Max(12, viewport.Height - topListHeight - 9);
        var leftWidth = Math.Max(34, ((innerWidth - 3) * 11) / 20);
        var rightWidth = Math.Max(24, innerWidth - leftWidth - 3);

        var lines = new List<string>();
        lines.AddRange(PlayerScreenShell.BuildHeader("Regions", data.PolityName, data.CurrentDate, data.IsSimulationRunning, innerWidth));
        lines.Add(PlayerScreenShell.BorderLine(PlayerScreenShell.FitVisible($"{PaneTitle}Known Regions{Reset}", innerWidth), innerWidth));

        var listLines = BuildRegionList(data, innerWidth, topListHeight);
        lines.AddRange(listLines.Select(line => PlayerScreenShell.BorderLine(PlayerScreenShell.FitVisible(line, innerWidth), innerWidth)));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));

        var lowerRows = BuildLowerRows(data.SelectedRegion, leftWidth, rightWidth, lowerHeight);
        lines.AddRange(lowerRows.Select(line => PlayerScreenShell.BorderLine(PlayerScreenShell.FitVisible(line, innerWidth), innerWidth)));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        lines.Add(PlayerScreenShell.BuildFooter(
            innerWidth,
            ["Tab: Screens", "Up/Down: Navigate", "Enter: Select", "Space: Pause/Run", "N: Next Tick"],
            ["Tab: Screens", "Up/Down: Navigate", "Enter: Select", "Space: Pause/Run"],
            ["Tab: Screens", "Up/Down: Navigate", "Enter: Select"]));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));

        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> BuildRegionList(RegionsViewModel data, int innerWidth, int listHeight)
    {
        var rows = new List<string>(listHeight);
        var columnWidths = new[] { 24, 12, 13, 11, 10 };
        rows.Add(
            PlayerScreenShell.FitVisible("Name", columnWidths[0]) + " | " +
            PlayerScreenShell.FitVisible("Biome", columnWidths[1]) + " | " +
            PlayerScreenShell.FitVisible("Knowledge", columnWidths[2]) + " | " +
            PlayerScreenShell.FitVisible("Presence", columnWidths[3]) + " | " +
            PlayerScreenShell.FitVisible("Threat", columnWidths[4]));

        if (data.Regions.Count == 0)
        {
            rows.Add($"{Dim}No known regions yet.{Reset}");
        }
        else
        {
            var visibleCount = Math.Max(1, listHeight - 1);
            var startIndex = Math.Clamp(data.SelectedIndex - (visibleCount / 2), 0, Math.Max(0, data.Regions.Count - visibleCount));
            var endIndex = Math.Min(data.Regions.Count, startIndex + visibleCount);

            for (var index = startIndex; index < endIndex; index++)
            {
                var region = data.Regions[index];
                var familiarity = region.Familiarity;
                var presence = region.PresenceText;
                var row = PlayerScreenShell.FitVisible(region.Name, columnWidths[0]) + " | " +
                          PlayerScreenShell.FitVisible(region.Biome, columnWidths[1]) + " | " +
                          PlayerScreenShell.FitVisible(familiarity, columnWidths[2]) + " | " +
                          PlayerScreenShell.FitVisible(presence, columnWidths[3]) + " | " +
                          PlayerScreenShell.FitVisible(ColorThreat($"{region.ThreatText} {region.ThreatScore:00}", region.ThreatScore), columnWidths[4]);

                if (index == data.SelectedIndex)
                {
                    row = $"{HighlightBackground}{Blue}> {Reset}{HighlightBackground}{row}{Reset}";
                }
                else
                {
                    row = $"  {row}";
                }

                rows.Add(PlayerScreenShell.FitVisible(row, innerWidth));
            }
        }

        while (rows.Count < listHeight)
        {
            rows.Add(string.Empty);
        }

        return rows;
    }

    private static IReadOnlyList<string> BuildLowerRows(RegionSummary? selectedRegion, int leftWidth, int rightWidth, int lowerHeight)
    {
        var leftLines = BuildLeftPanel(selectedRegion, leftWidth);
        var rightLines = BuildRightPanel(selectedRegion, rightWidth);
        var rows = new List<string>(lowerHeight);

        for (var index = 0; index < lowerHeight; index++)
        {
            var left = index < leftLines.Count ? leftLines[index] : string.Empty;
            var right = index < rightLines.Count ? rightLines[index] : string.Empty;
            rows.Add($"{PlayerScreenShell.FitVisible(left, leftWidth)} | {PlayerScreenShell.FitVisible(right, rightWidth)}");
        }

        return rows;
    }

    private static IReadOnlyList<string> BuildLeftPanel(RegionSummary? selectedRegion, int width)
    {
        if (selectedRegion is null)
        {
            return
            [
                $"{PaneTitle}Region Details{Reset}",
                $"{Dim}No region selected.{Reset}"
            ];
        }

        var lines = new List<string>
        {
            $"{PaneTitle}Selected Region{Reset}",
            $"{Blue}{selectedRegion.Name}{Reset}",
            $"Biome: {selectedRegion.Biome}",
            $"Water: {ColorWater(selectedRegion.WaterAvailability)}",
            $"Fertility: {selectedRegion.Fertility}",
            $"Presence: {(selectedRegion.PresencePopulation > 0 ? selectedRegion.PresencePopulation.ToString("N0") : "No notable presence")}",
            $"{Dim}{new string('-', width)}{Reset}",
            $"{PaneTitle}Resources & Features{Reset}"
        };

        lines.AddRange(BuildBulletLines(selectedRegion.Flora.Select(item => $"Flora: {item}"), width, Green));
        lines.AddRange(BuildBulletLines(selectedRegion.Fauna.Select(item => $"{selectedRegion.FaunaLabel}: {item}"), width, Cyan));
        lines.AddRange(BuildBulletLines(selectedRegion.Materials.Select(item => $"Material: {item}"), width, Yellow));
        lines.AddRange(BuildBulletLines(selectedRegion.Biology.Select(item => $"Biology: {item}"), width, Purple));
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Knowledge{Reset}");
        lines.AddRange(BuildBulletLines(selectedRegion.Knowledge, width, Purple));

        return lines;
    }

    private static IReadOnlyList<string> BuildRightPanel(RegionSummary? selectedRegion, int width)
    {
        if (selectedRegion is null)
        {
            return
            [
                $"{PaneTitle}Presence{Reset}",
                $"{Dim}No regional context available.{Reset}"
            ];
        }

        var lines = new List<string>
        {
            $"{PaneTitle}Presence{Reset}",
            $"Threat: {ColorThreat(selectedRegion.ThreatText, selectedRegion.ThreatScore)}",
            $"{Dim}{new string('-', width)}{Reset}",
            $"{PaneTitle}Connection{Reset}"
        };

        lines.AddRange(BuildBulletLines(selectedRegion.Context, width, Yellow));
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Opportunity / Risk{Reset}");
        lines.AddRange(BuildBulletLines(selectedRegion.Opportunities, width, Green));
        lines.AddRange(BuildBulletLines(selectedRegion.Risks, width, Orange));
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Fossils / Deep History{Reset}");
        lines.AddRange(BuildBulletLines(selectedRegion.Fossils, width, Cyan));
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Groups Here{Reset}");
        lines.AddRange(BuildBulletLines(selectedRegion.GroupPresence.Count > 0 ? selectedRegion.GroupPresence : ["No notable presence"], width, Blue));

        return lines;
    }

    private static IReadOnlyList<string> BuildBulletLines(IEnumerable<string> items, int width, string color)
    {
        return RendererTextWrap.BuildBulletLines(items, width, color, Reset, $"{Dim}None{Reset}");
    }

    private static string ColorWater(string waterAvailability)
    {
        return waterAvailability switch
        {
            "High" => $"{Green}{waterAvailability}{Reset}",
            "Medium" => $"{Yellow}{waterAvailability}{Reset}",
            "Low" => $"{Orange}{waterAvailability}{Reset}",
            _ => waterAvailability
        };
    }

    private static string ColorThreat(string threatText, int threatScore)
    {
        var color = threatScore switch
        {
            >= 70 => Red,
            >= 40 => Orange,
            _ => Green
        };

        return $"{color}{threatText}{Reset}";
    }

}
