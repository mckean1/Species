using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Catalogs;

public sealed class FloraSpeciesCatalog
{
    private readonly Dictionary<string, FloraSpeciesDefinition> definitionsById;
    private readonly List<FloraSpeciesDefinition> definitions;

    public FloraSpeciesCatalog(IReadOnlyList<FloraSpeciesDefinition> definitions)
    {
        this.definitions = definitions.ToList();
        Definitions = this.definitions;
        definitionsById = this.definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<FloraSpeciesDefinition> Definitions { get; }

    public FloraSpeciesDefinition? GetById(string id)
    {
        return definitionsById.GetValueOrDefault(id);
    }

    public void AddOrReplace(FloraSpeciesDefinition definition)
    {
        definitionsById[definition.Id] = definition;
        var index = definitions.FindIndex(item => string.Equals(item.Id, definition.Id, StringComparison.Ordinal));
        if (index >= 0)
        {
            definitions[index] = definition;
            return;
        }

        definitions.Add(definition);
    }

    public static FloraSpeciesCatalog CreateStarterSet()
    {
        return new FloraSpeciesCatalog(
        [
            new FloraSpeciesDefinition
            {
                Id = "flora-grass",
                Name = "Grass",
                CoreBiomes = [Biome.Plains, Biome.Forest],
                SupportedWaterAvailabilities = [WaterAvailability.Low, WaterAvailability.Medium],
                PreferredFertilityMin = 0.35f,
                PreferredFertilityMax = 0.80f,
                GrowthRate = 0.75f,
                FoodValue = 0.45f,
                BaselineTraits = new BiologicalTraitProfile
                {
                    ColdTolerance = 48,
                    HeatTolerance = 50,
                    DroughtTolerance = 44,
                    Flexibility = 52,
                    BodySize = 40,
                    Reproduction = 62,
                    Mobility = 20,
                    Defense = 32,
                    Resilience = 48
                }
            },
            new FloraSpeciesDefinition
            {
                Id = "flora-shrub",
                Name = "Shrub",
                CoreBiomes = [Biome.Plains, Biome.Highlands, Biome.Desert],
                SupportedWaterAvailabilities = [WaterAvailability.Low, WaterAvailability.Medium],
                PreferredFertilityMin = 0.20f,
                PreferredFertilityMax = 0.65f,
                GrowthRate = 0.45f,
                FoodValue = 0.35f,
                BaselineTraits = new BiologicalTraitProfile
                {
                    ColdTolerance = 42,
                    HeatTolerance = 56,
                    DroughtTolerance = 58,
                    Flexibility = 48,
                    BodySize = 46,
                    Reproduction = 44,
                    Mobility = 18,
                    Defense = 44,
                    Resilience = 52
                }
            },
            new FloraSpeciesDefinition
            {
                Id = "flora-reed",
                Name = "Reed",
                CoreBiomes = [Biome.Wetlands],
                SupportedWaterAvailabilities = [WaterAvailability.Medium, WaterAvailability.High],
                PreferredFertilityMin = 0.40f,
                PreferredFertilityMax = 0.90f,
                GrowthRate = 0.68f,
                FoodValue = 0.40f,
                BaselineTraits = new BiologicalTraitProfile
                {
                    ColdTolerance = 38,
                    HeatTolerance = 50,
                    DroughtTolerance = 24,
                    Flexibility = 42,
                    BodySize = 52,
                    Reproduction = 60,
                    Mobility = 22,
                    Defense = 28,
                    Resilience = 46
                }
            },
            new FloraSpeciesDefinition
            {
                Id = "flora-berry-bush",
                Name = "Berry Bush",
                CoreBiomes = [Biome.Forest, Biome.Wetlands],
                SupportedWaterAvailabilities = [WaterAvailability.Medium, WaterAvailability.High],
                PreferredFertilityMin = 0.50f,
                PreferredFertilityMax = 0.90f,
                GrowthRate = 0.40f,
                FoodValue = 0.70f,
                BaselineTraits = new BiologicalTraitProfile
                {
                    ColdTolerance = 46,
                    HeatTolerance = 52,
                    DroughtTolerance = 34,
                    Flexibility = 60,
                    BodySize = 44,
                    Reproduction = 42,
                    Mobility = 16,
                    Defense = 36,
                    Resilience = 50
                }
            },
            new FloraSpeciesDefinition
            {
                Id = "flora-moss",
                Name = "Moss",
                CoreBiomes = [Biome.Wetlands, Biome.Highlands, Biome.Tundra],
                SupportedWaterAvailabilities = [WaterAvailability.Medium, WaterAvailability.High],
                PreferredFertilityMin = 0.15f,
                PreferredFertilityMax = 0.55f,
                GrowthRate = 0.30f,
                FoodValue = 0.20f,
                BaselineTraits = new BiologicalTraitProfile
                {
                    ColdTolerance = 62,
                    HeatTolerance = 28,
                    DroughtTolerance = 18,
                    Flexibility = 34,
                    BodySize = 24,
                    Reproduction = 36,
                    Mobility = 8,
                    Defense = 26,
                    Resilience = 64
                }
            }
        ]);
    }
}
