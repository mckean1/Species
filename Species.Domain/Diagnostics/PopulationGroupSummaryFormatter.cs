using Species.Domain.Models;

namespace Species.Domain.Diagnostics;

public static class PopulationGroupSummaryFormatter
{
    public static string Format(World world)
    {
        var regionNamesById = world.Regions.ToDictionary(region => region.Id, region => region.Name, StringComparer.Ordinal);
        var lines = new List<string>
        {
            $"Population Groups: {world.PopulationGroups.Count}"
        };

        foreach (var group in world.PopulationGroups.OrderBy(group => group.Id, StringComparer.Ordinal))
        {
            var currentRegionName = regionNamesById.GetValueOrDefault(group.CurrentRegionId, "Unknown Region");
            var originRegionName = regionNamesById.GetValueOrDefault(group.OriginRegionId, "Unknown Region");
            var knownRegions = group.KnownRegionIds.Count == 0
                ? "none"
                : string.Join(", ", group.KnownRegionIds.OrderBy(id => id, StringComparer.Ordinal));
            var knownDiscoveries = group.KnownDiscoveryIds.Count == 0
                ? "none"
                : string.Join(", ", group.KnownDiscoveryIds.OrderBy(id => id, StringComparer.Ordinal));

            lines.Add(
                $"{group.Id} | {group.Name} | Species={group.SpeciesId} | CurrentRegion={group.CurrentRegionId} ({currentRegionName}) | OriginRegion={group.OriginRegionId} ({originRegionName}) | Population={group.Population} | StoredFood={group.StoredFood} | SubsistenceMode={group.SubsistenceMode} | Pressures=[Food:{group.Pressures.FoodPressure}, Water:{group.Pressures.WaterPressure}, Threat:{group.Pressures.ThreatPressure}, Overcrowding:{group.Pressures.OvercrowdingPressure}, Migration:{group.Pressures.MigrationPressure}] | KnownRegions=[{knownRegions}] | KnownDiscoveries=[{knownDiscoveries}]");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
