using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class PoliticalHistoryRecord
{
    public string RelatedPolityId { get; init; } = string.Empty;

    public string RelatedPolityName { get; set; } = string.Empty;

    public PoliticalHistoryKind Kind { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public string Summary { get; set; } = string.Empty;

    public PoliticalHistoryRecord Clone()
    {
        return new PoliticalHistoryRecord
        {
            RelatedPolityId = RelatedPolityId,
            RelatedPolityName = RelatedPolityName,
            Kind = Kind,
            Year = Year,
            Month = Month,
            Summary = Summary
        };
    }
}
