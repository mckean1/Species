using Species.Client.Presentation;

namespace Species.Client.Renderers;

public static class RendererTextWrap
{
    public static IReadOnlyList<string> WrapText(string text, int width, bool returnEmptyLineWhenBlank = false)
    {
        return Wrap(text, width, string.Empty, string.Empty, returnEmptyLineWhenBlank);
    }

    public static IReadOnlyList<string> WrapBullet(string text, int width, string color, string reset)
    {
        return Wrap(text, width, $"{color}* {reset}", "  ");
    }

    public static IReadOnlyList<string> BuildBulletLines(
        IEnumerable<string> items,
        int width,
        string color,
        string reset,
        string emptyText)
    {
        var lines = new List<string>();
        foreach (var item in items)
        {
            lines.AddRange(WrapBullet(item, width, color, reset));
        }

        return lines.Count > 0
            ? lines
            : [PlayerScreenShell.FitVisible(emptyText, width)];
    }

    private static IReadOnlyList<string> Wrap(
        string text,
        int width,
        string firstPrefix,
        string continuationPrefix,
        bool returnEmptyLineWhenBlank = false)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return returnEmptyLineWhenBlank
                ? [string.Empty]
                : [PlayerScreenShell.FitVisible(firstPrefix, width)];
        }

        var lines = new List<string>();
        var current = firstPrefix;

        foreach (var word in words)
        {
            var candidate = BuildCandidate(current, word);
            if (PlayerScreenShell.VisibleLength(candidate) <= width)
            {
                current = candidate;
                continue;
            }

            lines.Add(PlayerScreenShell.FitVisible(current, width));
            current = string.IsNullOrEmpty(continuationPrefix)
                ? word
                : $"{continuationPrefix}{word}";
        }

        lines.Add(PlayerScreenShell.FitVisible(current, width));
        return lines;
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
}
