using System.Text;
using Species.Domain.Enums;
using Species.Domain.Models;
using Species.Client.DataBuilders;
using Species.Client.Presentation;

namespace Species.Client.Renderers;

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
    private const string HighlightBackground = "\u001b[48;5;236m";

    public static string Render(
        World world,
        string focalPolityId,
        int selectedIndex,
        bool isSimulationRunning,
        bool isLawActionMenuOpen,
        int selectedActionIndex,
        TerminalViewport viewport)
    {
        var data = LawsScreenDataBuilder.Build(world, focalPolityId, selectedIndex);
        var innerWidth = Math.Max(82, viewport.Width - 4);
        var leftWidth = Math.Max(34, ((innerWidth - 3) * 9) / 20);
        var rightWidth = Math.Max(30, innerWidth - leftWidth - 3);
        var bodyHeight = Math.Max(18, viewport.Height - 7);

        var listLines = BuildLawList(data, leftWidth);
        var detailLines = BuildLawDetail(data, rightWidth, isSimulationRunning, isLawActionMenuOpen, selectedActionIndex);
        var lines = new List<string>();
        lines.AddRange(PlayerScreenShell.BuildHeader("Laws", data.PolityName, data.CurrentDate, isSimulationRunning, innerWidth));

        var topRows = Math.Max(listLines.Count, detailLines.Count);
        for (var row = 0; row < topRows; row++)
        {
            var left = row < listLines.Count ? listLines[row] : string.Empty;
            var right = row < detailLines.Count ? detailLines[row] : string.Empty;
            lines.Add(BorderLine($"{FitVisible(left, leftWidth)} | {FitVisible(right, rightWidth)}", innerWidth));
        }

        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        lines.Add(BorderLine($"{PaneTitle}Governance Condition{Reset}", innerWidth));
        foreach (var line in BuildBulletLines(data.GovernanceSummary, innerWidth, Blue))
        {
            lines.Add(BorderLine(line, innerWidth));
        }

        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        lines.Add(BorderLine($"{PaneTitle}Active Laws{Reset}", innerWidth));
        foreach (var line in BuildEnactedLawLines(data.EnactedLaws, innerWidth))
        {
            lines.Add(BorderLine(line, innerWidth));
        }

        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        lines.Add(PlayerScreenShell.BuildFooter(
            innerWidth,
            BuildFullFooter(isSimulationRunning, data.HasSelectedPendingDecision, isLawActionMenuOpen),
            BuildMediumFooter(isSimulationRunning, data.HasSelectedPendingDecision, isLawActionMenuOpen),
            ["Tab: Screens", "Up/Down: Navigate", "Enter: Select"]));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));

        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> BuildFullFooter(bool isSimulationRunning, bool hasSelectedPendingDecision, bool isLawActionMenuOpen)
    {
        if (hasSelectedPendingDecision && !isSimulationRunning && isLawActionMenuOpen)
        {
            return ["Tab: Screens", "Up/Down: Navigate", "Enter: Confirm", "Space: Pause/Run", "N: Next Tick"];
        }

        return ["Tab: Screens", "Up/Down: Navigate", "Enter: Select", "Space: Pause/Run", "N: Next Tick"];
    }

    private static IReadOnlyList<string> BuildMediumFooter(bool isSimulationRunning, bool hasSelectedPendingDecision, bool isLawActionMenuOpen)
    {
        if (hasSelectedPendingDecision && !isSimulationRunning && isLawActionMenuOpen)
        {
            return ["Tab: Screens", "Up/Down: Navigate", "Enter: Confirm", "Space: Pause/Run"];
        }

        return ["Tab: Screens", "Up/Down: Navigate", "Enter: Select", "Space: Pause/Run"];
    }

    private static IReadOnlyList<string> BuildLawList(LawsScreenData data, int width)
    {
        var lines = new List<string>
        {
            $"{PaneTitle}Pending Decisions{Reset}"
        };

        if (data.PendingDecisions.Count == 0)
        {
            lines.Add(FitVisible($"{Dim}No pending decision is waiting right now.{Reset}", width));
        }
        else
        {
            foreach (var law in data.PendingDecisions)
            {
                var selected = data.SelectedLaw is not null && string.Equals(data.SelectedLaw.Id, law.Id, StringComparison.Ordinal);
                var row = $"{ColorProposalStatus(law.Status)} {Blue}{law.Name}{Reset} {Dim}[{law.Category}]{Reset}";
                lines.Add(selected
                    ? $"{HighlightBackground}{Yellow}> {Reset}{HighlightBackground}{FitVisible(row, Math.Max(0, width - 2))}{Reset}"
                    : FitVisible($"  {row}", width));
            }
        }

        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}Recent Outcomes{Reset}");

        if (data.RecentDecisions.Count == 0)
        {
            lines.Add(FitVisible($"{Dim}No recent law outcomes are visible.{Reset}", width));
        }
        else
        {
            foreach (var law in data.RecentDecisions)
            {
                var selected = data.SelectedLaw is not null && string.Equals(data.SelectedLaw.Id, law.Id, StringComparison.Ordinal);
                var row = $"{ColorProposalStatus(law.Status)} {Blue}{law.Name}{Reset} {Dim}[{law.Category}]{Reset}";
                lines.Add(selected
                    ? $"{HighlightBackground}{Yellow}> {Reset}{HighlightBackground}{FitVisible(row, Math.Max(0, width - 2))}{Reset}"
                    : FitVisible($"  {row}", width));
            }
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildLawDetail(
        LawsScreenData data,
        int width,
        bool isSimulationRunning,
        bool isLawActionMenuOpen,
        int selectedActionIndex)
    {
        if (data.SelectedLaw is null)
        {
            return
            [
                $"{PaneTitle}Selected Law{Reset}",
                $"{Dim}When pressure and politics align, a pending decision appears here.{Reset}"
            ];
        }

        var law = data.SelectedLaw;
        var lines = new List<string>
        {
            $"{PaneTitle}Selected Law{Reset}",
            $"{Blue}{law.Name}{Reset}",
            $"Status: {ColorProposalStatus(law.Status)}",
            $"Category: {Purple}{law.Category}{Reset}",
            $"Backed By: {Yellow}{law.BackedBy}{Reset}",
            $"Support: {Green}{law.Support}{Reset}",
            $"Opposition: {Red}{law.Opposition}{Reset}",
            $"Urgency: {Orange}{law.Urgency}{Reset}",
            $"Age: {law.AgeInMonths} months",
            $"{Dim}{new string('-', width)}{Reset}"
        };

        lines.AddRange(WrapText(law.Summary, width));

        if (!string.IsNullOrWhiteSpace(law.ReasonSummary))
        {
            lines.AddRange(WrapText($"Why now: {law.ReasonSummary}", width));
        }

        if (!string.IsNullOrWhiteSpace(law.TradeoffSummary))
        {
            lines.AddRange(WrapText($"Tradeoff: {law.TradeoffSummary}", width));
        }

        if (law.Status == LawProposalStatus.Active)
        {
            lines.Add($"{Dim}{new string('-', width)}{Reset}");
            lines.Add($"{PaneTitle}Decision Actions{Reset}");
            if (isSimulationRunning)
            {
                lines.AddRange(WrapText("Pause the simulation to Pass or Veto this proposal.", width));
            }
            else if (!isLawActionMenuOpen)
            {
                lines.AddRange(WrapText("Press Enter to open the Pass / Veto decision.", width));
            }
            else
            {
                lines.Add(RenderActionRow("Pass", selectedActionIndex == 0, width));
                lines.Add(RenderActionRow("Veto", selectedActionIndex == 1, width));
                lines.AddRange(WrapText("Enter confirms the highlighted action.", width));
            }
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildEnactedLawLines(IReadOnlyList<EnactedLawScreenItem> laws, int width)
    {
        if (laws.Count == 0)
        {
            return BuildBulletLines(["No active laws are currently holding the polity together."], width, Dim);
        }

        var lines = new List<string>();
        foreach (var law in laws)
        {
            lines.AddRange(BuildBulletLines(
                [
                    $"{law.Name} [{law.State}]",
                    law.IntentSummary,
                    $"Core {law.CoreEffectiveness}; Periphery {law.PeripheralEffectiveness}; Compliance {law.Compliance}.",
                    law.TradeoffSummary
                ],
                width,
                ResolveLawStateColor(law.State)));
        }

        return lines;
    }

    private static string RenderActionRow(string label, bool selected, int width)
    {
        var row = label switch
        {
            "Pass" => $"{Green}{label}{Reset}",
            _ => $"{Red}{label}{Reset}"
        };

        return selected
            ? $"{HighlightBackground}{Yellow}> {Reset}{HighlightBackground}{FitVisible(row, Math.Max(0, width - 2))}{Reset}"
            : FitVisible($"  {row}", width);
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

    private static string ColorProposalStatus(LawProposalStatus status)
    {
        return status switch
        {
            LawProposalStatus.Active => $"{Yellow}Pending{Reset}",
            LawProposalStatus.Passed => $"{Green}Passed{Reset}",
            LawProposalStatus.Vetoed => $"{Red}Vetoed{Reset}",
            LawProposalStatus.Abstained => $"{Dim}Faded{Reset}",
            _ => $"{Dim}Unknown{Reset}"
        };
    }

    private static string ResolveLawStateColor(string state)
    {
        return state switch
        {
            "Active" => Green,
            "Under Pressure" => Yellow,
            "Contested" => Orange,
            "Failing" => Red,
            _ => Dim
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
