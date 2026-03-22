using Species.Domain.Catalogs;
using Species.Domain.Discovery;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class CuriosityPressureSystem
{
    private const int DiscoveryRelief = 45;
    private const int ScoutDiscoveryRelief = 65;
    private const int EncounterRelief = 80;
    private const int AdvancementRelief = 100;
    private const int SomeOpportunityGain = 18;
    private const int SubstantialOpportunityGainBonus = 10;

    public World Run(
        World world,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        IReadOnlyList<DiscoveryChange> scoutingChanges,
        IReadOnlyList<DiscoveryChange> discoveryChanges,
        IReadOnlyList<AdvancementChange> advancementChanges)
    {
        if (world.Polities.Count == 0)
        {
            return world;
        }

        var updatedPolities = world.Polities
            .Select(polity => polity.Clone())
            .ToArray();
        var updatedWorld = new World(
            world.Seed,
            world.CurrentYear,
            world.CurrentMonth,
            world.Regions,
            world.PopulationGroups,
            world.Chronicle,
            updatedPolities,
            world.FocalPolityId);
        var polityIdsByGroupId = world.PopulationGroups.ToDictionary(group => group.Id, group => group.PolityId, StringComparer.Ordinal);
        var scoutingChangesByPolityId = GroupDiscoveryChangesByPolityId(scoutingChanges, polityIdsByGroupId);
        var discoveryChangesByPolityId = GroupDiscoveryChangesByPolityId(discoveryChanges, polityIdsByGroupId);
        var advancementChangesByPolityId = GroupAdvancementChangesByPolityId(advancementChanges, polityIdsByGroupId);

        foreach (var polity in updatedPolities)
        {
            var context = PolityData.BuildContext(updatedWorld, polity);
            if (context is null)
            {
                continue;
            }

            var relief = ResolveRelief(
                scoutingChangesByPolityId.GetValueOrDefault(polity.Id),
                discoveryChangesByPolityId.GetValueOrDefault(polity.Id),
                advancementChangesByPolityId.GetValueOrDefault(polity.Id));
            if (relief > 0)
            {
                polity.Curiosity.MonthsSinceLastNovelty = 0;
                polity.Curiosity.StoredRawPressure = Math.Max(0, polity.Curiosity.StoredRawPressure - relief);
                continue;
            }

            polity.Curiosity.MonthsSinceLastNovelty++;
            var opportunityLevel = ResolveOpportunityLevel(context, updatedWorld, discoveryCatalog, floraCatalog, faunaCatalog);
            var droughtGrowth = ResolveDroughtGrowth(polity.Curiosity.MonthsSinceLastNovelty);
            var stabilityModifier = ResolveStabilityModifier(context);
            var growth = ResolveGrowth(droughtGrowth, opportunityLevel, stabilityModifier);
            polity.Curiosity.StoredRawPressure = Math.Max(0, polity.Curiosity.StoredRawPressure + growth);
        }

        return updatedWorld;
    }

    private static Dictionary<string, List<DiscoveryChange>> GroupDiscoveryChangesByPolityId(
        IReadOnlyList<DiscoveryChange> changes,
        IReadOnlyDictionary<string, string> polityIdsByGroupId)
    {
        var result = new Dictionary<string, List<DiscoveryChange>>(StringComparer.Ordinal);
        foreach (var change in changes.Where(change => change.UnlockedEntries.Count > 0))
        {
            var polityId = ResolvePolityId(change.GroupId, polityIdsByGroupId);
            if (string.IsNullOrWhiteSpace(polityId))
            {
                continue;
            }

            if (!result.TryGetValue(polityId, out var grouped))
            {
                grouped = [];
                result[polityId] = grouped;
            }

            grouped.Add(change);
        }

        return result;
    }

    private static Dictionary<string, List<AdvancementChange>> GroupAdvancementChangesByPolityId(
        IReadOnlyList<AdvancementChange> changes,
        IReadOnlyDictionary<string, string> polityIdsByGroupId)
    {
        var result = new Dictionary<string, List<AdvancementChange>>(StringComparer.Ordinal);
        foreach (var change in changes.Where(change => change.UnlockedAdvancementNames.Count > 0))
        {
            var polityId = ResolvePolityId(change.GroupId, polityIdsByGroupId);
            if (string.IsNullOrWhiteSpace(polityId))
            {
                continue;
            }

            if (!result.TryGetValue(polityId, out var grouped))
            {
                grouped = [];
                result[polityId] = grouped;
            }

            grouped.Add(change);
        }

        return result;
    }

    private static string ResolvePolityId(string groupId, IReadOnlyDictionary<string, string> polityIdsByGroupId)
    {
        if (polityIdsByGroupId.TryGetValue(groupId, out var polityId))
        {
            return polityId;
        }

        return groupId;
    }

    private static int ResolveRelief(
        IReadOnlyList<DiscoveryChange>? scoutingChanges,
        IReadOnlyList<DiscoveryChange>? discoveryChanges,
        IReadOnlyList<AdvancementChange>? advancementChanges)
    {
        var relief = 0;
        if (discoveryChanges is not null)
        {
            relief += discoveryChanges.Sum(change => ResolveDiscoveryRelief(change));
        }

        if (scoutingChanges is not null)
        {
            relief += scoutingChanges.Sum(change => ResolveDiscoveryRelief(change, isScoutSourced: true));
        }

        if (advancementChanges is not null)
        {
            relief += advancementChanges.Count * AdvancementRelief;
        }

        return relief;
    }

    private static int ResolveDiscoveryRelief(DiscoveryChange change, bool isScoutSourced = false)
    {
        if (change.UnlockedEntries.Any(entry => entry.IsEncounter))
        {
            return EncounterRelief;
        }

        return isScoutSourced || change.UnlockedEntries.Any(entry => entry.IsScoutSourced)
            ? ScoutDiscoveryRelief
            : DiscoveryRelief;
    }

    private static int ResolveGrowth(int droughtGrowth, ReachableOpportunityLevel opportunityLevel, float stabilityModifier)
    {
        if (droughtGrowth <= 0 || stabilityModifier <= 0.0f || opportunityLevel == ReachableOpportunityLevel.None)
        {
            return 0;
        }

        var opportunityGain = SomeOpportunityGain;
        if (opportunityLevel == ReachableOpportunityLevel.Substantial)
        {
            opportunityGain += SubstantialOpportunityGainBonus;
        }

        return (int)Math.Round(droughtGrowth * opportunityGain * stabilityModifier, MidpointRounding.AwayFromZero);
    }

    private static int ResolveDroughtGrowth(int monthsSinceLastNovelty)
    {
        return monthsSinceLastNovelty switch
        {
            <= 5 => 0,
            <= 11 => 1,
            <= 23 => 2,
            _ => 3
        };
    }

    private static float ResolveStabilityModifier(PolityContext context)
    {
        var severeFoodCrisis =
            context.FoodAccounting.UnresolvedDeficit > 0 ||
            context.Pressures.Food.DisplayValue >= 75 ||
            context.Pressures.Water.DisplayValue >= 75;
        var collapse =
            context.FoodAccounting.EndingTotalStores <= 0 &&
            severeFoodCrisis &&
            context.MaterialShortageMonths >= 4;
        if (collapse)
        {
            return 0.0f;
        }

        if (severeFoodCrisis || context.MaterialShortageMonths >= 3 || context.Pressures.Threat.DisplayValue >= 75)
        {
            return 0.2f;
        }

        if (context.Pressures.Food.DisplayValue >= 50 ||
            context.Pressures.Water.DisplayValue >= 50 ||
            context.Pressures.Migration.DisplayValue >= 50 ||
            context.MaterialShortageMonths > 0)
        {
            return 0.55f;
        }

        return 1.0f;
    }

    private static ReachableOpportunityLevel ResolveOpportunityLevel(
        PolityContext context,
        World world,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var reachableUnknownCount = 0;
        var partiallyKnownCount = 0;
        var baseRegionIds = context.Polity.RegionalPresences
            .Where(presence => presence.IsCurrent || presence.Kind is PolityPresenceKind.Seasonal or PolityPresenceKind.Habitation or PolityPresenceKind.Core)
            .Select(presence => presence.RegionId)
            .Concat(context.MemberGroups.Select(group => group.CurrentRegionId))
            .Distinct(StringComparer.Ordinal);

        foreach (var baseRegionId in baseRegionIds)
        {
            if (!regionsById.TryGetValue(baseRegionId, out var sourceRegion))
            {
                continue;
            }

            foreach (var neighborId in sourceRegion.NeighborIds.Distinct(StringComparer.Ordinal))
            {
                if (!regionsById.TryGetValue(neighborId, out var targetRegion))
                {
                    continue;
                }

                if (IsUnknownFrontier(context, targetRegion, discoveryCatalog))
                {
                    reachableUnknownCount++;
                    continue;
                }

                if (!IsFullyKnown(context, targetRegion, discoveryCatalog, floraCatalog, faunaCatalog))
                {
                    partiallyKnownCount++;
                }
            }
        }

        if (reachableUnknownCount >= 2 || (reachableUnknownCount >= 1 && partiallyKnownCount >= 2))
        {
            return ReachableOpportunityLevel.Substantial;
        }

        if (reachableUnknownCount > 0 || partiallyKnownCount > 0)
        {
            return ReachableOpportunityLevel.Some;
        }

        return ReachableOpportunityLevel.None;
    }

    private static bool IsUnknownFrontier(PolityContext context, Region targetRegion, DiscoveryCatalog discoveryCatalog)
    {
        return !context.KnownRegionIds.Contains(targetRegion.Id) ||
               !context.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalWaterSourcesDiscoveryId(targetRegion.Id)) ||
               !discoveryCatalog.IsLocalRegionDiscoveryKnown(context.KnownDiscoveryIds, targetRegion.Id);
    }

    private static bool IsFullyKnown(
        PolityContext context,
        Region targetRegion,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var hasRegionKnowledge = context.KnownRegionIds.Contains(targetRegion.Id) &&
                                 context.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalWaterSourcesDiscoveryId(targetRegion.Id)) &&
                                 discoveryCatalog.IsLocalRegionDiscoveryKnown(context.KnownDiscoveryIds, targetRegion.Id);
        var unknownFlora = targetRegion.Ecosystem.FloraPopulations
            .Where(entry => entry.Value > 0)
            .Any(entry =>
            {
                var flora = floraCatalog.GetById(entry.Key);
                return flora is not null &&
                       flora.Tags.Contains(FloraTag.Edible) &&
                       !context.Polity.SpeciesAwareness.Any(state =>
                           state.SpeciesClass == SpeciesClass.Flora &&
                           string.Equals(state.SpeciesId, entry.Key, StringComparison.Ordinal) &&
                           state.CurrentStage == DiscoveryStage.Discovered);
            });
        var unknownFauna = targetRegion.Ecosystem.FaunaPopulations
            .Where(entry => entry.Value > 0)
            .Any(entry =>
            {
                var fauna = faunaCatalog.GetById(entry.Key);
                return fauna is not null &&
                       !context.Polity.SpeciesAwareness.Any(state =>
                           state.SpeciesClass == SpeciesClass.Fauna &&
                           string.Equals(state.SpeciesId, entry.Key, StringComparison.Ordinal) &&
                           state.CurrentStage == DiscoveryStage.Discovered);
            });

        return hasRegionKnowledge && !unknownFlora && !unknownFauna;
    }

    private enum ReachableOpportunityLevel
    {
        None,
        Some,
        Substantial
    }
}
