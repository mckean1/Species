using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Catalogs;

public sealed class DiscoveryCatalog
{
    public const string ToolStoneId = "discovery-tool-stone";
    public const string ClayShapingId = "discovery-clay-shaping";
    public const string SeasonalTrackingId = "discovery-seasonal-tracking";
    public const string PreservationCluesId = "discovery-preservation-clues";
    public const string ShelterMethodsId = "discovery-shelter-methods";
    private const string LocalRegionDiscoveryPrefix = "discovery-local-region:";
    private const string LegacyLocalRegionConditionsDiscoveryPrefix = "discovery-local-region-conditions:";

    private readonly Dictionary<string, DiscoveryDefinition> definitionsById;

    public DiscoveryCatalog(IReadOnlyList<DiscoveryDefinition> definitions)
    {
        Definitions = definitions;
        definitionsById = definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<DiscoveryDefinition> Definitions { get; }

    public DiscoveryDefinition? GetById(string id)
    {
        if (TryNormalizeDiscoveryId(id, out var normalizedId))
        {
            id = normalizedId;
        }

        return definitionsById.GetValueOrDefault(id);
    }

    public string GetLocalFloraDiscoveryId(string regionId) => $"discovery-local-flora:{regionId}";

    public string GetLocalFaunaDiscoveryId(string regionId) => $"discovery-local-fauna:{regionId}";

    public string GetLocalWaterSourcesDiscoveryId(string regionId) => $"discovery-local-water-sources:{regionId}";

    public string GetLocalRegionDiscoveryId(string regionId) => $"{LocalRegionDiscoveryPrefix}{regionId}";

    public string GetLegacyLocalRegionConditionsDiscoveryId(string regionId) => $"{LegacyLocalRegionConditionsDiscoveryPrefix}{regionId}";

    public bool IsLocalRegionDiscoveryId(string discoveryId)
    {
        return discoveryId.StartsWith(LocalRegionDiscoveryPrefix, StringComparison.Ordinal) ||
               discoveryId.StartsWith(LegacyLocalRegionConditionsDiscoveryPrefix, StringComparison.Ordinal);
    }

    public bool IsLocalRegionDiscoveryKnown(IReadOnlySet<string> knownDiscoveryIds, string regionId)
    {
        return knownDiscoveryIds.Contains(GetLocalRegionDiscoveryId(regionId)) ||
               knownDiscoveryIds.Contains(GetLegacyLocalRegionConditionsDiscoveryId(regionId));
    }

    public bool TryNormalizeDiscoveryId(string discoveryId, out string normalizedId)
    {
        if (discoveryId.StartsWith(LegacyLocalRegionConditionsDiscoveryPrefix, StringComparison.Ordinal))
        {
            normalizedId = $"{LocalRegionDiscoveryPrefix}{discoveryId[LegacyLocalRegionConditionsDiscoveryPrefix.Length..]}";
            return true;
        }

        normalizedId = discoveryId;
        return discoveryId.StartsWith(LocalRegionDiscoveryPrefix, StringComparison.Ordinal);
    }

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
        var definitions = new List<DiscoveryDefinition>(world.Regions.Count * 4 + 4)
        {
            new DiscoveryDefinition
            {
                Id = ToolStoneId,
                Name = "Tool Stone",
                Description = "Knowledge that some local stone can be shaped into practical cutting and processing tools.",
                Category = DiscoveryCategory.Material,
                DecisionEffectSummary = "Helps the polity recognize tool-grade stone as a practical resource rather than inert ground.",
                CausalSummary = "Emerges from repeated stone exposure, processing need, and workable local material access.",
                ContactSpreadAllowed = true
            },
            new DiscoveryDefinition
            {
                Id = ClayShapingId,
                Name = "Clay Shaping",
                Description = "Knowledge that clay can be formed into useful storage and support containers.",
                Category = DiscoveryCategory.Material,
                DecisionEffectSummary = "Helps the polity recognize clay as a meaningful practical resource.",
                CausalSummary = "Emerges from repeated clay exposure, use, and settled handling.",
                ContactSpreadAllowed = true
            },
            new DiscoveryDefinition
            {
                Id = SeasonalTrackingId,
                Name = "Seasonal Tracking",
                Description = "Knowledge of recurring prey and regional movement patterns across seasons and returns.",
                Category = DiscoveryCategory.Fauna,
                DecisionEffectSummary = "Improves how the polity interprets repeated hunting and return-pattern opportunities.",
                CausalSummary = "Emerges from repeated hunting, route use, and seasonal return observation.",
                ContactSpreadAllowed = true
            },
            new DiscoveryDefinition
            {
                Id = PreservationCluesId,
                Name = "Preservation Clues",
                Description = "Knowledge that stored food can be kept more reliably through better handling and containment.",
                Category = DiscoveryCategory.Material,
                DecisionEffectSummary = "Helps the polity connect storage pressure to practical preservation methods.",
                CausalSummary = "Emerges from repeated surplus, shortage, and storage stress.",
                ContactSpreadAllowed = true
            },
            new DiscoveryDefinition
            {
                Id = ShelterMethodsId,
                Name = "Shelter Methods",
                Description = "Knowledge of sturdier shelter practices and protective use of local materials.",
                Category = DiscoveryCategory.Contact,
                DecisionEffectSummary = "Helps the polity interpret timber, stone, hides, and contact as shelter opportunities.",
                CausalSummary = "Emerges from material strain, durable settlement practice, or repeated outside contact.",
                ContactSpreadAllowed = true
            }
        };

        foreach (var region in world.Regions.OrderBy(region => region.Id, StringComparer.Ordinal))
        {
            definitions.Add(new DiscoveryDefinition
            {
                Id = $"discovery-local-flora:{region.Id}",
                Name = $"{region.Name} Flora",
                Description = $"Knowledge of edible and useful local plants in {region.Name}.",
                Category = DiscoveryCategory.Flora,
                DecisionEffectSummary = "Improves how the group recognizes flora opportunity in this region during decision-making.",
                CausalSummary = "Emerges from repeated gathering and observation in the region."
            });
            definitions.Add(new DiscoveryDefinition
            {
                Id = $"discovery-local-fauna:{region.Id}",
                Name = $"{region.Name} Fauna",
                Description = $"Knowledge of prey and dangerous animal patterns in {region.Name}.",
                Category = DiscoveryCategory.Fauna,
                DecisionEffectSummary = "Improves how the group recognizes fauna opportunity and danger in this region during decision-making.",
                CausalSummary = "Emerges from repeated residence, fauna observation, threat signs, and hunting in the region."
            });
            definitions.Add(new DiscoveryDefinition
            {
                Id = $"discovery-local-water-sources:{region.Id}",
                Name = $"{region.Name} Water Sources",
                Description = $"Knowledge of where water can be found in {region.Name}.",
                Category = DiscoveryCategory.Water,
                DecisionEffectSummary = "Improves how the group values water conditions in this region during decision-making.",
                CausalSummary = "Emerges from repeated water exposure and residence in the region."
            });
            definitions.Add(new DiscoveryDefinition
            {
                Id = $"{LocalRegionDiscoveryPrefix}{region.Id}",
                Name = region.Name,
                Description = $"Knowledge of {region.Name} as a practical region to traverse, use, and return to.",
                Category = DiscoveryCategory.Region,
                DecisionEffectSummary = "Improves how the group judges whether staying in or returning to this region is worthwhile.",
                CausalSummary = "Emerges from repeated residence, pressure, and practical use of the region."
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
                    DecisionEffectSummary = "Reduces uncertainty when evaluating this neighboring move.",
                    CausalSummary = "Emerges from repeated traversal of the route."
                });
            }
        }

        return new DiscoveryCatalog(definitions);
    }
}
