using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class PoliticalAttachment
{
    public string RelatedPolityId { get; init; } = string.Empty;

    public PoliticalAttachmentKind Kind { get; set; }

    public int EstablishedYear { get; set; }

    public int EstablishedMonth { get; set; }

    public int IntegrationDepth { get; set; }

    public int Loyalty { get; set; }

    public bool IsActive { get; set; } = true;

    public string RegionFocusId { get; set; } = string.Empty;

    public PoliticalAttachment Clone()
    {
        return new PoliticalAttachment
        {
            RelatedPolityId = RelatedPolityId,
            Kind = Kind,
            EstablishedYear = EstablishedYear,
            EstablishedMonth = EstablishedMonth,
            IntegrationDepth = IntegrationDepth,
            Loyalty = Loyalty,
            IsActive = IsActive,
            RegionFocusId = RegionFocusId
        };
    }
}
