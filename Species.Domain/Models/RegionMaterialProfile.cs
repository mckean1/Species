using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class RegionMaterialProfile
{
    public MaterialStockpile Opportunities { get; init; } = new();

    public int ShelterPotential { get; init; }

    public int StoragePotential { get; init; }

    public int ToolPotential { get; init; }

    public int TextilePotential { get; init; }

    public int HidePotential { get; init; }

    public int Get(MaterialResource resource) => Opportunities.Get(resource);

    public RegionMaterialProfile Clone()
    {
        return new RegionMaterialProfile
        {
            Opportunities = Opportunities.Clone(),
            ShelterPotential = ShelterPotential,
            StoragePotential = StoragePotential,
            ToolPotential = ToolPotential,
            TextilePotential = TextilePotential,
            HidePotential = HidePotential
        };
    }
}
