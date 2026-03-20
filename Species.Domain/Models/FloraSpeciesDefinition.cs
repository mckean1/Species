using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class FloraSpeciesDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<Biome> CoreBiomes { get; init; } = Array.Empty<Biome>();

    public IReadOnlyList<WaterAvailability> SupportedWaterAvailabilities { get; init; } = Array.Empty<WaterAvailability>();

    public float PreferredFertilityMin { get; init; }

    public float PreferredFertilityMax { get; init; }

    public float GrowthRate { get; init; }

    public float FoodValue { get; init; }

    public BiologicalTraitProfile BaselineTraits { get; init; } = new();

    public string ParentSpeciesId { get; init; } = string.Empty;

    public string OriginRegionId { get; init; } = string.Empty;

    public bool IsExtinct { get; init; }

    public int? ExtinctOnYear { get; init; }

    public int? ExtinctOnMonth { get; init; }
}
