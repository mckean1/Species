using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class FaunaSpeciesDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public SpeciesClass SpeciesClass { get; init; } = SpeciesClass.Fauna;

    public IReadOnlyList<Biome> CoreBiomes { get; init; } = Array.Empty<Biome>();

    public IReadOnlyList<WaterAvailability> SupportedWaterAvailabilities { get; init; } = Array.Empty<WaterAvailability>();

    public float HabitatFertilityMin { get; init; }

    public float HabitatFertilityMax { get; init; }

    public DietCategory DietCategory { get; init; }

    public IReadOnlyList<FaunaDietLink> DietLinks { get; init; } = Array.Empty<FaunaDietLink>();

    public float RequiredIntake { get; init; }

    public float ReproductionRate { get; init; }

    public float MortalitySensitivity { get; init; }

    public float Mobility { get; init; }

    public float FeedingEfficiency { get; init; }

    public float PredatorVulnerability { get; init; }

    public float RegionalAbundance { get; init; }

    public float Conspicuousness { get; init; }

    public float FoodYield { get; init; }

    public IReadOnlyList<FaunaTag> Tags { get; init; } = Array.Empty<FaunaTag>();

    public BiologicalTraitProfile BaselineTraits { get; init; } = new();

    public string ParentSpeciesId { get; init; } = string.Empty;

    public string OriginRegionId { get; init; } = string.Empty;

    public bool IsExtinct { get; init; }

    public int? ExtinctOnYear { get; init; }

    public int? ExtinctOnMonth { get; init; }
}
