using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;
using Species.Domain.Enums;

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
        var innerWidth = Math.Max(96, viewport.Width - 4);
        var listWidth = Math.Max(56, ((innerWidth - 3) * 13) / 20);
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
        var lines = new List<string> { $"{PaneTitle}Known Species{Reset}" };
        var selectedLineIndex = -1;

        foreach (var section in data.Sections)
        {
            if (lines.Count > 1)
            {
                lines.Add(string.Empty);
            }

            lines.Add($"{ColorSection(section.Title)}{section.Title}{Reset}");
            if (section.Species.Count == 0)
            {
                lines.Add($"{Dim}{section.EmptyState}{Reset}");
                continue;
            }

            var widths = ResolveColumnWidths(section.Columns.Count, width);
            lines.Add(RenderTableRow(section.Columns, widths, header: true, isSelected: false));
            lines.Add($"{Dim}{new string('-', Math.Max(12, width - 1))}{Reset}");

            foreach (var species in section.Species)
            {
                var isSelected = IsSelected(species, data.SelectedSpecies);
                if (isSelected)
                {
                    selectedLineIndex = lines.Count;
                }

                lines.Add(RenderTableRow(species.Cells, widths, header: false, isSelected));
            }
        }

        return SliceVisibleWindow(lines, selectedLineIndex, bodyHeight);
    }

    private static IReadOnlyList<string> BuildDetailPanel(
        KnownSpeciesSummary? species,
        int width,
        int bodyHeight,
        bool isSimulationRunning)
    {
        var lines = new List<string> { $"{PaneTitle}Selected Species{Reset}" };

        if (species is null)
        {
            lines.Add($"{Dim}No known species are currently listed.{Reset}");
            while (lines.Count < bodyHeight)
            {
                lines.Add(string.Empty);
            }

            return lines;
        }

        lines.Add($"{ColorSpecies(species.SpeciesClass)}{species.Name}{Reset}");
        lines.Add($"Classification: {species.SpeciesClass}");
        lines.Add($"State: {ColorState(species.StateLabel)}");
        lines.Add($"Runtime: {(isSimulationRunning ? $"{Green}Running{Reset}" : $"{Dim}Paused{Reset}")}");
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.AddRange(RendererTextWrap.WrapText(species.Overview, width));
        lines.Add($"{Dim}{new string('-', width)}{Reset}");
        lines.Add($"{PaneTitle}{BuildDetailHeading(species.SpeciesClass, 0)}{Reset}");
        lines.AddRange(BuildBulletLines(species.Details, width, Blue, "No concrete details recorded."));
        lines.Add($"{PaneTitle}{BuildDetailHeading(species.SpeciesClass, 1)}{Reset}");
        lines.AddRange(BuildBulletLines(species.Traits, width, Green, "No notable traits recorded."));
        lines.Add($"{PaneTitle}{BuildDetailHeading(species.SpeciesClass, 2)}{Reset}");
        lines.AddRange(BuildBulletLines(species.Context, width, species.IsPlayerSpecies ? Orange : Purple, "No additional context recorded."));

        while (lines.Count < bodyHeight)
        {
            lines.Add(string.Empty);
        }

        return lines.Take(bodyHeight).ToArray();
    }

    private static IReadOnlyList<string> SliceVisibleWindow(IReadOnlyList<string> lines, int selectedLineIndex, int bodyHeight)
    {
        if (lines.Count <= bodyHeight)
        {
            return PadLines(lines, bodyHeight);
        }

        var startIndex = selectedLineIndex < 0
            ? 0
            : Math.Clamp(selectedLineIndex - (bodyHeight / 2), 0, Math.Max(0, lines.Count - bodyHeight));
        return lines.Skip(startIndex).Take(bodyHeight).ToArray();
    }

    private static IReadOnlyList<string> PadLines(IReadOnlyList<string> lines, int bodyHeight)
    {
        var padded = lines.ToList();
        while (padded.Count < bodyHeight)
        {
            padded.Add(string.Empty);
        }

        return padded;
    }

    private static int[] ResolveColumnWidths(int columnCount, int width)
    {
        if (columnCount <= 0)
        {
            return [];
        }

        if (columnCount == 4)
        {
            return DistributeWidths(width, [18, 10, 14, 16]);
        }

        return DistributeWidths(width, [18, 10, 14, 12, 14, 12]);
    }

    private static int[] DistributeWidths(int totalWidth, IReadOnlyList<int> baseWidths)
    {
        var separators = Math.Max(0, baseWidths.Count - 1) * 3;
        var available = Math.Max(baseWidths.Count * 6, totalWidth - separators);
        var totalBase = baseWidths.Sum();
        var widths = baseWidths
            .Select(width => Math.Max(6, (int)Math.Floor(available * (width / (double)totalBase))))
            .ToArray();
        var delta = available - widths.Sum();
        var index = 0;
        while (delta > 0)
        {
            widths[index % widths.Length]++;
            delta--;
            index++;
        }

        return widths;
    }

    private static string RenderTableRow(IReadOnlyList<string> cells, IReadOnlyList<int> widths, bool header, bool isSelected)
    {
        var renderedCells = cells
            .Take(widths.Count)
            .Select((cell, index) => PlayerScreenShell.FitVisible(cell, widths[index]))
            .ToArray();
        var row = string.Join($"{Dim} | {Reset}", renderedCells);

        if (header)
        {
            return $"{Dim}{row}{Reset}";
        }

        if (!isSelected)
        {
            return row;
        }

        return $"{HighlightBackground}{Yellow}>{Reset}{HighlightBackground} {PlayerScreenShell.FitVisible(row, Math.Max(0, widths.Sum() + ((widths.Count - 1) * 3)) - 2)}{Reset}";
    }

    private static bool IsSelected(KnownSpeciesSummary species, KnownSpeciesSummary? selectedSpecies)
    {
        return selectedSpecies is not null &&
               species.SpeciesClass == selectedSpecies.SpeciesClass &&
               string.Equals(species.Id, selectedSpecies.Id, StringComparison.Ordinal);
    }

    private static string BuildDetailHeading(SpeciesClass speciesClass, int index)
    {
        return speciesClass switch
        {
            SpeciesClass.Flora => index switch
            {
                0 => "Uses / Habitat",
                1 => "Known Traits",
                _ => "Discovery Context"
            },
            SpeciesClass.Fauna => index switch
            {
                0 => "Role / Habitat",
                1 => "Known Traits",
                _ => "Discovery Context"
            },
            _ => index switch
            {
                0 => "Encounter Notes",
                1 => "Known Traits",
                _ => "Contact Context"
            }
        };
    }

    private static string ColorSection(string title)
    {
        return title switch
        {
            "Flora" => Green,
            "Fauna" => Orange,
            _ => Purple
        };
    }

    private static string ColorSpecies(SpeciesClass speciesClass)
    {
        return speciesClass switch
        {
            SpeciesClass.Flora => Green,
            SpeciesClass.Fauna => Orange,
            _ => Purple
        };
    }

    private static string ColorState(string state)
    {
        if (state.Contains("encounter", StringComparison.OrdinalIgnoreCase))
        {
            return $"{Purple}{state}{Reset}";
        }

        if (state.Contains("discover", StringComparison.OrdinalIgnoreCase) || state.Contains("known", StringComparison.OrdinalIgnoreCase))
        {
            return $"{Green}{state}{Reset}";
        }

        return $"{Yellow}{state}{Reset}";
    }

    private static IReadOnlyList<string> BuildBulletLines(IReadOnlyList<string> items, int width, string color, string emptyText)
    {
        return RendererTextWrap.BuildBulletLines(items, width, color, Reset, $"{Dim}{emptyText}{Reset}");
    }
}
