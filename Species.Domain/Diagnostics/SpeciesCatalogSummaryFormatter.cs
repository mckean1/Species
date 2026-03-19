using Species.Domain.Catalogs;
using Species.Domain.Models;

namespace Species.Domain.Diagnostics;

public static class SpeciesCatalogSummaryFormatter
{
    public static string Format(FloraSpeciesCatalog floraCatalog, FaunaSpeciesCatalog faunaCatalog)
    {
        var lines = new List<string>
        {
            $"Flora Species: {floraCatalog.Definitions.Count}",
            $"Fauna Species: {faunaCatalog.Definitions.Count}",
            string.Empty,
            "Flora Definitions:"
        };

        foreach (var flora in floraCatalog.Definitions)
        {
            lines.Add(FormatFlora(flora));
        }

        lines.Add(string.Empty);
        lines.Add("Fauna Definitions:");

        foreach (var fauna in faunaCatalog.Definitions)
        {
            lines.Add(FormatFauna(fauna));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatFlora(FloraSpeciesDefinition flora)
    {
        return $"{flora.Id} | {flora.Name} | CoreBiomes=[{string.Join(", ", flora.CoreBiomes)}] | Water=[{string.Join(", ", flora.SupportedWaterAvailabilities)}] | FertilityBand={flora.PreferredFertilityMin:0.00}-{flora.PreferredFertilityMax:0.00} | GrowthRate={flora.GrowthRate:0.00} | FoodValue={flora.FoodValue:0.00}";
    }

    private static string FormatFauna(FaunaSpeciesDefinition fauna)
    {
        return $"{fauna.Id} | {fauna.Name} | CoreBiomes=[{string.Join(", ", fauna.CoreBiomes)}] | Water=[{string.Join(", ", fauna.SupportedWaterAvailabilities)}] | Diet={fauna.DietCategory} | FoodRequirement={fauna.FoodRequirement:0.00} | ReproductionRate={fauna.ReproductionRate:0.00} | MigrationTendency={fauna.MigrationTendency:0.00} | FoodYield={fauna.FoodYield:0.00}";
    }
}
