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
}
