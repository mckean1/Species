using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Catalogs;

public sealed class DiscoveryCatalog
{
    private readonly Dictionary<string, DiscoveryDefinition> definitionsById;

    public DiscoveryCatalog(IReadOnlyList<DiscoveryDefinition> definitions)
    {
        Definitions = definitions;
        definitionsById = definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<DiscoveryDefinition> Definitions { get; }

    public DiscoveryDefinition? GetById(string id)
    {
        return definitionsById.GetValueOrDefault(id);
    }

    public string GetLocalFloraDiscoveryId(string regionId) => $"discovery-local-flora:{regionId}";

    public string GetLocalFaunaDiscoveryId(string regionId) => $"discovery-local-fauna:{regionId}";

    public string GetLocalWaterSourcesDiscoveryId(string regionId) => $"discovery-local-water-sources:{regionId}";

    public string GetLocalRegionConditionsDiscoveryId(string regionId) => $"discovery-local-region-conditions:{regionId}";

    public string GetRouteKey(string firstRegionId, string secondRegionId)
    {
        return string.CompareOrdinal(firstRegionId, secondRegionId) <= 0
            ? $"{firstRegionId}|{secondRegionId}"
            : $"{secondRegionId}|{firstRegionId}";
    }

    public string GetRouteDiscoveryId(string firstRegionId, string secondRegionId)
    {
        return $"discovery-route:{GetRouteKey(firstRegionId, secondRegionId)}";
    }

    public static DiscoveryCatalog CreateForWorld(World world)
    {
        var definitions = new List<DiscoveryDefinition>(world.Regions.Count * 4);

        foreach (var region in world.Regions.OrderBy(region => region.Id, StringComparer.Ordinal))
        {
            definitions.Add(new DiscoveryDefinition
            {
                Id = $"discovery-local-flora:{region.Id}",
                Name = $"{region.Name} Flora",
                Description = $"Knowledge of edible and useful local plants in {region.Name}.",
                Category = DiscoveryCategory.Flora,
                DecisionEffectSummary = "Improves how the group recognizes flora opportunity in this region during decision-making."
            });
            definitions.Add(new DiscoveryDefinition
            {
                Id = $"discovery-local-fauna:{region.Id}",
                Name = $"{region.Name} Fauna",
                Description = $"Knowledge of prey and dangerous animal patterns in {region.Name}.",
                Category = DiscoveryCategory.Fauna,
                DecisionEffectSummary = "Improves how the group recognizes fauna opportunity and danger in this region during decision-making."
            });
            definitions.Add(new DiscoveryDefinition
            {
                Id = $"discovery-local-water-sources:{region.Id}",
                Name = $"{region.Name} Water Sources",
                Description = $"Knowledge of where water can be found in {region.Name}.",
                Category = DiscoveryCategory.Water,
                DecisionEffectSummary = "Improves how the group values water conditions in this region during decision-making."
            });
            definitions.Add(new DiscoveryDefinition
            {
                Id = $"discovery-local-region-conditions:{region.Id}",
                Name = $"{region.Name} Conditions",
                Description = $"Knowledge of the overall living conditions in {region.Name}.",
                Category = DiscoveryCategory.Region,
                DecisionEffectSummary = "Improves how the group judges whether staying in or returning to this region is worthwhile."
            });
        }

        var addedRoutes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var region in world.Regions.OrderBy(region => region.Id, StringComparer.Ordinal))
        {
            foreach (var neighborId in region.NeighborIds.OrderBy(id => id, StringComparer.Ordinal))
            {
                var routeKey = string.CompareOrdinal(region.Id, neighborId) <= 0
                    ? $"{region.Id}|{neighborId}"
                    : $"{neighborId}|{region.Id}";

                if (!addedRoutes.Add(routeKey))
                {
                    continue;
                }

                var neighborRegion = world.Regions.First(candidate => string.Equals(candidate.Id, neighborId, StringComparison.Ordinal));
                definitions.Add(new DiscoveryDefinition
                {
                    Id = $"discovery-route:{routeKey}",
                    Name = $"{region.Name} to {neighborRegion.Name} Route",
                    Description = $"Knowledge of the route between {region.Name} and {neighborRegion.Name}.",
                    Category = DiscoveryCategory.Route,
                    DecisionEffectSummary = "Reduces uncertainty when evaluating this neighboring move."
                });
            }
        }

        return new DiscoveryCatalog(definitions);
    }
}
