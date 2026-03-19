using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Catalogs;

public sealed class FaunaSpeciesCatalog
{
    private readonly Dictionary<string, FaunaSpeciesDefinition> definitionsById;

    public FaunaSpeciesCatalog(IReadOnlyList<FaunaSpeciesDefinition> definitions)
    {
        Definitions = definitions;
        definitionsById = definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<FaunaSpeciesDefinition> Definitions { get; }

    public FaunaSpeciesDefinition? GetById(string id)
    {
        return definitionsById.GetValueOrDefault(id);
    }

    public static FaunaSpeciesCatalog CreateStarterSet()
    {
        return new FaunaSpeciesCatalog(
        [
            new FaunaSpeciesDefinition
            {
                Id = "fauna-small-grazer",
                Name = "Small Grazer",
                CoreBiomes = [Biome.Plains, Biome.Forest],
                SupportedWaterAvailabilities = [WaterAvailability.Low, WaterAvailability.Medium],
                DietCategory = DietCategory.Herbivore,
                FoodRequirement = 0.35f,
                ReproductionRate = 0.65f,
                MigrationTendency = 0.40f,
                FoodYield = 0.35f
            },
            new FaunaSpeciesDefinition
            {
                Id = "fauna-large-browser",
                Name = "Large Browser",
                CoreBiomes = [Biome.Forest, Biome.Plains, Biome.Highlands],
                SupportedWaterAvailabilities = [WaterAvailability.Medium, WaterAvailability.High],
                DietCategory = DietCategory.Herbivore,
                FoodRequirement = 0.70f,
                ReproductionRate = 0.30f,
                MigrationTendency = 0.50f,
                FoodYield = 0.85f
            },
            new FaunaSpeciesDefinition
            {
                Id = "fauna-scavenger",
                Name = "Scavenger",
                CoreBiomes = [Biome.Plains, Biome.Desert, Biome.Highlands],
                SupportedWaterAvailabilities = [WaterAvailability.Low, WaterAvailability.Medium],
                DietCategory = DietCategory.Omnivore,
                FoodRequirement = 0.28f,
                ReproductionRate = 0.42f,
                MigrationTendency = 0.72f,
                FoodYield = 0.25f
            },
            new FaunaSpeciesDefinition
            {
                Id = "fauna-small-predator",
                Name = "Small Predator",
                CoreBiomes = [Biome.Forest, Biome.Highlands, Biome.Wetlands],
                SupportedWaterAvailabilities = [WaterAvailability.Medium, WaterAvailability.High],
                DietCategory = DietCategory.Carnivore,
                FoodRequirement = 0.40f,
                ReproductionRate = 0.38f,
                MigrationTendency = 0.55f,
                FoodYield = 0.30f
            },
            new FaunaSpeciesDefinition
            {
                Id = "fauna-pack-hunter",
                Name = "Pack Hunter",
                CoreBiomes = [Biome.Plains, Biome.Forest, Biome.Tundra],
                SupportedWaterAvailabilities = [WaterAvailability.Low, WaterAvailability.Medium, WaterAvailability.High],
                DietCategory = DietCategory.Carnivore,
                FoodRequirement = 0.62f,
                ReproductionRate = 0.26f,
                MigrationTendency = 0.78f,
                FoodYield = 0.55f
            }
        ]);
    }
}
