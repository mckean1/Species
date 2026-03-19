using System.Text;
using Species.Domain.Enums;
using Species.Domain.Models;

public static class ChronicleScreenRenderer
{
    private const string Reset = "\u001b[0m";
    private const string Dim = "\u001b[38;5;245m";
    private const string PaneTitle = "\u001b[38;5;222m";
    private const string White = "\u001b[97m";
    private const string Blue = "\u001b[38;5;111m";
    private const string Cyan = "\u001b[38;5;117m";
    private const string Purple = "\u001b[38;5;141m";
    private const string Orange = "\u001b[38;5;215m";
    private const string Yellow = "\u001b[38;5;221m";
    private const string Green = "\u001b[38;5;114m";
    private const string Red = "\u001b[38;5;210m";
    private const string HighlightBackground = "\u001b[48;5;236m";

    private static readonly string[] MonthNames =
    [
        "Jan", "Feb", "Mar", "Apr", "May", "Jun",
        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    ];

    public static string Render(World world, bool isSimulationRunning, TerminalViewport viewport)
    {
        var layout = ChronicleLayout.Create(viewport);
        var focusGroup = GetFocusGroup(world);
        var recordsLines = BuildRecordsPane(world, layout.RecordsContentWidth, layout.BodyHeight);
        var situationLines = BuildSituationPane(focusGroup, layout.SituationContentWidth, layout.BodyHeight, isSimulationRunning);
        var lines = new List<string>(layout.TotalHeight)
        {
            HorizontalBorder(layout.InnerWidth),
            BuildTopBar(world, layout.InnerWidth),
            HorizontalBorder(layout.InnerWidth),
            CombinePaneHeaders(world, layout.RecordsWidth, layout.SituationWidth)
        };

        for (var row = 0; row < layout.BodyHeight; row++)
        {
            var recordsLine = row < recordsLines.Count ? recordsLines[row] : string.Empty;
            var situationLine = row < situationLines.Count ? situationLines[row] : string.Empty;
            lines.Add(CombinePaneBodyRow(recordsLine, situationLine, layout.RecordsWidth, layout.SituationWidth));
        }

        lines.Add(HorizontalBorder(layout.InnerWidth));
        lines.Add(BuildFooter(layout.InnerWidth));
        lines.Add(HorizontalBorder(layout.InnerWidth));

        return string.Join(Environment.NewLine, lines);
    }

