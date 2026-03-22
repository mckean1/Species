namespace Species.Client.Presentation;

public static class CompactStatRowLayout
{
    private const int GapWidth = 3;
    private const int MinimumColumnWidth = 22;

    public static IReadOnlyList<string> RenderRows(int width, int preferredColumns, params CompactStatItem[] items)
    {
        if (width <= 0 || items.Length == 0)
        {
            return Array.Empty<string>();
        }

        var columns = ResolveColumnCount(width, Math.Max(1, Math.Min(preferredColumns, items.Length)));
        var rows = new List<string>();

        for (var index = 0; index < items.Length; index += columns)
        {
            var rowItems = items.Skip(index).Take(columns).ToArray();
            var cellWidth = Math.Max(MinimumColumnWidth, (width - ((rowItems.Length - 1) * GapWidth)) / rowItems.Length);
            rows.Add(string.Join(new string(' ', GapWidth), rowItems.Select(item => PlayerScreenShell.FitVisible($"{item.Label}: {item.Value}", cellWidth))));
        }

        return rows;
    }

    private static int ResolveColumnCount(int width, int maxColumns)
    {
        for (var columns = maxColumns; columns >= 1; columns--)
        {
            var cellWidth = (width - ((columns - 1) * GapWidth)) / columns;
            if (cellWidth >= MinimumColumnWidth)
            {
                return columns;
            }
        }

        return 1;
    }
}

public sealed record CompactStatItem(string Label, string Value);
