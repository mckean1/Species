using System.Text;
using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModelFactories;
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

    public static string Render(
        World world,
        string focalPolityId,
        int regionIndex,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog,
        bool isSimulationRunning,
        TerminalViewport viewport)
    {
        var data = RegionsViewModelFactory.Build(world, focalPolityId, regionIndex, floraCatalog, faunaCatalog, discoveryCatalog);
        var innerWidth = Math.Max(76, viewport.Width - 4);
        var topListHeight = Math.Max(8, Math.Min(12, viewport.Height / 3));
        var lowerHeight = Math.Max(12, viewport.Height - topListHeight - 9);
        var leftWidth = Math.Max(34, ((innerWidth - 3) * 11) / 20);
        var rightWidth = Math.Max(24, innerWidth - leftWidth - 3);
        var polityName = PlayerScreenShell.ResolvePolityName(world, focalPolityId);

        var lines = new List<string>();
        lines.AddRange(PlayerScreenShell.BuildHeader("Regions", polityName, data.CurrentDate, isSimulationRunning, innerWidth));
        lines.Add(BorderLine($"{PaneTitle}Known Regions{Reset}", innerWidth));

        var listLines = BuildRegionList(data, innerWidth, topListHeight);
        lines.AddRange(listLines.Select(line => BorderLine(line, innerWidth)));
        lines.Add(HorizontalBorder(innerWidth));

        var lowerRows = BuildLowerRows(data.SelectedRegion, leftWidth, rightWidth, lowerHeight);
        lines.AddRange(lowerRows.Select(line => BorderLine(line, innerWidth)));
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
            FitVisible("Name", columnWidths[0]) + " | " +
            FitVisible("Biome", columnWidths[1]) + " | " +
            FitVisible("Knowledge", columnWidths[2]) + " | " +
            FitVisible("Presence", columnWidths[3]) + " | " +
            FitVisible("Threat", columnWidths[4]));

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
                var row = FitVisible(region.Name, columnWidths[0]) + " | " +
                          FitVisible(region.Biome, columnWidths[1]) + " | " +
                          FitVisible(familiarity, columnWidths[2]) + " | " +
                          FitVisible(presence, columnWidths[3]) + " | " +
                          FitVisible(ColorThreat($"{region.ThreatText} {region.ThreatScore:00}", region.ThreatScore), columnWidths[4]);

                if (index == data.SelectedIndex)
                {
                    row = $"{HighlightBackground}{Blue}> {Reset}{HighlightBackground}{row}{Reset}";
                }
                else
                {
                    row = $"  {row}";
                }

                rows.Add(FitVisible(row, innerWidth));
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
            rows.Add($"{FitVisible(left, leftWidth)} | {FitVisible(right, rightWidth)}");
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
        var lines = new List<string>();

        foreach (var item in items)
        {
            var words = item.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = "*";

            foreach (var word in words)
            {
                var candidate = $"{current} {word}";
                if (candidate.Length <= width)
                {
                    current = candidate;
                    continue;
                }

                lines.Add(FitVisible($"{color}{current}{Reset}", width));
                current = $"  {word}";
            }

            lines.Add(FitVisible($"{color}{current}{Reset}", width));
        }

        if (lines.Count == 0)
        {
            lines.Add(FitVisible($"{Dim}None{Reset}", width));
        }

        return lines;
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
