using Species.Domain.Models;

namespace Species.Domain.Diagnostics;

public static class WorldSummaryFormatter
{
    public static string Format(World world)
    {
        var lines = new List<string>
        {
            $"World Seed: {world.Seed}",
            $"Total Regions: {world.Regions.Count}"
        };

        foreach (var region in world.Regions)
        {
            lines.Add(
                $"{region.Id} | {region.Name} | Biome={region.Biome} | Water={region.WaterAvailability} | Fertility={region.Fertility:0.00} | Neighbors=[{string.Join(", ", region.NeighborIds)}]");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
