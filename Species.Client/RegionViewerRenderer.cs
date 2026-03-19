using Species.Domain.Catalogs;
using Species.Domain.Models;

public static class RegionViewerRenderer
{
    public static string Render(
        World world,
        int regionIndex,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var orderedRegions = world.Regions.OrderBy(region => region.Id, StringComparer.Ordinal).ToArray();
        if (orderedRegions.Length == 0)
        {
            return string.Join(
                Environment.NewLine,
                [
                    "Species MVP",
                    "Screen: Region Viewer",
                    "Controls: TAB switch screen | Left/Right browse regions | ESC quit",
                    string.Empty,
                    "No regions are available."
                ]);
        }

        var region = orderedRegions[Math.Clamp(regionIndex, 0, orderedRegions.Length - 1)];
        var regionsById = orderedRegions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var groupsInRegion = world.PopulationGroups
            .Where(group => string.Equals(group.CurrentRegionId, region.Id, StringComparison.Ordinal))
            .OrderByDescending(group => group.Population)
            .ThenBy(group => group.Name, StringComparer.Ordinal)
            .ToArray();

        var lines = new List<string>
        {
            "Species MVP",
            "Screen: Region Viewer",
            $"Date: Year {world.CurrentYear}, Month {world.CurrentMonth}",
            "Controls: ENTER advance month | TAB switch screen | Left/Right browse regions | ESC quit",
            string.Empty,
            $"Region {Array.IndexOf(orderedRegions, region) + 1} of {orderedRegions.Length}",
            $"{region.Name} ({region.Id})",
            $"Biome: {region.Biome}",
            $"Water: {region.WaterAvailability}",
            $"Fertility: {region.Fertility:0.00}",
            string.Empty,
            "Flora"
        };

        lines.AddRange(BuildPopulationSection(region.Ecosystem.FloraPopulations, floraCatalog.GetById));
        lines.Add(string.Empty);
        lines.Add("Fauna");
        lines.AddRange(BuildPopulationSection(region.Ecosystem.FaunaPopulations, faunaCatalog.GetById));
        lines.Add(string.Empty);
        lines.Add("Groups Here");

        if (groupsInRegion.Length == 0)
        {
            lines.Add("No groups are currently present.");
        }
        else
        {
            foreach (var group in groupsInRegion)
            {
                var discoveries = BuildNames(group.KnownDiscoveryIds, discoveryCatalog.GetById, 3);
                var advancements = BuildNames(group.LearnedAdvancementIds, advancementCatalog.GetById, 3);
                lines.Add($"{group.Name} | Population {group.Population} | Stored Food {group.StoredFood} | Subsistence {group.SubsistenceMode}");
                lines.Add($"Known Discoveries: {discoveries}");
                lines.Add($"Learned Advancements: {advancements}");
            }
        }

        lines.Add(string.Empty);
        lines.Add("Neighbors");

        foreach (var neighborId in region.NeighborIds.OrderBy(id => id, StringComparer.Ordinal))
        {
            var neighborName = regionsById.GetValueOrDefault(neighborId)?.Name ?? neighborId;
            lines.Add($"{neighborName} ({neighborId})");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> BuildPopulationSection<TDefinition>(
        IReadOnlyDictionary<string, int> populations,
        Func<string, TDefinition?> resolveDefinition)
        where TDefinition : class
    {
        if (populations.Count == 0)
        {
            return ["None"];
        }

        return populations
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry =>
            {
                var name = resolveDefinition(entry.Key) switch
                {
                    FloraSpeciesDefinition flora => flora.Name,
                    FaunaSpeciesDefinition fauna => fauna.Name,
                    _ => entry.Key
                };
                return $"{name}: {entry.Value}";
            })
            .ToArray();
    }

    private static string BuildNames<TDefinition>(
        IEnumerable<string> ids,
        Func<string, TDefinition?> resolveDefinition,
        int maxCount)
        where TDefinition : class
    {
        var names = ids
            .OrderBy(id => id, StringComparer.Ordinal)
            .Select(id => resolveDefinition(id) switch
            {
                DiscoveryDefinition discovery => discovery.Name,
                AdvancementDefinition advancement => advancement.Name,
                _ => id
            })
            .Take(maxCount)
            .ToArray();

        if (names.Length == 0)
        {
            return "None";
        }

        return string.Join(", ", names);
    }
}
