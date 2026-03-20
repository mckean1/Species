using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Catalogs;

public sealed class FaunaSpeciesCatalog
{
    private readonly Dictionary<string, FaunaSpeciesDefinition> definitionsById;
    private readonly List<FaunaSpeciesDefinition> definitions;

    public FaunaSpeciesCatalog(IReadOnlyList<FaunaSpeciesDefinition> definitions)
    {
        this.definitions = definitions.ToList();
        Definitions = this.definitions;
        definitionsById = this.definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<FaunaSpeciesDefinition> Definitions { get; }

    public FaunaSpeciesDefinition? GetById(string id)
    {
        return definitionsById.GetValueOrDefault(id);
    }

    public void AddOrReplace(FaunaSpeciesDefinition definition)
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
                FoodYield = 0.35f,
                BaselineTraits = new BiologicalTraitProfile
                {
                    ColdTolerance = 44,
                    HeatTolerance = 46,
                    DroughtTolerance = 42,
                    Flexibility = 48,
                    BodySize = 34,
                    Reproduction = 66,
                    Mobility = 52,
                    Defense = 28,
                    Resilience = 42
                }
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
                FoodYield = 0.85f,
                BaselineTraits = new BiologicalTraitProfile
                {
                    ColdTolerance = 48,
                    HeatTolerance = 44,
                    DroughtTolerance = 36,
                    Flexibility = 34,
                    BodySize = 72,
                    Reproduction = 34,
                    Mobility = 46,
                    Defense = 58,
                    Resilience = 58
                }
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
                FoodYield = 0.25f,
                BaselineTraits = new BiologicalTraitProfile
                {
                    ColdTolerance = 42,
                    HeatTolerance = 58,
                    DroughtTolerance = 54,
                    Flexibility = 72,
                    BodySize = 28,
                    Reproduction = 42,
                    Mobility = 74,
                    Defense = 24,
                    Resilience = 44
                }
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
                FoodYield = 0.30f,
                BaselineTraits = new BiologicalTraitProfile
                {
                    ColdTolerance = 46,
                    HeatTolerance = 46,
                    DroughtTolerance = 34,
                    Flexibility = 36,
                    BodySize = 42,
                    Reproduction = 38,
                    Mobility = 58,
                    Defense = 52,
                    Resilience = 46
                }
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
                FoodYield = 0.55f,
                BaselineTraits = new BiologicalTraitProfile
                {
                    ColdTolerance = 54,
                    HeatTolerance = 40,
                    DroughtTolerance = 38,
                    Flexibility = 28,
                    BodySize = 62,
                    Reproduction = 28,
                    Mobility = 78,
                    Defense = 60,
                    Resilience = 52
                }
            }
        ]);
    }
}
