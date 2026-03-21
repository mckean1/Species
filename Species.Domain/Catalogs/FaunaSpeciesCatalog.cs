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
                HabitatFertilityMin = 0.30f,
                HabitatFertilityMax = 0.78f,
                DietCategory = DietCategory.Herbivore,
                DietLinks =
                [
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FloraSpecies, TargetSpeciesId = "flora-grass", Weight = 0.70f },
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FloraSpecies, TargetSpeciesId = "flora-shrub", Weight = 0.20f },
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FloraSpecies, TargetSpeciesId = "flora-moss", Weight = 0.10f, IsFallback = true }
                ],
                RequiredIntake = 0.32f,
                ReproductionRate = 0.62f,
                MortalitySensitivity = 0.44f,
                Mobility = 0.40f,
                FeedingEfficiency = 0.58f,
                PredatorVulnerability = 0.72f,
                RegionalAbundance = 0.76f,
                Conspicuousness = 0.56f,
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
                HabitatFertilityMin = 0.38f,
                HabitatFertilityMax = 0.88f,
                DietCategory = DietCategory.Herbivore,
                DietLinks =
                [
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FloraSpecies, TargetSpeciesId = "flora-berry-bush", Weight = 0.42f },
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FloraSpecies, TargetSpeciesId = "flora-shrub", Weight = 0.33f },
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FloraSpecies, TargetSpeciesId = "flora-grass", Weight = 0.25f, IsFallback = true }
                ],
                RequiredIntake = 0.64f,
                ReproductionRate = 0.30f,
                MortalitySensitivity = 0.58f,
                Mobility = 0.46f,
                FeedingEfficiency = 0.52f,
                PredatorVulnerability = 0.34f,
                RegionalAbundance = 0.48f,
                Conspicuousness = 0.70f,
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
                HabitatFertilityMin = 0.18f,
                HabitatFertilityMax = 0.62f,
                DietCategory = DietCategory.Omnivore,
                DietLinks =
                [
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FloraSpecies, TargetSpeciesId = "flora-berry-bush", Weight = 0.28f },
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FloraSpecies, TargetSpeciesId = "flora-shrub", Weight = 0.17f },
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FaunaSpecies, TargetSpeciesId = "fauna-small-grazer", Weight = 0.22f },
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.ScavengePool, Weight = 0.33f, IsFallback = true }
                ],
                RequiredIntake = 0.28f,
                ReproductionRate = 0.42f,
                MortalitySensitivity = 0.46f,
                Mobility = 0.74f,
                FeedingEfficiency = 0.70f,
                PredatorVulnerability = 0.62f,
                RegionalAbundance = 0.54f,
                Conspicuousness = 0.44f,
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
                HabitatFertilityMin = 0.26f,
                HabitatFertilityMax = 0.76f,
                DietCategory = DietCategory.Carnivore,
                DietLinks =
                [
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FaunaSpecies, TargetSpeciesId = "fauna-small-grazer", Weight = 0.60f },
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FaunaSpecies, TargetSpeciesId = "fauna-scavenger", Weight = 0.16f },
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.ScavengePool, Weight = 0.24f, IsFallback = true }
                ],
                RequiredIntake = 0.38f,
                ReproductionRate = 0.38f,
                MortalitySensitivity = 0.48f,
                Mobility = 0.58f,
                FeedingEfficiency = 0.66f,
                PredatorVulnerability = 0.40f,
                RegionalAbundance = 0.42f,
                Conspicuousness = 0.52f,
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
                HabitatFertilityMin = 0.24f,
                HabitatFertilityMax = 0.74f,
                DietCategory = DietCategory.Carnivore,
                DietLinks =
                [
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FaunaSpecies, TargetSpeciesId = "fauna-large-browser", Weight = 0.54f },
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FaunaSpecies, TargetSpeciesId = "fauna-small-grazer", Weight = 0.28f },
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.FaunaSpecies, TargetSpeciesId = "fauna-scavenger", Weight = 0.06f, IsFallback = true },
                    new FaunaDietLink { TargetKind = FaunaDietTargetKind.ScavengePool, Weight = 0.12f, IsFallback = true }
                ],
                RequiredIntake = 0.58f,
                ReproductionRate = 0.26f,
                MortalitySensitivity = 0.56f,
                Mobility = 0.78f,
                FeedingEfficiency = 0.74f,
                PredatorVulnerability = 0.22f,
                RegionalAbundance = 0.30f,
                Conspicuousness = 0.66f,
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
