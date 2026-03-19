using Species.Domain.Constants;
using Species.Domain.Models;

namespace Species.Domain.Diagnostics;

public static class ChronicleFeedFormatter
{
    public static string Format(Chronicle chronicle)
    {
        var lines = new List<string>
        {
            "Chronicle"
        };

        var visibleEntries = chronicle
            .GetVisibleFeedEntries()
            .Take(ChronicleConstants.MaxVisibleFeedEntries)
            .ToArray();

        if (visibleEntries.Length == 0)
        {
            lines.Add("No visible chronicle entries yet.");
            lines.Add("Advance time to let history unfold.");
            return string.Join(Environment.NewLine, lines);
        }

        foreach (var entry in visibleEntries)
        {
            lines.Add($"Year {entry.EventYear}, Month {entry.EventMonth} | {entry.Message}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
