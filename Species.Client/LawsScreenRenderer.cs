using System.Text;
using Species.Domain.Models;

public static class LawsScreenRenderer
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
        string focalGroupId,
        int selectedIndex,
        bool isSimulationRunning,
        TerminalViewport viewport)
    {
        var data = LawsScreenDataBuilder.Build(world, focalGroupId, selectedIndex);
        var innerWidth = Math.Max(80, viewport.Width - 4);
        var leftWidth = Math.Max(32, ((innerWidth - 3) * 9) / 20);
        var rightWidth = Math.Max(28, innerWidth - leftWidth - 3);
        var bodyHeight = Math.Max(18, viewport.Height - 6);

        var bodyLines = BuildBodyLines(data, leftWidth, rightWidth, isSimulationRunning);
        if (bodyLines.Count > bodyHeight)
        {
            bodyLines = bodyLines.Take(bodyHeight).ToList();
            bodyLines[^1] = FitVisible($"{Dim}... additional law details truncated ...{Reset}", innerWidth);
        }

        while (bodyLines.Count < bodyHeight)
        {
            bodyLines.Add(string.Empty);
        }

        var lines = new List<string>
        {
            HorizontalBorder(innerWidth),
            BorderLine(PadBetween($"{PaneTitle}Laws{Reset}", $"{Dim}{data.CurrentDate}{Reset}", innerWidth), innerWidth),
            HorizontalBorder(innerWidth)
        };

        lines.AddRange(bodyLines.Select(line => BorderLine(FitVisible(line, innerWidth), innerWidth)));
        lines.Add(HorizontalBorder(innerWidth));
        lines.Add(BorderLine(PlayerScreenNavigation.BuildFooterText(innerWidth, Dim, Reset, "Select law"), innerWidth));
        lines.Add(HorizontalBorder(innerWidth));

        return string.Join(Environment.NewLine, lines);
    }

    private static List<string> BuildBodyLines(LawsScreenData data, int leftWidth, int rightWidth, bool isSimulationRunning)
    {
        var lines = new List<string>();
        var runtimeText = isSimulationRunning ? $"{Green}Running{Reset}" : $"{Yellow}Paused{Reset}";

        lines.Add($"{Blue}{data.PolityName}{Reset}");
        lines.Add($"Runtime: {runtimeText}");
        lines.Add($"{Dim}{new string('-', leftWidth + rightWidth + 3)}{Reset}");

        var listLines = BuildLawList(data, leftWidth);
        var detailLines = BuildLawDetail(data, rightWidth);
        lines.Add(CombinePaneRow($"{PaneTitle}Laws{Reset}", $"{PaneTitle}Selected Law{Reset}", leftWidth, rightWidth));
        lines.AddRange(CombinePaneRows(listLines, detailLines, leftWidth, rightWidth));

        lines.Add($"{Dim}{new string('-', leftWidth + rightWidth + 3)}{Reset}");
        lines.Add($"{PaneTitle}Law Notes{Reset}");
        foreach (var note in BuildBulletLines(data.DraftNotes, leftWidth + rightWidth + 3, Dim))
        {
            lines.Add(note);
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildLawList(LawsScreenData data, int width)
    {
        if (data.Laws.Count == 0)
        {
            return BuildBulletLines(data.EmptyStateNotes, width, Dim);
        }

        var lines = new List<string>();
        for (var index = 0; index < data.Laws.Count; index++)
        {
            var law = data.Laws[index];
            var marker = index == data.SelectedIndex ? $"{Yellow}> {Reset}" : "  ";
            var row = $"{marker}{ColorStatus(law.Status)} {Blue}{law.Name}{Reset}";

            if (!string.IsNullOrWhiteSpace(law.Category))
            {
                row += $" {Dim}[{law.Category}]{Reset}";
            }

            if (law.IsQuestionable)
            {
                row += $" {Orange}!{Reset}";
            }

            lines.Add(FitVisible(row, width));
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildLawDetail(LawsScreenData data, int width)
    {
        if (data.SelectedLaw is null)
        {
            var lines = new List<string>
            {
                $"{Dim}No laws are currently in force.{Reset}",
                $"{Dim}This polity has not adopted formal laws yet.{Reset}",
                $"{Dim}{new string('-', width)}{Reset}",
                $"{PaneTitle}What To Expect{Reset}"
            };
            lines.AddRange(BuildBulletLines(
                [
                    "Active laws will appear here once the law system provides polity law state.",
                    "Draft or available laws will be listed separately when they exist.",
                    "Benefits and risks will be shown in plain language rather than raw internals."
                ],
                width,
                Dim));
            return lines;
        }

        var law = data.SelectedLaw;
        var lines2 = new List<string>
        {
            $"{Blue}{law.Name}{Reset}",
            $"Status: {ColorStatus(law.Status)}"
        };

        if (!string.IsNullOrWhiteSpace(law.Category))
        {
            lines2.Add($"Category: {Purple}{law.Category}{Reset}");
        }

        lines2.AddRange(WrapText(law.Description, width));
        lines2.Add($"{Dim}{new string('-', width)}{Reset}");
        lines2.Add($"{PaneTitle}Why It Matters{Reset}");
        lines2.AddRange(BuildBulletLines([law.WhyItMatters], width, Yellow));
        lines2.Add($"{PaneTitle}Benefits{Reset}");
        lines2.AddRange(BuildBulletLines(law.Benefits, width, Green));
        lines2.Add($"{PaneTitle}Risks / Tradeoffs{Reset}");
        lines2.AddRange(BuildBulletLines(law.Risks, width, Orange));

        if (law.IsQuestionable)
        {
            lines2.Add($"{PaneTitle}Risk Profile{Reset}");
            lines2.AddRange(BuildBulletLines(["This law carries visible social or moral risk."], width, Red));
        }

        return lines2;
    }

    private static IReadOnlyList<string> BuildBulletLines(IReadOnlyList<string> items, int width, string color)
    {
        var lines = new List<string>();

        foreach (var item in items)
        {
            lines.AddRange(WrapBullet(item, width, color));
        }

        if (lines.Count == 0)
        {
            lines.Add(FitVisible($"{Dim}None{Reset}", width));
        }

        return lines;
    }

    private static IReadOnlyList<string> WrapText(string text, int width)
    {
        return Wrap(text, width, string.Empty, string.Empty);
    }

    private static IReadOnlyList<string> WrapBullet(string text, int width, string color)
    {
        return Wrap(text, width, $"{color}* {Reset}", "  ");
    }

    private static IReadOnlyList<string> Wrap(string text, int width, string firstPrefix, string nextPrefix)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return [FitVisible(firstPrefix, width)];
        }

        var lines = new List<string>();
        var current = firstPrefix;

        foreach (var word in words)
        {
            var candidate = BuildCandidate(current, word);
            if (VisibleLength(candidate) <= width)
            {
                current = candidate;
                continue;
            }

            lines.Add(FitVisible(current, width));
            current = string.IsNullOrEmpty(nextPrefix) ? word : $"{nextPrefix}{word}";
        }

        lines.Add(FitVisible(current, width));
        return lines;
    }

    private static IReadOnlyList<string> CombinePaneRows(
        IReadOnlyList<string> leftLines,
        IReadOnlyList<string> rightLines,
        int leftWidth,
        int rightWidth)
    {
        var count = Math.Max(leftLines.Count, rightLines.Count);
        var rows = new List<string>(count);

        for (var index = 0; index < count; index++)
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

    private static string BuildCandidate(string current, string word)
    {
        if (string.IsNullOrEmpty(current))
        {
            return word;
        }

        return current.EndsWith(' ')
            ? $"{current}{word}"
            : $"{current} {word}";
    }

    private static string ColorStatus(LawScreenStatus status)
    {
        return status switch
        {
            LawScreenStatus.Active => $"{Green}Active{Reset}",
            LawScreenStatus.Draft => $"{Yellow}Draft{Reset}",
            _ => $"{Dim}Inactive{Reset}"
        };
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
