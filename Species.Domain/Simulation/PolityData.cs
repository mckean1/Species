using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed record PolityContext(
    Polity Polity,
    PopulationGroup? LeadGroup,
    IReadOnlyList<PopulationGroup> MemberGroups,
    int TotalPopulation,
    int TotalStoredFood,
    MaterialStockpile TotalMaterialStores,
    PressureState Pressures,
    IReadOnlySet<string> KnownRegionIds,
    IReadOnlySet<string> KnownDiscoveryIds,
    IReadOnlySet<string> LearnedAdvancementIds,
    Settlement? PrimarySettlement,
    string HomeRegionId,
    string CoreRegionId,
    string CurrentRegionId,
    string OriginRegionId,
    string SpeciesId,
    PolityAnchoringKind AnchoringKind,
    string ParentPolityId,
    GovernanceState Governance,
    ExternalPressureState ExternalPressure,
    PoliticalScaleState ScaleState,
    SocialMemoryState SocialMemory,
    SocialIdentityState SocialIdentity,
    MaterialProductionState MaterialProduction,
    int MaterialShortageMonths,
    int MaterialSurplusMonths);

public static class PolityData
{
    public static Polity? Resolve(World world, string polityId)
    {
        if (!string.IsNullOrWhiteSpace(polityId))
        {
            var polity = world.Polities.FirstOrDefault(item => string.Equals(item.Id, polityId, StringComparison.Ordinal));
            if (polity is not null)
            {
                return polity;
            }
        }

        if (!string.IsNullOrWhiteSpace(world.FocalPolityId))
        {
            var focal = world.Polities.FirstOrDefault(item => string.Equals(item.Id, world.FocalPolityId, StringComparison.Ordinal));
            if (focal is not null)
            {
                return focal;
            }
        }

        return world.Polities
            .Select(polity => BuildContext(world, polity))
            .Where(context => context is not null)
            .OrderByDescending(context => context!.TotalPopulation)
            .ThenBy(context => context!.Polity.Name, StringComparer.Ordinal)
            .Select(context => context!.Polity)
            .FirstOrDefault();
    }

    public static PolityContext? BuildContext(World world, Polity polity)
    {
        var groupsById = world.PopulationGroups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var memberGroups = polity.MemberGroupIds
            .Select(groupsById.GetValueOrDefault)
            .Where(group => group is not null)
            .Cast<PopulationGroup>()
            .OrderByDescending(group => group.Population)
            .ThenBy(group => group.Name, StringComparer.Ordinal)
            .ThenBy(group => group.Id, StringComparer.Ordinal)
            .ToArray();

        if (memberGroups.Length == 0)
        {
            return null;
        }

        var leadGroup = memberGroups[0];
        var totalPopulation = memberGroups.Sum(group => group.Population);
        var totalStoredFood = memberGroups.Sum(group => group.StoredFood);
        var totalMaterialStores = polity.MaterialStores.Clone();
        foreach (var settlement in polity.Settlements.Where(settlement => settlement.IsActive))
        {
            totalMaterialStores.Timber += settlement.MaterialStores.Timber;
            totalMaterialStores.Stone += settlement.MaterialStores.Stone;
            totalMaterialStores.Fiber += settlement.MaterialStores.Fiber;
            totalMaterialStores.Clay += settlement.MaterialStores.Clay;
            totalMaterialStores.Hides += settlement.MaterialStores.Hides;
        }
        var pressureWeight = Math.Max(1, totalPopulation);

        var knownRegionIds = memberGroups
            .SelectMany(group => group.KnownRegionIds)
            .ToHashSet(StringComparer.Ordinal);
        var knownDiscoveryIds = memberGroups
            .SelectMany(group => group.KnownDiscoveryIds)
            .ToHashSet(StringComparer.Ordinal);
        var learnedAdvancementIds = memberGroups
            .SelectMany(group => group.LearnedAdvancementIds)
            .ToHashSet(StringComparer.Ordinal);

        return new PolityContext(
            polity,
            leadGroup,
            memberGroups,
            totalPopulation,
            totalStoredFood,
            totalMaterialStores,
            new PressureState
            {
                FoodPressure = WeightedAverage(memberGroups, pressureWeight, group => group.Pressures.FoodPressure),
                WaterPressure = WeightedAverage(memberGroups, pressureWeight, group => group.Pressures.WaterPressure),
                ThreatPressure = WeightedAverage(memberGroups, pressureWeight, group => group.Pressures.ThreatPressure),
                OvercrowdingPressure = WeightedAverage(memberGroups, pressureWeight, group => group.Pressures.OvercrowdingPressure),
                MigrationPressure = WeightedAverage(memberGroups, pressureWeight, group => group.Pressures.MigrationPressure)
            },
            knownRegionIds,
            knownDiscoveryIds,
            learnedAdvancementIds,
            polity.Settlements.FirstOrDefault(settlement => settlement.IsActive && settlement.IsPrimary),
            string.IsNullOrWhiteSpace(polity.HomeRegionId) ? leadGroup.OriginRegionId : polity.HomeRegionId,
            string.IsNullOrWhiteSpace(polity.CoreRegionId) ? leadGroup.OriginRegionId : polity.CoreRegionId,
            leadGroup.CurrentRegionId,
            leadGroup.OriginRegionId,
            leadGroup.SpeciesId,
            polity.AnchoringKind,
            polity.ParentPolityId,
            polity.Governance.Clone(),
            polity.ExternalPressure.Clone(),
            polity.ScaleState.Clone(),
            polity.SocialMemory.Clone(),
            polity.SocialIdentity.Clone(),
            polity.MaterialProduction.Clone(),
            polity.MaterialShortageMonths,
            polity.MaterialSurplusMonths);
    }

    private static int WeightedAverage(
        IReadOnlyList<PopulationGroup> groups,
        int totalPopulation,
        Func<PopulationGroup, int> selector)
    {
        if (groups.Count == 0)
        {
            return 0;
        }

        if (totalPopulation <= 0)
        {
            return (int)Math.Round(groups.Average(selector), MidpointRounding.AwayFromZero);
        }

        var weightedTotal = groups.Sum(group => selector(group) * Math.Max(1, group.Population));
        return (int)Math.Round(weightedTotal / (double)groups.Sum(group => Math.Max(1, group.Population)), MidpointRounding.AwayFromZero);
    }
}
