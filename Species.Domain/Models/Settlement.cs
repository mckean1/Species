using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class Settlement
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string PolityId { get; init; } = string.Empty;

    public string RegionId { get; set; } = string.Empty;

    public SettlementType Type { get; set; }

    public int FoundedYear { get; set; }

    public int FoundedMonth { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsPrimary { get; set; }

    public int StoredFood { get; set; }

    public Settlement Clone()
    {
        return new Settlement
        {
            Id = Id,
            Name = Name,
            PolityId = PolityId,
            RegionId = RegionId,
            Type = Type,
            FoundedYear = FoundedYear,
            FoundedMonth = FoundedMonth,
            IsActive = IsActive,
            IsPrimary = IsPrimary,
            StoredFood = StoredFood
        };
    }
}
