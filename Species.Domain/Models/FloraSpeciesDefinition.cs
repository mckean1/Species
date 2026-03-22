using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class FloraSpeciesDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public SpeciesClass SpeciesClass { get; init; } = SpeciesClass.Flora;

    public IReadOnlyList<Biome> CoreBiomes { get; init; } = Array.Empty<Biome>();

    public IReadOnlyList<WaterAvailability> SupportedWaterAvailabilities { get; init; } = Array.Empty<WaterAvailability>();

    public float HabitatFertilityMin { get; init; }

    public float HabitatFertilityMax { get; init; }

    public float GrowthRate { get; init; }

    public float RecoveryRate { get; init; }

    public float UsableBiomass { get; init; }

    public float ConsumptionResilience { get; init; }

    public float SpreadTendency { get; init; }

    public float RegionalAbundance { get; init; }

    public float Conspicuousness { get; init; }

    public IReadOnlyList<FloraTag> Tags { get; init; } = Array.Empty<FloraTag>();

    public BiologicalTraitProfile BaselineTraits { get; init; } = new();

    public string ParentSpeciesId { get; init; } = string.Empty;

    public string OriginRegionId { get; init; } = string.Empty;

    public bool IsExtinct { get; init; }

    public int? ExtinctOnYear { get; init; }

    public int? ExtinctOnMonth { get; init; }
}
