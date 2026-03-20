using System.Text;
using Species.Domain.Catalogs;
using Species.Domain.Models;

public static class PolityScreenRenderer
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

    public static string Render(
        World world,
        string focalPolityId,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog,
        bool isSimulationRunning,
        TerminalViewport viewport)
    {
        var data = PolityScreenDataBuilder.Build(world, focalPolityId, discoveryCatalog, advancementCatalog);
        var innerWidth = Math.Max(76, viewport.Width - 4);
        var bodyHeight = Math.Max(16, viewport.Height - 7);
        var leftWidth = Math.Max(34, ((innerWidth - 3) * 11) / 20);
        var rightWidth = Math.Max(24, innerWidth - leftWidth - 3);

        var bodyLines = BuildBodyLines(data, isSimulationRunning, leftWidth, rightWidth);
        if (bodyLines.Count > bodyHeight)
        {
            bodyLines = bodyLines.Take(bodyHeight).ToList();
            bodyLines[^1] = FitVisible($"{Dim}... additional polity details truncated ...{Reset}", innerWidth);
        }

        while (bodyLines.Count < bodyHeight)
        {
            bodyLines.Add(string.Empty);
        }

        var lines = new List<string>(bodyHeight + 8);
        lines.AddRange(PlayerScreenShell.BuildHeader("Polity", data.PolityName, data.CurrentDate, isSimulationRunning, innerWidth));

        lines.AddRange(bodyLines.Select(line => BorderLine(FitVisible(line, innerWidth), innerWidth)));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        lines.Add(PlayerScreenShell.BuildFooter(innerWidth));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));

        return string.Join(Environment.NewLine, lines);
    }

    private static List<string> BuildBodyLines(PolityScreenData data, bool isSimulationRunning, int leftWidth, int rightWidth)
    {
        var lines = new List<string>();
        var runningText = isSimulationRunning ? $"{Green}Running{Reset}" : $"{Yellow}Paused{Reset}";

        lines.Add($"{Blue}{data.PolityName}{Reset}");
        lines.Add($"Government Form: {data.GovernmentForm}");
        lines.Add($"Anchoring: {data.Anchoring}");
        lines.Add($"Species: {data.Species}");
        lines.Add($"Home Region: {data.HomeRegion}");
        lines.Add($"Core Region: {data.CoreRegion}");
        lines.Add($"Primary Site: {data.PrimarySite}");
        lines.Add($"Population: {data.Population}");
        lines.Add($"Runtime: {runningText}");
        lines.Add($"{Dim}{new string('-', leftWidth + rightWidth + 3)}{Reset}");

        var pressureLines = BuildPressureLines(data.Pressures, leftWidth);
        var alertLines = BuildBulletLines(data.Alerts, rightWidth, Orange);
        lines.Add(CombinePaneRow($"{PaneTitle}Pressure Summary{Reset}", $"{PaneTitle}Current Issues{Reset}", leftWidth, rightWidth));
        lines.AddRange(CombinePaneRows(pressureLines, alertLines, leftWidth, rightWidth));

        lines.Add($"{Dim}{new string('-', leftWidth + rightWidth + 3)}{Reset}");

        var strengthsProblems = BuildStrengthProblemLines(data, leftWidth);
        var progressLines = BuildBulletLines(data.ProgressItems, rightWidth, Purple);
        lines.Add(CombinePaneRow($"{PaneTitle}Strengths / Problems{Reset}", $"{PaneTitle}Key Discoveries / Progress{Reset}", leftWidth, rightWidth));
        lines.AddRange(CombinePaneRows(strengthsProblems, progressLines, leftWidth, rightWidth));

        lines.Add($"{Dim}{new string('-', leftWidth + rightWidth + 3)}{Reset}");
        lines.Add($"{PaneTitle}Active Laws{Reset}");
        foreach (var law in BuildBulletLines(data.ActiveLaws, leftWidth + rightWidth + 3, Dim))
        {
            lines.Add(law);
        }

        lines.Add($"{Dim}{new string('-', leftWidth + rightWidth + 3)}{Reset}");
        lines.Add($"{PaneTitle}Regional Presence{Reset}");
        foreach (var presence in BuildBulletLines(data.RegionalPresence, leftWidth + rightWidth + 3, Purple))
        {
            lines.Add(presence);
        }

        lines.Add($"{Dim}{new string('-', leftWidth + rightWidth + 3)}{Reset}");
        lines.Add($"{PaneTitle}Political Blocs{Reset}");
        foreach (var bloc in BuildPoliticalBlocLines(data.PoliticalBlocs, leftWidth + rightWidth + 3))
        {
            lines.Add(bloc);
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildPoliticalBlocLines(IReadOnlyList<PoliticalBlocScreenItem> blocs, int width)
    {
        if (blocs.Count == 0)
        {
            return BuildBulletLines(["No political blocs are visible."], width, Dim);
        }

        return BuildBulletLines(
            blocs.Select(bloc => $"{bloc.Name} [Influence {bloc.Influence} | Satisfaction {bloc.Satisfaction}]").ToArray(),
            width,
            Dim);
    }

    private static IReadOnlyList<string> BuildPressureLines(IReadOnlyList<PolityPressureItem> pressures, int width)
    {
        var lines = new List<string>(pressures.Count);
        var labelWidth = Math.Min(9, pressures.Max(item => item.Label.Length));
        var barWidth = Math.Max(8, width - labelWidth - 8);

        foreach (var pressure in pressures)
        {
            var filledCount = Math.Clamp((int)Math.Round(barWidth * (pressure.Value / 100.0), MidpointRounding.AwayFromZero), 0, barWidth);
            var filled = new string('=', filledCount);
            var empty = new string('-', barWidth - filledCount);
            var valueText = pressure.Value.ToString().PadLeft(3);
            var line = $"{pressure.Label.PadRight(labelWidth)}  [{GetSeverityColor(pressure.Value)}{filled}{Dim}{empty}{Reset}] {valueText}";
            lines.Add(FitVisible(line, width));
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildStrengthProblemLines(PolityScreenData data, int width)
    {
        var lines = new List<string>();
        lines.Add($"{Green}Strengths{Reset}");
        lines.AddRange(BuildBulletLines(data.Strengths, width, Green));
        lines.Add(string.Empty);
        lines.Add($"{Orange}Problems{Reset}");
        lines.AddRange(BuildBulletLines(data.Problems, width, Orange));
        return lines;
    }

    private static IReadOnlyList<string> BuildBulletLines(IReadOnlyList<string> items, int width, string color)
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

    private static IReadOnlyList<string> CombinePaneRows(
        IReadOnlyList<string> leftLines,
        IReadOnlyList<string> rightLines,
        int leftWidth,
        int rightWidth)
    {
        var rowCount = Math.Max(leftLines.Count, rightLines.Count);
        var rows = new List<string>(rowCount);

        for (var index = 0; index < rowCount; index++)
        {
            var left = index < leftLines.Count ? leftLines[index] : string.Empty;
            var right = index < rightLines.Count ? rightLines[index] : string.Empty;
            rows.Add(CombinePaneRow(left, right, leftWidth, rightWidth));
        }

        return rows;
    }

    private static string CombinePaneRow(string left, string right, int leftWidth, int rightWidth)
    {
        return $"{FitVisible(left, leftWidth)} | {FitVisible(right, rightWidth)}";
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

        if (visibleLength == width && index < text.Length)
        {
            return builder.ToString();
        }

        if (visibleLength < width)
        {
            builder.Append(' ', width - visibleLength);
        }

        return builder.ToString();
    }

    private static string PadBetween(string left, string right, int width)
    {
        var leftLength = VisibleLength(left);
        var rightLength = VisibleLength(right);
        var spaces = Math.Max(1, width - leftLength - rightLength);
        return left + new string(' ', spaces) + right;
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

    private static string GetSeverityColor(int value)
    {
        return value switch
        {
            >= 80 => Red,
            >= 60 => Orange,
            >= 40 => Yellow,
            _ => Green
        };
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
