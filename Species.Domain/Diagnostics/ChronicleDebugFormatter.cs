using Species.Domain.Constants;
using Species.Domain.Models;

namespace Species.Domain.Diagnostics;

public static class ChronicleDebugFormatter
{
    public static string Format(Chronicle chronicle)
    {
        var visibleEntries = chronicle.GetVisibleFeedEntries();
        var pendingEntries = chronicle.GetPendingEntries();
        var lines = new List<string>
        {
            "Chronicle Debug",
            $"Recorded={chronicle.Entries.Count} | Visible={visibleEntries.Count} | Pending={pendingEntries.Count} | RevealDelayMonths={ChronicleConstants.RevealDelayMonths} | MaxRevealPerTick={ChronicleConstants.MaxRevealPerTick}",
            string.Empty,
            "Visible Feed:"
        };

        if (visibleEntries.Count == 0)
        {
            lines.Add("(none)");
        }
        else
        {
            foreach (var entry in visibleEntries)
            {
                lines.Add(
                    $"{entry.Category} | Event=Y{entry.EventYear} M{entry.EventMonth} | Revealed=Y{entry.RevealedYear} M{entry.RevealedMonth} | {entry.Message}");
            }
        }

        lines.Add(string.Empty);
        lines.Add("Pending Entries:");

        if (pendingEntries.Count == 0)
        {
            lines.Add("(none)");
        }
        else
        {
            foreach (var entry in pendingEntries)
            {
                lines.Add(
                    $"{entry.Category} | Event=Y{entry.EventYear} M{entry.EventMonth} | RevealAfter=Y{entry.RevealAfterYear} M{entry.RevealAfterMonth} | {entry.Message}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}
