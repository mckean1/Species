using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Catalogs;

public sealed class FloraSpeciesCatalog
{
    private readonly Dictionary<string, FloraSpeciesDefinition> definitionsById;

    public FloraSpeciesCatalog(IReadOnlyList<FloraSpeciesDefinition> definitions)
    {
        Definitions = definitions;
        definitionsById = definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<FloraSpeciesDefinition> Definitions { get; }

    public FloraSpeciesDefinition? GetById(string id)
    {
        return definitionsById.GetValueOrDefault(id);
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
                FoodValue = 0.45f
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
                FoodValue = 0.35f
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
                FoodValue = 0.40f
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
                FoodValue = 0.70f
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
                FoodValue = 0.20f
            }
        ]);
    }
}
