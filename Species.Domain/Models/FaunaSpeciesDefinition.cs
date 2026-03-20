using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class FaunaSpeciesDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<Biome> CoreBiomes { get; init; } = Array.Empty<Biome>();

    public IReadOnlyList<WaterAvailability> SupportedWaterAvailabilities { get; init; } = Array.Empty<WaterAvailability>();

    public DietCategory DietCategory { get; init; }

    public float FoodRequirement { get; init; }

    public float ReproductionRate { get; init; }

    public float MigrationTendency { get; init; }

    public float FoodYield { get; init; }

    public BiologicalTraitProfile BaselineTraits { get; init; } = new();

    public string ParentSpeciesId { get; init; } = string.Empty;

    public string OriginRegionId { get; init; } = string.Empty;

    public bool IsExtinct { get; init; }

    public int? ExtinctOnYear { get; init; }

    public int? ExtinctOnMonth { get; init; }
}
