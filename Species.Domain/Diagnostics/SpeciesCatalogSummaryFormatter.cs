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
        return $"{flora.Id} | {flora.Name} | Class={flora.SpeciesClass} | CoreBiomes=[{string.Join(", ", flora.CoreBiomes)}] | Water=[{string.Join(", ", flora.SupportedWaterAvailabilities)}] | FertilityBand={flora.HabitatFertilityMin:0.00}-{flora.HabitatFertilityMax:0.00} | Growth={flora.GrowthRate:0.00} | Recovery={flora.RecoveryRate:0.00} | Biomass={flora.UsableBiomass:0.00} | Resilience={flora.ConsumptionResilience:0.00} | Spread={flora.SpreadTendency:0.00} | Abundance={flora.RegionalAbundance:0.00}";
    }

    private static string FormatFauna(FaunaSpeciesDefinition fauna)
    {
        var diet = string.Join(", ", fauna.DietLinks
            .OrderBy(link => link.IsFallback)
            .ThenByDescending(link => link.Weight)
            .Select(link =>
            {
                var target = link.TargetKind == Enums.FaunaDietTargetKind.ScavengePool
                    ? "scavenge"
                    : link.TargetSpeciesId;
                var role = link.IsFallback ? "fallback" : "preferred";
                return $"{target}:{link.Weight:0.00}:{role}";
            }));
        return $"{fauna.Id} | {fauna.Name} | Class={fauna.SpeciesClass} | CoreBiomes=[{string.Join(", ", fauna.CoreBiomes)}] | Water=[{string.Join(", ", fauna.SupportedWaterAvailabilities)}] | FertilityBand={fauna.HabitatFertilityMin:0.00}-{fauna.HabitatFertilityMax:0.00} | Diet={fauna.DietCategory} [{diet}] | Intake={fauna.RequiredIntake:0.00} | Repro={fauna.ReproductionRate:0.00} | Mortality={fauna.MortalitySensitivity:0.00} | Mobility={fauna.Mobility:0.00} | FeedEff={fauna.FeedingEfficiency:0.00} | Vulnerability={fauna.PredatorVulnerability:0.00} | Abundance={fauna.RegionalAbundance:0.00} | FoodYield={fauna.FoodYield:0.00}";
    }
}
