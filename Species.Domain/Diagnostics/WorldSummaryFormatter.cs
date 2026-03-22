using Species.Domain.Models;

namespace Species.Domain.Diagnostics;

public static class WorldSummaryFormatter
{
    public static string Format(World world)
    {
        var lines = new List<string>
        {
            $"World Seed: {world.Seed}",
            $"Simulation Date: Year {world.CurrentYear}, Month {world.CurrentMonth}",
            $"Total Regions: {world.Regions.Count}",
            $"Population Groups: {world.PopulationGroups.Count}"
        };

        foreach (var region in world.Regions)
        {
            var flora = region.Ecosystem.FloraPopulations.Count == 0
                ? "none"
                : string.Join(", ", region.Ecosystem.FloraPopulations
                    .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                    .Select(entry => $"{entry.Key}:{entry.Value}"));
            var fauna = region.Ecosystem.FaunaPopulations.Count == 0
                ? "none"
                : string.Join(", ", region.Ecosystem.FaunaPopulations
                    .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                    .Select(entry => $"{entry.Key}:{entry.Value}"));
            var proto = region.Ecosystem.ProtoLifeSubstrate;

            lines.Add(
                $"{region.Id} | {region.Name} | Temp={region.TemperatureBand} | Terrain={region.TerrainRuggedness} | Biome={region.Biome} | Water={region.WaterAvailability} | Fertility={region.Fertility:0.00} | Neighbors=[{string.Join(", ", region.NeighborIds)}] | Proto=[FCap:{proto.ProtoFloraCapacity:0.00}, FaCap:{proto.ProtoFaunaCapacity:0.00}, FPress:{proto.ProtoFloraPressure:0.00}, FaPress:{proto.ProtoFaunaPressure:0.00}, Vacancy:{proto.EcologicalVacancy:0.00}] | Flora=[{flora}] | Fauna=[{fauna}]");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
