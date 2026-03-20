using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class PolityRegionalPresence
{
    public string RegionId { get; init; } = string.Empty;

    public PolityPresenceKind Kind { get; set; }

    public int TotalMonthsPresent { get; set; }

    public int ConsecutiveMonthsPresent { get; set; }

    public int ReturnCount { get; set; }

    public int MonthsSinceLastPresence { get; set; }

    public bool IsCurrent { get; set; }

    public PolityRegionalPresence Clone()
    {
        return new PolityRegionalPresence
        {
            RegionId = RegionId,
            Kind = Kind,
            TotalMonthsPresent = TotalMonthsPresent,
            ConsecutiveMonthsPresent = ConsecutiveMonthsPresent,
            ReturnCount = ReturnCount,
            MonthsSinceLastPresence = MonthsSinceLastPresence,
            IsCurrent = IsCurrent
        };
    }
}
