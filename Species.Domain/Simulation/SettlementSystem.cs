using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class SettlementSystem
{
    public SettlementUpdateResult Run(World world)
    {
        var groupsById = world.PopulationGroups.ToDictionary(group => group.Id, CloneGroup, StringComparer.Ordinal);
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var updatedPolities = new List<Polity>(world.Polities.Count);
        var changes = new List<SettlementChange>();

        foreach (var polity in world.Polities.OrderBy(polity => polity.Id, StringComparer.Ordinal))
        {
            var updatedPolity = polity.Clone();
            var previousPresences = polity.RegionalPresences.ToDictionary(presence => presence.RegionId, presence => presence.Clone(), StringComparer.Ordinal);
            var currentGroups = updatedPolity.MemberGroupIds
                .Select(groupsById.GetValueOrDefault)
                .Where(group => group is not null)
                .Cast<PopulationGroup>()
                .OrderByDescending(group => group.Population)
                .ThenBy(group => group.Id, StringComparer.Ordinal)
                .ToArray();

            if (currentGroups.Length == 0)
            {
                updatedPolities.Add(updatedPolity);
                continue;
            }

            if (string.IsNullOrWhiteSpace(updatedPolity.HomeRegionId))
            {
                updatedPolity.HomeRegionId = currentGroups[0].OriginRegionId;
            }

            var localGroupsByRegionId = currentGroups
                .GroupBy(group => group.CurrentRegionId, StringComparer.Ordinal)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray(), StringComparer.Ordinal);

            UpdateRegionalPresences(updatedPolity, previousPresences, localGroupsByRegionId, changes, regionsById);
            UpdateSettlements(updatedPolity, localGroupsByRegionId, world, changes, groupsById, regionsById);
            UpdatePolityAnchoring(updatedPolity, previousPresences, changes, regionsById, currentGroups);
            updatedPolities.Add(updatedPolity);
        }

        var updatedGroups = world.PopulationGroups
            .Select(group => groupsById[group.Id])
            .ToArray();

        return new SettlementUpdateResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, updatedPolities, world.FocalPolityId),
            changes);
    }

    private static void UpdateRegionalPresences(
        Polity polity,
        IReadOnlyDictionary<string, PolityRegionalPresence> previousPresences,
        IReadOnlyDictionary<string, PopulationGroup[]> localGroupsByRegionId,
        ICollection<SettlementChange> changes,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        var presencesByRegionId = polity.RegionalPresences.ToDictionary(presence => presence.RegionId, StringComparer.Ordinal);
        var activeSettlementByRegionId = polity.Settlements
            .Where(settlement => settlement.IsActive)
            .GroupBy(settlement => settlement.RegionId, StringComparer.Ordinal)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.OrderByDescending(settlement => ScoreSettlementType(settlement.Type)).First(), StringComparer.Ordinal);
        var allRegionIds = presencesByRegionId.Keys
            .Concat(localGroupsByRegionId.Keys)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        polity.RegionalPresences.Clear();

        foreach (var regionId in allRegionIds.OrderBy(id => id, StringComparer.Ordinal))
        {
            var presence = presencesByRegionId.TryGetValue(regionId, out var existing)
                ? existing.Clone()
                : new PolityRegionalPresence { RegionId = regionId };
            var wasKind = previousPresences.TryGetValue(regionId, out var previousPresence)
                ? previousPresence.Kind
                : (PolityPresenceKind?)null;
            var wasCurrent = previousPresence?.IsCurrent ?? false;
            var isCurrent = localGroupsByRegionId.TryGetValue(regionId, out var localGroups) && localGroups.Length > 0;
            var localPopulation = isCurrent ? localGroups!.Sum(group => group.Population) : 0;
            var settlement = activeSettlementByRegionId.GetValueOrDefault(regionId);

            if (isCurrent)
            {
                presence.TotalMonthsPresent++;
                presence.ConsecutiveMonthsPresent++;
                if (presence.MonthsSinceLastPresence > 0)
                {
                    presence.ReturnCount++;
                }

                presence.MonthsSinceLastPresence = 0;
                presence.IsCurrent = true;
            }
            else
            {
                presence.ConsecutiveMonthsPresent = 0;
                presence.MonthsSinceLastPresence++;
                presence.IsCurrent = false;
            }

            presence.Kind = ResolvePresenceKind(presence, settlement, localPopulation);

            if (presence.MonthsSinceLastPresence > 18 && settlement is null)
            {
                continue;
            }

            polity.RegionalPresences.Add(presence);

            if (!wasCurrent && presence.IsCurrent && presence.Kind == PolityPresenceKind.Seasonal && presence.ReturnCount >= 2)
            {
                changes.Add(BuildChange(
                    polity,
                    regionsById,
                    regionId,
                    settlement,
                    SettlementChangeKind.SeasonalPattern,
                    $"{polity.Name} established a seasonal return to {ResolveRegionName(regionsById, regionId)}."));
            }

            if (wasCurrent && !presence.IsCurrent && wasKind is PolityPresenceKind.Seasonal or PolityPresenceKind.Habitation or PolityPresenceKind.Core)
            {
                changes.Add(BuildChange(
                    polity,
                    regionsById,
                    regionId,
                    settlement,
                    SettlementChangeKind.PresenceLost,
                    $"{polity.Name} lost its outlying presence in {ResolveRegionName(regionsById, regionId)}."));
            }
        }
    }

    private static void UpdateSettlements(
        Polity polity,
        IReadOnlyDictionary<string, PopulationGroup[]> localGroupsByRegionId,
        World world,
        ICollection<SettlementChange> changes,
        IDictionary<string, PopulationGroup> groupsById,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        foreach (var settlement in polity.Settlements.Where(settlement => settlement.IsActive))
        {
            if (localGroupsByRegionId.ContainsKey(settlement.RegionId))
            {
                continue;
            }

            var presence = polity.RegionalPresences.FirstOrDefault(item => string.Equals(item.RegionId, settlement.RegionId, StringComparison.Ordinal));
            if (presence?.MonthsSinceLastPresence >= 6)
            {
                settlement.IsActive = false;
                settlement.IsPrimary = false;
                changes.Add(BuildChange(
                    polity,
                    regionsById,
                    settlement.RegionId,
                    settlement,
                    SettlementChangeKind.Abandoned,
                    $"{polity.Name} abandoned {settlement.Name} in {ResolveRegionName(regionsById, settlement.RegionId)}."));
            }
        }

        foreach (var (regionId, localGroups) in localGroupsByRegionId.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            var presence = polity.RegionalPresences.FirstOrDefault(item => string.Equals(item.RegionId, regionId, StringComparison.Ordinal));
            if (presence is null)
            {
                continue;
            }

            var currentSettlement = polity.Settlements.FirstOrDefault(settlement =>
                settlement.IsActive &&
                string.Equals(settlement.RegionId, regionId, StringComparison.Ordinal));
            var candidateType = ResolveSettlementCandidate(polity, presence, localGroups);

            if (candidateType is not null && currentSettlement is null)
            {
                currentSettlement = new Settlement
                {
                    Id = $"{polity.Id}:settlement:{regionId}",
                    Name = BuildSettlementName(regionsById, regionId, candidateType.Value),
                    PolityId = polity.Id,
                    RegionId = regionId,
                    Type = candidateType.Value,
                    FoundedYear = world.CurrentYear,
                    FoundedMonth = world.CurrentMonth,
                    IsActive = true,
                    StoredFood = 0
                };
                polity.Settlements.Add(currentSettlement);
                changes.Add(BuildChange(
                    polity,
                    regionsById,
                    regionId,
                    currentSettlement,
                    SettlementChangeKind.Founded,
                    $"{polity.Name} founded {currentSettlement.Name} in {ResolveRegionName(regionsById, regionId)}."));
            }
            else if (candidateType is not null &&
                     currentSettlement is not null &&
                     ScoreSettlementType(candidateType.Value) > ScoreSettlementType(currentSettlement.Type))
            {
                currentSettlement.Type = candidateType.Value;
                currentSettlement.Name = BuildSettlementName(regionsById, regionId, candidateType.Value);
            }

            if (currentSettlement is not null && currentSettlement.IsActive)
            {
                DepositSettlementStores(currentSettlement, localGroups, groupsById);
            }
        }
    }

    private static void UpdatePolityAnchoring(
        Polity polity,
        IReadOnlyDictionary<string, PolityRegionalPresence> previousPresences,
        ICollection<SettlementChange> changes,
        IReadOnlyDictionary<string, Region> regionsById,
        IReadOnlyList<PopulationGroup> currentGroups)
    {
        var previousCoreRegionId = polity.CoreRegionId;
        var previousAnchoringKind = polity.AnchoringKind;
        var activeSettlements = polity.Settlements
            .Where(settlement => settlement.IsActive)
            .OrderByDescending(settlement => settlement.IsPrimary)
            .ThenByDescending(settlement => ScoreSettlementType(settlement.Type))
            .ThenByDescending(settlement => settlement.StoredFood)
            .ThenBy(settlement => settlement.Id, StringComparer.Ordinal)
            .ToArray();

        var primarySettlement = activeSettlements
            .OrderByDescending(settlement => ComputeSettlementPriority(polity, settlement))
            .FirstOrDefault();
        foreach (var settlement in polity.Settlements)
        {
            settlement.IsPrimary = primarySettlement is not null && string.Equals(settlement.Id, primarySettlement.Id, StringComparison.Ordinal);
        }

        polity.PrimarySettlementId = primarySettlement?.Id ?? string.Empty;

        polity.CoreRegionId = ResolveCoreRegionId(polity, primarySettlement, currentGroups);
        polity.AnchoringKind = ResolveAnchoringKind(polity, primarySettlement);

        if (string.IsNullOrWhiteSpace(polity.HomeRegionId) && currentGroups.Count > 0)
        {
            polity.HomeRegionId = currentGroups[0].OriginRegionId;
        }

        if (polity.AnchoringKind == PolityAnchoringKind.Anchored &&
            (!string.Equals(previousCoreRegionId, polity.CoreRegionId, StringComparison.Ordinal) || previousAnchoringKind != PolityAnchoringKind.Anchored))
        {
            changes.Add(BuildChange(
                polity,
                regionsById,
                polity.CoreRegionId,
                primarySettlement,
                SettlementChangeKind.Anchored,
                $"{polity.Name} anchored itself in {ResolveRegionName(regionsById, polity.CoreRegionId)}."));
        }
    }

    private static string ResolveCoreRegionId(Polity polity, Settlement? primarySettlement, IReadOnlyList<PopulationGroup> currentGroups)
    {
        var bestPresence = polity.RegionalPresences
            .OrderByDescending(presence => ScorePresence(polity, presence, primarySettlement))
            .ThenBy(presence => presence.RegionId, StringComparer.Ordinal)
            .FirstOrDefault();

        if (bestPresence is not null && ScorePresence(polity, bestPresence, primarySettlement) > 0)
        {
            return bestPresence.RegionId;
        }

        return primarySettlement?.RegionId
               ?? currentGroups.FirstOrDefault()?.CurrentRegionId
               ?? polity.HomeRegionId;
    }

    private static int ScorePresence(Polity polity, PolityRegionalPresence presence, Settlement? primarySettlement)
    {
        var kindScore = presence.Kind switch
        {
            PolityPresenceKind.Core => 400,
            PolityPresenceKind.Habitation => 250,
            PolityPresenceKind.Seasonal => 140,
            PolityPresenceKind.PassingThrough => 60,
            _ => 0
        };
        var settlementBonus = polity.Settlements.Any(settlement =>
            settlement.IsActive &&
            string.Equals(settlement.RegionId, presence.RegionId, StringComparison.Ordinal))
            ? 120
            : 0;
        var primaryBonus = primarySettlement is not null && string.Equals(primarySettlement.RegionId, presence.RegionId, StringComparison.Ordinal)
            ? 120
            : 0;
        var currentBonus = presence.IsCurrent ? 40 : 0;
        var absencePenalty = presence.MonthsSinceLastPresence * 10;

        return kindScore + settlementBonus + primaryBonus + currentBonus + presence.TotalMonthsPresent - absencePenalty;
    }

    private static PolityAnchoringKind ResolveAnchoringKind(Polity polity, Settlement? primarySettlement)
    {
        if (primarySettlement?.Type == SettlementType.Village ||
            polity.RegionalPresences.Any(presence => presence.Kind == PolityPresenceKind.Core && presence.MonthsSinceLastPresence <= 2))
        {
            return PolityAnchoringKind.Anchored;
        }

        if (primarySettlement is not null ||
            polity.RegionalPresences.Any(presence => presence.Kind == PolityPresenceKind.Habitation && presence.MonthsSinceLastPresence <= 2))
        {
            return PolityAnchoringKind.SemiRooted;
        }

        if (polity.RegionalPresences.Any(presence => presence.Kind == PolityPresenceKind.Seasonal && presence.MonthsSinceLastPresence <= 6))
        {
            return PolityAnchoringKind.Seasonal;
        }

        return PolityAnchoringKind.Mobile;
    }

    private static PolityPresenceKind ResolvePresenceKind(PolityRegionalPresence presence, Settlement? settlement, int localPopulation)
    {
        if (presence.IsCurrent)
        {
            if (settlement?.Type == SettlementType.Village || presence.ConsecutiveMonthsPresent >= 18)
            {
                return PolityPresenceKind.Core;
            }

            if (settlement is not null || (presence.ConsecutiveMonthsPresent >= 8 && localPopulation >= 45))
            {
                return PolityPresenceKind.Habitation;
            }

            if (presence.ReturnCount >= 2 && presence.TotalMonthsPresent >= 6)
            {
                return PolityPresenceKind.Seasonal;
            }

            return PolityPresenceKind.PassingThrough;
        }

        if (settlement?.IsActive == true && settlement.Type == SettlementType.Village && presence.MonthsSinceLastPresence <= 6)
        {
            return PolityPresenceKind.Core;
        }

        if (settlement?.IsActive == true && presence.MonthsSinceLastPresence <= 6)
        {
            return PolityPresenceKind.Habitation;
        }

        if (presence.ReturnCount >= 2 && presence.MonthsSinceLastPresence <= 8)
        {
            return PolityPresenceKind.Seasonal;
        }

        return PolityPresenceKind.PassingThrough;
    }

    private static SettlementType? ResolveSettlementCandidate(Polity polity, PolityRegionalPresence presence, IReadOnlyList<PopulationGroup> localGroups)
    {
        var localPopulation = localGroups.Sum(group => group.Population);
        var averageFoodPressure = Average(localGroups, group => group.Pressures.FoodPressure);
        var averageWaterPressure = Average(localGroups, group => group.Pressures.WaterPressure);
        var averageThreatPressure = Average(localGroups, group => group.Pressures.ThreatPressure);
        var averageMigrationPressure = Average(localGroups, group => group.Pressures.MigrationPressure);
        var totalStoredFood = localGroups.Sum(group => group.StoredFood);

        if (presence.ConsecutiveMonthsPresent >= 18 &&
            localPopulation >= 60 &&
            averageMigrationPressure <= 40 &&
            averageFoodPressure <= 55 &&
            averageWaterPressure <= 55 &&
            averageThreatPressure <= 65 &&
            totalStoredFood >= Math.Max(20, localPopulation / 2))
        {
            return SettlementType.Village;
        }

        if (presence.ReturnCount >= 2 &&
            presence.TotalMonthsPresent >= 6 &&
            averageMigrationPressure <= 65 &&
            averageThreatPressure <= 75)
        {
            return SettlementType.SeasonalBase;
        }

        if (presence.ConsecutiveMonthsPresent >= 8 &&
            localPopulation >= 35 &&
            averageMigrationPressure <= 55 &&
            averageFoodPressure <= 65 &&
            averageWaterPressure <= 65)
        {
            return SettlementType.CampHub;
        }

        return null;
    }

    private static void DepositSettlementStores(Settlement settlement, IReadOnlyList<PopulationGroup> localGroups, IDictionary<string, PopulationGroup> groupsById)
    {
        var localPopulation = localGroups.Sum(group => group.Population);
        var hasFoodStorage = localGroups.Any(group => group.LearnedAdvancementIds.Contains(AdvancementCatalog.FoodStorageId));
        var targetReserve = settlement.Type switch
        {
            SettlementType.Village => Math.Max(40, localPopulation),
            SettlementType.CampHub => Math.Max(25, (int)Math.Round(localPopulation * 0.75, MidpointRounding.AwayFromZero)),
            _ => Math.Max(20, localPopulation / 2)
        };
        if (hasFoodStorage)
        {
            targetReserve = (int)Math.Round(targetReserve * AdvancementConstants.FoodStorageSettlementReserveMultiplier, MidpointRounding.AwayFromZero);
        }
        var availableCapacity = Math.Max(0, targetReserve - settlement.StoredFood);
        if (availableCapacity == 0)
        {
            return;
        }

        var localSurplus = Math.Max(0, localGroups.Sum(group => group.StoredFood) - Math.Max(10, localPopulation / 2));
        var deposit = Math.Min(availableCapacity, Math.Max(0, localSurplus / 3));
        if (deposit <= 0)
        {
            return;
        }

        var remaining = deposit;
        foreach (var group in localGroups
                     .OrderByDescending(group => group.StoredFood)
                     .ThenBy(group => group.Id, StringComparer.Ordinal))
        {
            if (remaining <= 0)
            {
                break;
            }

            var contribution = Math.Min(group.StoredFood / 3, remaining);
            if (contribution <= 0)
            {
                continue;
            }

            groupsById[group.Id].StoredFood -= contribution;
            settlement.StoredFood += contribution;
            remaining -= contribution;
        }
    }

    private static int ComputeSettlementPriority(Polity polity, Settlement settlement)
    {
        var typeScore = ScoreSettlementType(settlement.Type) * 100;
        var presenceScore = polity.RegionalPresences
            .Where(presence => string.Equals(presence.RegionId, settlement.RegionId, StringComparison.Ordinal))
            .Select(presence => ScorePresence(polity, presence, settlement))
            .FirstOrDefault();
        return typeScore + presenceScore + settlement.StoredFood;
    }

    private static int ScoreSettlementType(SettlementType type)
    {
        return type switch
        {
            SettlementType.Village => 3,
            SettlementType.CampHub => 2,
            _ => 1
        };
    }

    private static int Average(IReadOnlyList<PopulationGroup> groups, Func<PopulationGroup, int> selector)
    {
        if (groups.Count == 0)
        {
            return 0;
        }

        return (int)Math.Round(groups.Average(selector), MidpointRounding.AwayFromZero);
    }

    private static SettlementChange BuildChange(
        Polity polity,
        IReadOnlyDictionary<string, Region> regionsById,
        string regionId,
        Settlement? settlement,
        SettlementChangeKind kind,
        string message)
    {
        return new SettlementChange
        {
            PolityId = polity.Id,
            PolityName = polity.Name,
            RegionId = regionId,
            RegionName = ResolveRegionName(regionsById, regionId),
            SettlementId = settlement?.Id ?? string.Empty,
            SettlementName = settlement?.Name ?? string.Empty,
            Kind = kind,
            Message = message
        };
    }

    private static string BuildSettlementName(IReadOnlyDictionary<string, Region> regionsById, string regionId, SettlementType type)
    {
        var regionName = ResolveRegionName(regionsById, regionId);
        return type switch
        {
            SettlementType.Village => $"{regionName} Village",
            SettlementType.CampHub => $"{regionName} Camp",
            _ => $"{regionName} Base"
        };
    }

    private static string ResolveRegionName(IReadOnlyDictionary<string, Region> regionsById, string regionId)
    {
        return regionsById.TryGetValue(regionId, out var region) ? region.Name : regionId;
    }

    private static PopulationGroup CloneGroup(PopulationGroup group)
    {
        return new PopulationGroup
        {
            Id = group.Id,
            Name = group.Name,
            SpeciesId = group.SpeciesId,
            PolityId = group.PolityId,
            CurrentRegionId = group.CurrentRegionId,
            OriginRegionId = group.OriginRegionId,
            Population = group.Population,
            StoredFood = group.StoredFood,
            SubsistenceMode = group.SubsistenceMode,
            LastRegionId = group.LastRegionId,
            MonthsSinceLastMove = group.MonthsSinceLastMove,
            Pressures = new PressureState
            {
                FoodPressure = group.Pressures.FoodPressure,
                WaterPressure = group.Pressures.WaterPressure,
                ThreatPressure = group.Pressures.ThreatPressure,
                OvercrowdingPressure = group.Pressures.OvercrowdingPressure,
                MigrationPressure = group.Pressures.MigrationPressure
            },
            KnownRegionIds = new HashSet<string>(group.KnownRegionIds, StringComparer.Ordinal),
            KnownDiscoveryIds = new HashSet<string>(group.KnownDiscoveryIds, StringComparer.Ordinal),
            DiscoveryEvidence = group.DiscoveryEvidence.Clone(),
            LearnedAdvancementIds = new HashSet<string>(group.LearnedAdvancementIds, StringComparer.Ordinal),
            AdvancementEvidence = group.AdvancementEvidence.Clone()
        };
    }
}

public sealed class SettlementUpdateResult
{
    public SettlementUpdateResult(World world, IReadOnlyList<SettlementChange> changes)
    {
        World = world;
        Changes = changes;
    }

    public World World { get; }

    public IReadOnlyList<SettlementChange> Changes { get; }
}
