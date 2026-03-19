using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class Region
{
    public string Id { get; }
    public string Name { get; }
    public double Fertility { get; }
    public Biome Biome { get; }
    public WaterAvailability WaterAvailability { get; }
    public IReadOnlyList<string> NeighborIds { get; }
    public RegionEcosystem Ecosystem { get; }

    public Region(
        string id,
        string name,
        double fertility,
        Biome biome,
        WaterAvailability waterAvailability,
        IReadOnlyList<string> neighborIds,
        RegionEcosystem? ecosystem = null)
    {
        Id = id;
        Name = name;
        Fertility = fertility;
        Biome = biome;
        WaterAvailability = waterAvailability;
        NeighborIds = neighborIds;
        Ecosystem = ecosystem ?? new RegionEcosystem();
    }
}
