using System.Text;
using Species.Domain.Enums;
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
        string focalPolityId,
        int selectedIndex,
        bool isSimulationRunning,
        TerminalViewport viewport)
    {
        var data = LawsScreenDataBuilder.Build(world, focalPolityId, selectedIndex);
        var innerWidth = Math.Max(80, viewport.Width - 4);
        var leftWidth = Math.Max(32, ((innerWidth - 3) * 9) / 20);
        var rightWidth = Math.Max(30, innerWidth - leftWidth - 3);
        var bodyHeight = Math.Max(18, viewport.Height - 7);

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

        var lines = new List<string>();
        lines.AddRange(PlayerScreenShell.BuildHeader("Laws", data.PolityName, data.CurrentDate, isSimulationRunning, innerWidth));
        lines.AddRange(bodyLines.Select(line => BorderLine(FitVisible(line, innerWidth), innerWidth)));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        lines.Add(PlayerScreenShell.BuildFooter(
            innerWidth,
            data.HasActiveProposal && !isSimulationRunning ? "Arrows select | P pass | V veto" : "Arrows select"));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));

        return string.Join(Environment.NewLine, lines);
    }

    private static List<string> BuildBodyLines(LawsScreenData data, int leftWidth, int rightWidth, bool isSimulationRunning)
    {
        var lines = new List<string>();
        var runtimeText = isSimulationRunning ? $"{Green}Running{Reset}" : $"{Yellow}Paused{Reset}";

        lines.Add($"Runtime: {runtimeText}");
        lines.Add($"{Dim}{new string('-', leftWidth + rightWidth + 3)}{Reset}");
        lines.Add(CombinePaneRow($"{PaneTitle}Law Proposals{Reset}", $"{PaneTitle}Selected Proposal{Reset}", leftWidth, rightWidth));
        lines.AddRange(CombinePaneRows(BuildLawList(data, leftWidth), BuildLawDetail(data, rightWidth, isSimulationRunning), leftWidth, rightWidth));
        lines.Add($"{Dim}{new string('-', leftWidth + rightWidth + 3)}{Reset}");
        lines.Add($"{PaneTitle}Enacted Laws{Reset}");

        foreach (var line in BuildEnactedLawLines(data.EnactedLaws, leftWidth + rightWidth + 3))
        {
            lines.Add(line);
        }

        lines.Add($"{Dim}{new string('-', leftWidth + rightWidth + 3)}{Reset}");
        lines.Add($"{PaneTitle}Law Notes{Reset}");

        foreach (var note in BuildBulletLines(data.Laws.Count == 0 ? data.EmptyStateNotes : data.Notes, leftWidth + rightWidth + 3, Dim))
        {
            lines.Add(note);
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildEnactedLawLines(IReadOnlyList<EnactedLawScreenItem> laws, int width)
    {
        if (laws.Count == 0)
        {
            return BuildBulletLines(["No enacted laws are active."], width, Dim);
        }

        var lines = new List<string>();
        foreach (var law in laws)
        {
            lines.AddRange(BuildBulletLines(
                [$"{law.Name} [{law.Category} | impact {law.ImpactScale}]",
                  $"Enforcement: {law.Enforcement} | Compliance: {law.Compliance}",
                  law.Summary],
                width,
                Dim));
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
            var row = $"{marker}{ColorStatus(law.Status)} {Blue}{law.Name}{Reset} {Dim}[{law.Category}]{Reset}";
            lines.Add(FitVisible(row, width));
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildLawDetail(LawsScreenData data, int width, bool isSimulationRunning)
    {
        if (data.SelectedLaw is null)
        {
            return
            [
                $"{Dim}No active law proposal.{Reset}",
                $"{Dim}When current pressures and government form align, a proposal will appear.{Reset}"
            ];
        }

        var law = data.SelectedLaw;
        var lines = new List<string>
        {
            $"{Blue}{law.Name}{Reset}",
            $"Status: {ColorStatus(law.Status)}",
            $"Category: {Purple}{law.Category}{Reset}",
            $"Backed By: {Yellow}{law.BackedBy}{Reset}",
            $"Support: {Green}{law.Support}{Reset}",
            $"Opposition: {Red}{law.Opposition}{Reset}",
            $"Urgency: {Orange}{law.Urgency}{Reset}",
            $"Age: {law.AgeInMonths} months",
            $"Ignored: {law.IgnoredMonths} months",
            $"Impact: {law.ImpactScale}",
            $"{Dim}{new string('-', width)}{Reset}"
        };

        lines.AddRange(WrapText(law.Summary, width));

        if (law.Status == LawProposalStatus.Active)
        {
            lines.Add($"{Dim}{new string('-', width)}{Reset}");
            lines.Add($"{PaneTitle}Actions{Reset}");
            lines.AddRange(BuildBulletLines(
                isSimulationRunning
                    ? ["Pause the simulation to Pass or Veto the active proposal."]
                    : ["Press P to pass this law.", "Press V to veto this law."],
                width,
                law.Status == LawProposalStatus.Active && !isSimulationRunning ? Yellow : Dim));
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildBulletLines(IReadOnlyList<string> items, int width, string color)
    {
        var lines = new List<string>();
        foreach (var item in items)
        {
            lines.AddRange(Wrap(item, width, $"{color}* {Reset}", "  "));
        }

        return lines.Count > 0 ? lines : [FitVisible($"{Dim}None{Reset}", width)];
    }

    private static IReadOnlyList<string> WrapText(string text, int width)
    {
        return Wrap(text, width, string.Empty, string.Empty);
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
            var candidate = string.IsNullOrEmpty(current) || current.EndsWith(' ')
                ? $"{current}{word}"
                : $"{current} {word}";
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

    private static string ColorStatus(LawProposalStatus status)
    {
        return status switch
        {
            LawProposalStatus.Active => $"{Green}Active{Reset}",
            LawProposalStatus.Passed => $"{Green}Passed{Reset}",
            LawProposalStatus.Vetoed => $"{Red}Vetoed{Reset}",
            LawProposalStatus.Abstained => $"{Dim}Abstained{Reset}",
            _ => $"{Dim}Unknown{Reset}"
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

    private static string BorderLine(string content, int innerWidth)
    {
        return $"| {FitVisible(content, innerWidth)} |";
    }
}