    private static PopulationGroup? GetFocusGroup(World world)
    {
        var latestEntry = world.Chronicle.GetVisibleFeedEntries().FirstOrDefault();
        if (latestEntry is not null)
        {
            var matchingGroup = world.PopulationGroups.FirstOrDefault(group =>
                string.Equals(group.Id, latestEntry.GroupId, StringComparison.Ordinal));

            if (matchingGroup is not null)
            {
                return matchingGroup;
            }
        }

        return world.PopulationGroups
            .OrderByDescending(group => group.Population)
            .ThenBy(group => group.Name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static List<string> BuildRecordsPane(World world, int contentWidth, int availableHeight)
    {
        var lines = new List<string>(availableHeight);
        if (availableHeight <= 0 || contentWidth <= 0)
        {
            return lines;
        }

        var visibleEntries = world.Chronicle.GetVisibleFeedEntries();
        if (visibleEntries.Count == 0)
        {
            AppendWrappedText(lines, availableHeight, contentWidth, "No visible records yet.", Dim);
            AppendWrappedText(lines, availableHeight, contentWidth, "Advance time to let history unfold.", Dim);
            return lines;
        }

        var isTruncated = false;

        for (var index = 0; index < visibleEntries.Count; index++)
        {
            if (index > 0 && visibleEntries[index - 1].EventYear != visibleEntries[index].EventYear)
            {
                if (!TryAddLine(lines, PadVisible($"{Dim}{new string('-', contentWidth)}{Reset}", contentWidth), availableHeight))
                {
                    isTruncated = true;
                    break;
                }
            }

            foreach (var line in BuildRecordLines(visibleEntries[index], index == 0, contentWidth))
            {
                if (TryAddLine(lines, line, availableHeight))
                {
                    continue;
                }

                isTruncated = true;
                break;
            }

            if (isTruncated)
            {
                break;
            }
        }

        if (isTruncated && lines.Count > 0)
        {
            lines[^1] = PadVisible($"{Dim}... older records continue ...{Reset}", contentWidth);
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildRecordLines(ChronicleEntry entry, bool isNewest, int contentWidth)
    {
        var dateText = $"{FormatMonthYear(entry.EventMonth, entry.EventYear)}  ";
        var firstPrefix = isNewest
            ? new List<Segment>
            {
                new("| ", Green),
                new("NEW ", Green),
                new(dateText, Dim)
            }
            : new List<Segment>
            {
                new("  "),
                new(dateText, Dim)
            };
        var continuationPrefix = new List<Segment>
        {
            new(new string(' ', GetVisibleLength(firstPrefix)))
        };

        return WrapSegments(
            BuildMessageSegments(entry),
            firstPrefix,
            continuationPrefix,
            contentWidth,
            isNewest ? HighlightBackground : null);
    }

    private static IReadOnlyList<Segment> BuildMessageSegments(ChronicleEntry entry)
    {
        return entry.Category switch
        {
            ChronicleEventCategory.Migration => BuildMigrationSegments(entry),
            ChronicleEventCategory.Discovery => BuildDiscoverySegments(entry, " discovered "),
            ChronicleEventCategory.Advancement => BuildDiscoverySegments(entry, " learned "),
            ChronicleEventCategory.Shortage => BuildSimpleSegments(entry.GroupName, " suffered food shortages.", Orange),
            ChronicleEventCategory.Decline => BuildSimpleSegments(entry.GroupName, " declined after hunger and loss.", Orange),
            ChronicleEventCategory.Extinction => BuildSimpleSegments(entry.GroupName, " died out.", Orange),
            _ => [new Segment(entry.Message)]
        };
    }

    private static IReadOnlyList<Segment> BuildMigrationSegments(ChronicleEntry entry)
    {
        var prefix = $"{entry.GroupName} migrated to ";
        var suffix = ".";
        var regionName = entry.Message.StartsWith(prefix, StringComparison.Ordinal) && entry.Message.EndsWith(suffix, StringComparison.Ordinal)
            ? entry.Message[prefix.Length..^suffix.Length]
            : entry.Message;

        return
        [
            new Segment(entry.GroupName, Blue),
            new Segment(" migrated to "),
            new Segment(regionName, Cyan),
            new Segment(".")
        ];
    }

    private static IReadOnlyList<Segment> BuildDiscoverySegments(ChronicleEntry entry, string connector)
    {
        var prefix = entry.GroupName + connector;
        var suffix = ".";
        var itemName = entry.Message.StartsWith(prefix, StringComparison.Ordinal) && entry.Message.EndsWith(suffix, StringComparison.Ordinal)
            ? entry.Message[prefix.Length..^suffix.Length]
            : entry.Message;

        return
        [
            new Segment(entry.GroupName, Blue),
            new Segment(connector),
            new Segment(itemName, Purple),
            new Segment(".")
        ];
    }

    private static IReadOnlyList<Segment> BuildSimpleSegments(string groupName, string trailingText, string trailingColor)
    {
        return
        [
            new Segment(groupName, Blue),
            new Segment(trailingText, trailingColor)
        ];
    }

    private static List<string> BuildSituationPane(
        PopulationGroup? focusGroup,
        int contentWidth,
        int availableHeight,
        bool isSimulationRunning)
    {
        var lines = new List<string>(availableHeight);
        if (availableHeight <= 0 || contentWidth <= 0)
        {
            return lines;
        }

        var stateColor = isSimulationRunning ? Green : Yellow;
        var stateText = isSimulationRunning ? "Running" : "Paused";

        TryAddLine(lines, PadVisible($"{Dim}Status:{Reset} {stateColor}{stateText}{Reset}", contentWidth), availableHeight);

        if (focusGroup is null)
        {
            TryAddLine(lines, string.Empty, availableHeight);
            TryAddLine(lines, PadVisible($"{PaneTitle}Pressure State{Reset}", contentWidth), availableHeight);
            AppendWrappedText(lines, availableHeight, contentWidth, "No polity is active yet.", Dim);
            TryAddLine(lines, string.Empty, availableHeight);
            TryAddLine(lines, PadVisible($"{PaneTitle}Top Alerts{Reset}", contentWidth), availableHeight);
            AppendWrappedText(lines, availableHeight, contentWidth, "No alerts yet.", Dim);
            return lines;
        }

        TryAddLine(lines, PadVisible($"{Dim}Polity:{Reset} {Blue}{focusGroup.Name}{Reset}", contentWidth), availableHeight);
        TryAddLine(lines, string.Empty, availableHeight);
        TryAddLine(lines, PadVisible($"{PaneTitle}Pressure State{Reset}", contentWidth), availableHeight);

        foreach (var line in BuildPressureLines(focusGroup, contentWidth))
        {
            if (!TryAddLine(lines, line, availableHeight))
            {
                return lines;
            }
        }

        if (TryAddLine(lines, string.Empty, availableHeight))
        {
            TryAddLine(lines, PadVisible($"{PaneTitle}Top Alerts{Reset}", contentWidth), availableHeight);
        }

        foreach (var line in BuildAlertLines(focusGroup, contentWidth))
        {
            if (!TryAddLine(lines, line, availableHeight))
            {
                break;
            }
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildPressureLines(PopulationGroup focusGroup, int contentWidth)
    {
        var pressureRows = new (string Label, int Value)[]
        {
            ("Food", focusGroup.Pressures.FoodPressure),
            ("Water", focusGroup.Pressures.WaterPressure),
            ("Threat", focusGroup.Pressures.ThreatPressure),
            ("Crowding", focusGroup.Pressures.OvercrowdingPressure),
            ("Migration", focusGroup.Pressures.MigrationPressure)
        };
        var lines = new List<string>(pressureRows.Length * 2);
        var labelWidth = Math.Min(9, pressureRows.Max(row => row.Label.Length));
        var valueWidth = 3;
        var singleLineBarWidth = contentWidth - labelWidth - valueWidth - 5;

        foreach (var row in pressureRows)
        {
            var valueText = row.Value.ToString().PadLeft(valueWidth, ' ');
            var barColor = GetSeverityColor(row.Value);

            if (singleLineBarWidth >= 10)
            {
                var bar = BuildPressureBar(row.Value, singleLineBarWidth, barColor);
                lines.Add(PadVisible($"{row.Label.PadRight(labelWidth)} [{bar}] {valueText}", contentWidth));
                continue;
            }

            var compactBarWidth = Math.Max(6, contentWidth - valueWidth - 3);
            lines.Add(PadVisible($"{row.Label} {valueText}", contentWidth));
            lines.Add(PadVisible($"[{BuildPressureBar(row.Value, compactBarWidth, barColor)}]", contentWidth));
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildAlertLines(PopulationGroup focusGroup, int contentWidth)
    {
        var alerts = BuildAlerts(focusGroup).Take(3).ToArray();
        if (alerts.Length == 0)
        {
            return [PadVisible($"{Dim}No urgent developments.{Reset}", contentWidth)];
        }

        var lines = new List<string>();
        foreach (var alert in alerts)
        {
            lines.AddRange(WrapPlainText(alert.Text, alert.Color, contentWidth, "* ", "  "));
        }

        return lines;
    }

    private static IReadOnlyList<(string Text, string Color)> BuildAlerts(PopulationGroup focusGroup)
    {
        var alertCandidates = new List<(string Text, string Color, int Severity)>
        {
            BuildAlert("Food strain is worsening", focusGroup.Pressures.FoodPressure),
            BuildAlert("Water access is under pressure", focusGroup.Pressures.WaterPressure),
            BuildAlert("Threats near the polity are rising", focusGroup.Pressures.ThreatPressure),
            BuildAlert("Crowding is tightening around the core", focusGroup.Pressures.OvercrowdingPressure),
            BuildAlert("Movement toward safer ground is increasing", focusGroup.Pressures.MigrationPressure)
        };

        return alertCandidates
            .Where(alert => alert.Severity >= 40)
            .OrderByDescending(alert => alert.Severity)
            .Select(alert => (alert.Text, alert.Color))
            .ToArray();
    }

    private static (string Text, string Color, int Severity) BuildAlert(string text, int severity)
    {
        return (text, GetSeverityColor(severity), severity);
    }

    private static string CombinePaneHeaders(World world, int recordsWidth, int situationWidth)
    {
        var left = PadVisible($"{PaneTitle}Records{Reset}", Math.Max(0, recordsWidth - 2));
        var right = PadVisible($"{PaneTitle}Situation{Reset}", Math.Max(0, situationWidth - 2));
        return $"| {left} | {right} |";
    }

    private static string BuildTopBar(World world, int innerWidth)
    {
        return BorderLine(
            PadBetween(
                $"{PaneTitle}Chronicle{Reset}",
                $"{White}{FormatMonthYear(world.CurrentMonth, world.CurrentYear)}{Reset}",
                innerWidth),
            innerWidth);
    }

    private static string CombinePaneBodyRow(string recordsLine, string situationLine, int recordsWidth, int situationWidth)
    {
        return $"| {PadVisible(recordsLine, Math.Max(0, recordsWidth - 2))} | {PadVisible(situationLine, Math.Max(0, situationWidth - 2))} |";
    }

    private static string BuildFooter(int innerWidth)
    {
        IReadOnlyList<Segment> segments = innerWidth switch
        {
            >= 64 =>
            [
                new Segment("[TAB]", Dim),
                new Segment(" Next screen   "),
                new Segment("[SPACE]", Dim),
                new Segment(" Run/Pause   "),
                new Segment("[ESC]", Dim),
                new Segment(" Quit")
            ],
            >= 44 =>
            [
                new Segment("[TAB]", Dim),
                new Segment(" Screen   "),
                new Segment("[SPACE]", Dim),
                new Segment(" Run   "),
                new Segment("[ESC]", Dim),
                new Segment(" Quit")
            ],
            _ =>
            [
                new Segment("[TAB]", Dim),
                new Segment("  "),
                new Segment("[SPACE]", Dim),
                new Segment("  "),
                new Segment("[ESC]", Dim)
            ]
        };

        return BorderLine(RenderSegments(segments, null, innerWidth), innerWidth);
    }

    private static IReadOnlyList<string> WrapSegments(
        IReadOnlyList<Segment> segments,
        IReadOnlyList<Segment> firstPrefix,
        IReadOnlyList<Segment> continuationPrefix,
        int contentWidth,
        string? background)
    {
        var tokens = TokenizeSegments(segments);
        var lines = new List<string>();
        var prefix = firstPrefix;
        var tokenIndex = 0;

        while (tokenIndex < tokens.Count)
        {
            var prefixWidth = GetVisibleLength(prefix);
            var remainingWidth = Math.Max(1, contentWidth - prefixWidth);
            var lineSegments = new List<Segment>(prefix);
            var hasContent = false;

            while (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];
                if (string.IsNullOrWhiteSpace(token.Text))
                {
                    if (!hasContent)
                    {
                        tokenIndex++;
                        continue;
                    }

                    if (remainingWidth < 1)
                    {
                        break;
                    }

                    lineSegments.Add(new Segment(" ", token.Color));
                    remainingWidth--;
                    tokenIndex++;
                    continue;
                }

                if (token.Text.Length <= remainingWidth)
                {
                    lineSegments.Add(token);
                    remainingWidth -= token.Text.Length;
                    hasContent = true;
                    tokenIndex++;
                    continue;
                }

                if (hasContent)
                {
                    break;
                }

                var splitLength = Math.Max(1, remainingWidth);
                lineSegments.Add(new Segment(token.Text[..splitLength], token.Color));
                tokens[tokenIndex] = new Segment(token.Text[splitLength..], token.Color);
                remainingWidth = 0;
                hasContent = true;
                break;
            }

            var styled = RenderSegments(lineSegments, background, contentWidth);
            lines.Add(background is not null
                ? PadHighlighted(styled, contentWidth)
                : PadVisible(styled, contentWidth));
            prefix = continuationPrefix;
        }

        if (lines.Count == 0)
        {
            var styled = RenderSegments(firstPrefix, background, contentWidth);
            lines.Add(background is not null
                ? PadHighlighted(styled, contentWidth)
                : PadVisible(styled, contentWidth));
        }

        return lines;
    }

    private static List<Segment> TokenizeSegments(IReadOnlyList<Segment> segments)
    {
        var tokens = new List<Segment>();

        foreach (var segment in segments)
        {
            var start = 0;
            while (start < segment.Text.Length)
            {
                var isWhitespace = char.IsWhiteSpace(segment.Text[start]);
                var end = start + 1;

                while (end < segment.Text.Length && char.IsWhiteSpace(segment.Text[end]) == isWhitespace)
                {
                    end++;
                }

                tokens.Add(new Segment(segment.Text[start..end], segment.Color));
                start = end;
            }
        }

        return tokens;
    }

    private static IReadOnlyList<string> WrapPlainText(string text, string color, int contentWidth, string firstPrefix, string continuationPrefix)
    {
        var lines = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentPrefix = firstPrefix;
        var currentText = string.Empty;

        foreach (var word in words)
        {
            var remainingWord = word;

            while (remainingWord.Length > 0)
            {
                var availableWidth = Math.Max(1, contentWidth - currentPrefix.Length);
                var candidate = string.IsNullOrEmpty(currentText) ? remainingWord : $"{currentText} {remainingWord}";
                if (candidate.Length <= availableWidth)
                {
                    currentText = candidate;
                    break;
                }

                if (!string.IsNullOrEmpty(currentText))
                {
                    lines.Add(PadVisible($"{color}{currentPrefix}{currentText}{Reset}", contentWidth));
                    currentPrefix = continuationPrefix;
                    currentText = string.Empty;
                    continue;
                }

                lines.Add(PadVisible($"{color}{currentPrefix}{remainingWord[..availableWidth]}{Reset}", contentWidth));
                currentPrefix = continuationPrefix;
                remainingWord = remainingWord[availableWidth..];
            }
        }

        if (!string.IsNullOrEmpty(currentText) || lines.Count == 0)
        {
            lines.Add(PadVisible($"{color}{currentPrefix}{currentText}{Reset}", contentWidth));
        }

        return lines;
    }

    private static void AppendWrappedText(List<string> lines, int maxHeight, int contentWidth, string text, string color)
    {
        foreach (var line in WrapPlainText(text, color, contentWidth, string.Empty, string.Empty))
        {
            if (!TryAddLine(lines, line, maxHeight))
            {
                return;
            }
        }
    }

    private static bool TryAddLine(List<string> lines, string line, int maxHeight)
    {
        if (lines.Count >= maxHeight)
        {
            return false;
        }

        lines.Add(line);
        return true;
    }

    private static string BuildPressureBar(int value, int width, string barColor)
    {
        var filledCount = (int)Math.Round(width * (value / 100.0), MidpointRounding.AwayFromZero);
        var clampedFilledCount = Math.Clamp(filledCount, 0, width);
        var filled = new string('=', clampedFilledCount);
        var empty = new string('-', Math.Max(0, width - clampedFilledCount));
        return $"{barColor}{filled}{Dim}{empty}{Reset}";
    }

    private static string RenderSegments(IReadOnlyList<Segment> segments, string? background, int maxWidth)
    {
        var trimmed = TrimSegments(segments, maxWidth);
        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(background))
        {
            builder.Append(background);
        }

        foreach (var segment in trimmed)
        {
            if (!string.IsNullOrEmpty(segment.Color))
            {
                builder.Append(segment.Color);
            }

            builder.Append(segment.Text);
        }

        if (!string.IsNullOrEmpty(background) || trimmed.Any(segment => !string.IsNullOrEmpty(segment.Color)))
        {
            builder.Append(Reset);
        }

        return builder.ToString();
    }

    private static IReadOnlyList<Segment> TrimSegments(IReadOnlyList<Segment> segments, int maxWidth)
    {
        var totalLength = segments.Sum(segment => segment.Text.Length);
        if (totalLength <= maxWidth)
        {
            return segments;
        }

        var targetWidth = Math.Max(3, maxWidth - 3);
        var visibleCount = 0;
        var trimmed = new List<Segment>();

        foreach (var segment in segments)
        {
            if (visibleCount >= targetWidth)
            {
                break;
            }

            var available = targetWidth - visibleCount;
            if (segment.Text.Length <= available)
            {
                trimmed.Add(segment);
                visibleCount += segment.Text.Length;
                continue;
            }

            trimmed.Add(new Segment(segment.Text[..available], segment.Color));
            visibleCount += available;
            break;
        }

        if (trimmed.Count == 0)
        {
            return [new Segment("...")];
        }

        var last = trimmed[^1];
        trimmed[^1] = new Segment(last.Text + "...", last.Color);
        return trimmed;
    }

    private static string PadVisible(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var visibleLength = VisibleLength(text);
        if (visibleLength >= width)
        {
            return text;
        }

        return text + new string(' ', width - visibleLength);
    }

    private static string PadBetween(string left, string right, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var leftLength = VisibleLength(left);
        var rightLength = VisibleLength(right);
        var spaces = Math.Max(1, width - leftLength - rightLength);
        return left + new string(' ', spaces) + right;
    }

    private static string PadHighlighted(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var visibleLength = VisibleLength(text);
        if (visibleLength >= width)
        {
            return text;
        }

        var padding = new string(' ', width - visibleLength);
        return text.EndsWith(Reset, StringComparison.Ordinal)
            ? text[..^Reset.Length] + padding + Reset
            : text + padding;
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

    private static int GetVisibleLength(IReadOnlyList<Segment> segments)
    {
        return segments.Sum(segment => segment.Text.Length);
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

    private static string FormatMonthYear(int month, int year)
    {
        var monthIndex = Math.Clamp(month, 1, 12) - 1;
        return $"{MonthNames[monthIndex]} {year:D3}";
    }

    private static string HorizontalBorder(int innerWidth)
    {
        return "+" + new string('-', Math.Max(0, innerWidth + 2)) + "+";
    }

    private static string BorderLine(string content, int innerWidth)
    {
        return $"| {PadVisible(content, Math.Max(0, innerWidth))} |";
    }

    private readonly record struct Segment(string Text, string? Color = null);

    private readonly record struct ChronicleLayout(
        int InnerWidth,
        int RecordsWidth,
        int SituationWidth,
        int RecordsContentWidth,
        int SituationContentWidth,
        int BodyHeight,
        int TotalHeight)
    {
        public static ChronicleLayout Create(TerminalViewport viewport)
        {
            var innerWidth = Math.Max(1, viewport.Width - 4);
            var totalHeight = Math.Max(8, viewport.Height);
            var bodyHeight = Math.Max(1, totalHeight - 8);
            var availablePaneWidth = Math.Max(2, innerWidth - 3);
            var minimumRecordsWidth = Math.Min(availablePaneWidth, Math.Max(18, (int)Math.Ceiling(availablePaneWidth * 0.55)));
            var preferredSituationWidth = Math.Clamp((int)Math.Round(availablePaneWidth * 0.31), 22, 42);
            var situationWidth = Math.Min(preferredSituationWidth, Math.Max(1, availablePaneWidth - minimumRecordsWidth));
            var recordsWidth = Math.Max(1, availablePaneWidth - situationWidth);

            if (recordsWidth <= situationWidth && availablePaneWidth >= 3)
            {
                recordsWidth = Math.Max(availablePaneWidth / 2 + 1, recordsWidth);
                recordsWidth = Math.Min(recordsWidth, availablePaneWidth - 1);
                situationWidth = Math.Max(1, availablePaneWidth - recordsWidth);
            }

            return new ChronicleLayout(
                innerWidth,
                recordsWidth,
                situationWidth,
                Math.Max(0, recordsWidth - 2),
                Math.Max(0, situationWidth - 2),
                bodyHeight,
                totalHeight);
        }
    }
}
