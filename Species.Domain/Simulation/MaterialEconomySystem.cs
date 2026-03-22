using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class MaterialEconomySystem
{
    public MaterialEconomyResult Run(World world)
    {
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var groupsById = world.PopulationGroups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var updatedPolities = new List<Polity>(world.Polities.Count);
        var changes = new List<MaterialEconomyChange>();

        foreach (var polity in world.Polities.OrderBy(polity => polity.Id, StringComparer.Ordinal))
        {
            var updatedPolity = polity.Clone();
            var memberGroups = updatedPolity.MemberGroupIds
                .Select(groupsById.GetValueOrDefault)
                .Where(group => group is not null)
                .Cast<PopulationGroup>()
                .ToArray();
            if (memberGroups.Length == 0)
            {
                updatedPolities.Add(updatedPolity);
                continue;
            }

            var previousShortageMonths = updatedPolity.MaterialShortageMonths;
            var previousSurplusMonths = updatedPolity.MaterialSurplusMonths;
            var polityExtraction = new MaterialStockpile();
            var activeSettlements = updatedPolity.Settlements.Where(settlement => settlement.IsActive).ToArray();
            var settlementsByRegionId = activeSettlements.ToDictionary(settlement => settlement.RegionId, StringComparer.Ordinal);
            var activeLawIds = updatedPolity.EnactedLaws
                .Where(law => law.IsActive)
                .Select(law => law.DefinitionId)
                .ToHashSet(StringComparer.Ordinal);
            var knownDiscoveryIds = memberGroups.SelectMany(group => group.KnownDiscoveryIds).ToHashSet(StringComparer.Ordinal);
            var learnedAdvancementIds = memberGroups.SelectMany(group => group.LearnedAdvancementIds).ToHashSet(StringComparer.Ordinal);

            foreach (var presence in updatedPolity.RegionalPresences
                         .Where(IsUsablePresence)
                         .OrderByDescending(presence => presence.IsCurrent)
                         .ThenByDescending(presence => presence.Kind)
                         .ThenBy(presence => presence.RegionId, StringComparer.Ordinal))
            {
                if (!regionsById.TryGetValue(presence.RegionId, out var region))
                {
                    continue;
                }

                var localPopulation = memberGroups
                    .Where(group => string.Equals(group.CurrentRegionId, presence.RegionId, StringComparison.Ordinal))
                    .Sum(group => group.Population);
                if (localPopulation <= 0 && !settlementsByRegionId.ContainsKey(presence.RegionId))
                {
                    continue;
                }

                var extracted = ExtractForRegion(region, presence, localPopulation, knownDiscoveryIds, learnedAdvancementIds, activeLawIds);
                if (extracted.Total <= 0)
                {
                    continue;
                }

                polityExtraction = Merge(polityExtraction, extracted);
                if (settlementsByRegionId.TryGetValue(region.Id, out var settlement))
                {
                    DepositToSettlement(settlement, extracted);
                }
                else
                {
                    DepositToPolity(updatedPolity, extracted);
                }
            }

            foreach (var settlement in activeSettlements)
            {
                RebalanceSettlementStores(settlement, updatedPolity.MaterialStores, activeLawIds);
                ApplySettlementCondition(settlement, memberGroups, changes, updatedPolity, regionsById);
            }

            updatedPolity.MaterialProduction = BuildProductionState(updatedPolity, memberGroups, activeLawIds);
            ApplyStoreDecay(updatedPolity.MaterialStores, updatedPolity.MaterialProduction);

            var hasDeficit = HasPolityWideSupportShortage(updatedPolity.MaterialProduction, activeSettlements);
            var hasSurplus = !hasDeficit && HasPolityWideSupportSurplus(updatedPolity.MaterialProduction, activeSettlements);
            ApplyPolitySupportHistory(updatedPolity, hasDeficit, hasSurplus);

            if (polityExtraction.Total >= 8 && activeSettlements.Length > 0)
            {
                var bestSettlement = activeSettlements
                    .OrderByDescending(settlement => settlement.MaterialStores.Total)
                    .ThenByDescending(settlement => settlement.StoredFood)
                    .ThenBy(settlement => settlement.Id, StringComparer.Ordinal)
                    .First();
                changes.Add(BuildChange(
                    updatedPolity,
                    bestSettlement,
                    regionsById.GetValueOrDefault(bestSettlement.RegionId),
                    MaterialEconomyChangeKind.ExtractionSecured,
                    $"{bestSettlement.Name} secured {SummarizeTopResource(polityExtraction)} stores.",
                    polityExtraction,
                    updatedPolity.MaterialProduction));
            }

            if (updatedPolity.MaterialProduction.StorageSupport >= 50 &&
                previousSurplusMonths == 0 &&
                updatedPolity.MaterialSurplusMonths >= 1)
            {
                var site = ResolvePrimarySite(updatedPolity);
                changes.Add(BuildChange(
                    updatedPolity,
                    site,
                    site is null ? null : regionsById.GetValueOrDefault(site.RegionId),
                    MaterialEconomyChangeKind.StorageImproved,
                    $"{(site?.Name ?? updatedPolity.Name)} improved storage with local clay and fiber.",
                    polityExtraction,
                    updatedPolity.MaterialProduction));
            }

            if (updatedPolity.MaterialShortageMonths >= 2 && previousShortageMonths < 2)
            {
                var site = ResolvePrimarySite(updatedPolity);
                changes.Add(BuildChange(
                    updatedPolity,
                    site,
                    site is null ? null : regionsById.GetValueOrDefault(site.RegionId),
                    MaterialEconomyChangeKind.Shortage,
                    $"{(site?.Name ?? updatedPolity.Name)} weakened as practical support fell behind local needs.",
                    polityExtraction,
                    updatedPolity.MaterialProduction));
            }

            if (updatedPolity.MaterialSurplusMonths >= 2 && previousSurplusMonths < 2)
            {
                var site = ResolvePrimarySite(updatedPolity);
                changes.Add(BuildChange(
                    updatedPolity,
                    site,
                    site is null ? null : regionsById.GetValueOrDefault(site.RegionId),
                    MaterialEconomyChangeKind.Stabilized,
                    $"{updatedPolity.Name} stabilized through strong harvests and local materials.",
                    polityExtraction,
                    updatedPolity.MaterialProduction));
            }

            if (updatedPolity.MaterialShortageMonths >= 4 && activeSettlements.Any(settlement => settlement.MaterialSupport <= 30))
            {
                var site = activeSettlements
                    .OrderBy(settlement => settlement.MaterialSupport)
                    .ThenBy(settlement => settlement.Id, StringComparer.Ordinal)
                    .First();
                changes.Add(BuildChange(
                    updatedPolity,
                    site,
                    regionsById.GetValueOrDefault(site.RegionId),
                    MaterialEconomyChangeKind.Contraction,
                    $"Persistent support weakness forced contraction at {site.Name}.",
                    polityExtraction,
                    updatedPolity.MaterialProduction));
            }

            updatedPolities.Add(updatedPolity);
        }

        return new MaterialEconomyResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, world.PopulationGroups, world.Chronicle, updatedPolities, world.FocalPolityId),
            changes);
    }

    public static RegionMaterialProfile BuildRegionMaterialProfile(Region region)
    {
        var floraTotal = region.Ecosystem.FloraPopulations.Values.Sum(value => (long)value);
        var faunaTotal = region.Ecosystem.FaunaPopulations.Values.Sum(value => (long)value);
        var waterModifier = region.WaterAvailability switch
        {
            WaterAvailability.High => 1.15,
            WaterAvailability.Medium => 1.0,
            _ => 0.8
        };
        var fertilityFactor = 0.65 + (region.Fertility * 0.9);
        var timber = ScoreBiome(region.Biome, 12, 6, 4, 2, 1) + Scale(floraTotal, 0.0035 * waterModifier);
        var stone = ScoreBiome(region.Biome, 3, 5, 11, 12, 2) + Scale(1.0 - region.Fertility, 10.0);
        var fiber = ScoreBiome(region.Biome, 8, 7, 5, 2, 2) + Scale(floraTotal, 0.0026 * waterModifier);
        var clay = ScoreBiome(region.Biome, 5, 6, 4, 3, 3) + Scale(fertilityFactor, 8.0) + (region.WaterAvailability == WaterAvailability.High ? 3 : 0);
        var hides = ScoreBiome(region.Biome, 4, 5, 4, 6, 3) + Scale(faunaTotal, 0.0022);

        return new RegionMaterialProfile
        {
            Opportunities = new MaterialStockpile
            {
                Timber = Math.Max(0, timber),
                Stone = Math.Max(0, stone),
                Fiber = Math.Max(0, fiber),
                Clay = Math.Max(0, clay),
                Hides = Math.Max(0, hides)
            },
            ShelterPotential = Math.Max(0, timber + stone / 2),
            StoragePotential = Math.Max(0, clay + fiber / 2),
            ToolPotential = Math.Max(0, stone + timber / 3),
            TextilePotential = Math.Max(0, fiber + hides / 2),
            HidePotential = Math.Max(0, hides)
        };
    }

    private static bool IsUsablePresence(PolityRegionalPresence presence)
    {
        return presence.IsCurrent ||
               presence.Kind is PolityPresenceKind.Core or PolityPresenceKind.Habitation ||
               (presence.Kind == PolityPresenceKind.Seasonal && presence.MonthsSinceLastPresence <= 2);
    }

    private static MaterialStockpile ExtractForRegion(
        Region region,
        PolityRegionalPresence presence,
        int localPopulation,
        IReadOnlySet<string> knownDiscoveryIds,
        IReadOnlySet<string> learnedAdvancementIds,
        IReadOnlySet<string> activeLawIds)
    {
        var laborFactor = Math.Clamp(localPopulation / 30.0, 0.4, 2.0);
        var presenceFactor = presence.Kind switch
        {
            PolityPresenceKind.Core => 1.00,
            PolityPresenceKind.Habitation => 0.80,
            PolityPresenceKind.Seasonal => 0.55,
            _ => 0.35
        };
        var continuityFactor = 0.8 + Math.Clamp(presence.TotalMonthsPresent / 24.0, 0.0, 0.45);
        var knowledgeFactor = knownDiscoveryIds.Count > 0 ? 1.05 : 0.95;
        var advancementFactor = learnedAdvancementIds.Contains(AdvancementCatalog.StoneToolmakingId)
            ? AdvancementConstants.StoneToolmakingMaterialExtractionMultiplier
            : 1.00f;
        var lawFactor = 1.0;
        if (activeLawIds.Contains(GovernanceLawCatalog.ExtractionObligationId))
        {
            lawFactor += 0.18;
        }

        if (activeLawIds.Contains(GovernanceLawCatalog.EaseExtractionBurdenId))
        {
            lawFactor -= 0.12;
        }

        var profile = region.MaterialProfile;

        var multiplier = laborFactor * presenceFactor * continuityFactor * knowledgeFactor * advancementFactor * lawFactor;
        return new MaterialStockpile
        {
            Timber = Scale(profile.Opportunities.Timber, 0.14 * multiplier),
            Stone = Scale(profile.Opportunities.Stone, 0.12 * multiplier),
            Fiber = Scale(profile.Opportunities.Fiber, 0.13 * multiplier),
            Clay = Scale(profile.Opportunities.Clay, 0.11 * multiplier),
            Hides = Scale(profile.Opportunities.Hides, 0.10 * multiplier)
        };
    }

    private static void DepositToSettlement(Settlement settlement, MaterialStockpile extracted)
    {
        settlement.MaterialStores.Timber += extracted.Timber;
        settlement.MaterialStores.Stone += extracted.Stone;
        settlement.MaterialStores.Fiber += extracted.Fiber;
        settlement.MaterialStores.Clay += extracted.Clay;
        settlement.MaterialStores.Hides += extracted.Hides;
    }

    private static void DepositToPolity(Polity polity, MaterialStockpile extracted)
    {
        polity.MaterialStores.Timber += extracted.Timber;
        polity.MaterialStores.Stone += extracted.Stone;
        polity.MaterialStores.Fiber += extracted.Fiber;
        polity.MaterialStores.Clay += extracted.Clay;
        polity.MaterialStores.Hides += extracted.Hides;
    }

    private static void RebalanceSettlementStores(Settlement settlement, MaterialStockpile polityStores, IReadOnlySet<string> activeLawIds)
    {
        var transferToPolity = settlement.MaterialStores.Total / 4;
        if (activeLawIds.Contains(GovernanceLawCatalog.CentralizeStoresId))
        {
            transferToPolity = Math.Max(transferToPolity, settlement.MaterialStores.Total / 3);
        }

        if (activeLawIds.Contains(GovernanceLawCatalog.LocalStoreAutonomyId))
        {
            transferToPolity = Math.Max(0, settlement.MaterialStores.Total / 6);
        }

        if (transferToPolity <= 0)
        {
            return;
        }

        MoveShare(settlement.MaterialStores, polityStores, MaterialResource.Timber, transferToPolity);
        MoveShare(settlement.MaterialStores, polityStores, MaterialResource.Stone, transferToPolity);
        MoveShare(settlement.MaterialStores, polityStores, MaterialResource.Fiber, transferToPolity);
        MoveShare(settlement.MaterialStores, polityStores, MaterialResource.Clay, transferToPolity);
        MoveShare(settlement.MaterialStores, polityStores, MaterialResource.Hides, transferToPolity);
    }

    private static void ApplySettlementCondition(
        Settlement settlement,
        IReadOnlyList<PopulationGroup> memberGroups,
        ICollection<MaterialEconomyChange> changes,
        Polity polity,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        var previousShortageMonths = settlement.MaterialShortageMonths;
        var previousSurplusMonths = settlement.MaterialSurplusMonths;
        var localPopulation = memberGroups
            .Where(group => string.Equals(group.CurrentRegionId, settlement.RegionId, StringComparison.Ordinal))
            .Sum(group => group.Population);
        var requirement = settlement.Type switch
        {
            SettlementType.Village => Math.Max(12, localPopulation / 5),
            SettlementType.CampHub => Math.Max(8, localPopulation / 6),
            _ => Math.Max(6, localPopulation / 8)
        };
        var storeValue = settlement.MaterialStores.Timber +
                         settlement.MaterialStores.Stone +
                         settlement.MaterialStores.Fiber +
                         settlement.MaterialStores.Clay +
                         settlement.MaterialStores.Hides;
        settlement.MaterialSupport = Math.Clamp((int)Math.Round((storeValue / (double)Math.Max(1, requirement)) * 45.0, MidpointRounding.AwayFromZero), 0, 100);

        if (settlement.MaterialSupport >= 60)
        {
            settlement.MaterialSurplusMonths++;
            settlement.MaterialShortageMonths = 0;
        }
        else if (settlement.MaterialSupport <= 30)
        {
            settlement.MaterialShortageMonths++;
            settlement.MaterialSurplusMonths = 0;
        }
        else
        {
            settlement.MaterialShortageMonths = 0;
            settlement.MaterialSurplusMonths = 0;
        }

        if (settlement.MaterialShortageMonths == 2 && previousShortageMonths < 2)
        {
            changes.Add(BuildChange(
                polity,
                settlement,
                regionsById.GetValueOrDefault(settlement.RegionId),
                MaterialEconomyChangeKind.Shortage,
                $"{settlement.Name} is short of basic materials.",
                settlement.MaterialStores,
                polity.MaterialProduction));
        }

        if (settlement.MaterialSurplusMonths == 2 && previousSurplusMonths < 2)
        {
            changes.Add(BuildChange(
                polity,
                settlement,
                regionsById.GetValueOrDefault(settlement.RegionId),
                MaterialEconomyChangeKind.Stabilized,
                $"{settlement.Name} built up dependable local stores.",
                settlement.MaterialStores,
                polity.MaterialProduction));
        }
    }

    private static MaterialProductionState BuildProductionState(Polity polity, IReadOnlyList<PopulationGroup> memberGroups, IReadOnlySet<string> activeLawIds)
    {
        var allStores = polity.MaterialStores.Clone();
        var learnedAdvancementIds = memberGroups
            .SelectMany(group => group.LearnedAdvancementIds)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var settlement in polity.Settlements.Where(settlement => settlement.IsActive))
        {
            allStores.Timber += settlement.MaterialStores.Timber;
            allStores.Stone += settlement.MaterialStores.Stone;
            allStores.Fiber += settlement.MaterialStores.Fiber;
            allStores.Clay += settlement.MaterialStores.Clay;
            allStores.Hides += settlement.MaterialStores.Hides;
        }

        var totalPopulation = Math.Max(1, memberGroups.Sum(group => group.Population));
        var shelterSupport = Math.Clamp((int)Math.Round(((allStores.Timber * 1.2) + allStores.Stone) / totalPopulation * 20.0, MidpointRounding.AwayFromZero), 0, 100);
        var storageSupport = Math.Clamp((int)Math.Round(((allStores.Clay * 1.4) + (allStores.Fiber * 0.8)) / totalPopulation * 22.0, MidpointRounding.AwayFromZero), 0, 100);
        var toolSupport = Math.Clamp((int)Math.Round(((allStores.Stone * 1.3) + (allStores.Timber * 0.4)) / totalPopulation * 18.0, MidpointRounding.AwayFromZero), 0, 100);
        var textileSupport = Math.Clamp((int)Math.Round(((allStores.Fiber * 1.1) + (allStores.Hides * 1.0)) / totalPopulation * 20.0, MidpointRounding.AwayFromZero), 0, 100);

        if (learnedAdvancementIds.Contains(AdvancementCatalog.FoodStorageId))
        {
            storageSupport = Math.Clamp((int)Math.Round(storageSupport * 1.10, MidpointRounding.AwayFromZero), 0, 100);
        }

        if (learnedAdvancementIds.Contains(AdvancementCatalog.HideWorkingId))
        {
            textileSupport = Math.Clamp((int)Math.Round(textileSupport + AdvancementConstants.HideWorkingTextileSupportBonus, MidpointRounding.AwayFromZero), 0, 100);
        }

        if (learnedAdvancementIds.Contains(AdvancementCatalog.FiberWorkingId))
        {
            textileSupport = Math.Clamp((int)Math.Round(textileSupport + AdvancementConstants.FiberWorkingTextileSupportBonus, MidpointRounding.AwayFromZero), 0, 100);
        }

        if (activeLawIds.Contains(GovernanceLawCatalog.CentralizeStoresId))
        {
            storageSupport = Math.Clamp(storageSupport + 6, 0, 100);
        }

        if (activeLawIds.Contains(GovernanceLawCatalog.LocalStoreAutonomyId))
        {
            storageSupport = Math.Clamp(storageSupport - 3, 0, 100);
            textileSupport = Math.Clamp(textileSupport + 4, 0, 100);
        }

        if (activeLawIds.Contains(GovernanceLawCatalog.ExtractionObligationId))
        {
            toolSupport = Math.Clamp(toolSupport + 5, 0, 100);
        }

        if (activeLawIds.Contains(GovernanceLawCatalog.FrontierIntegrationId))
        {
            shelterSupport = Math.Clamp(shelterSupport + 4, 0, 100);
        }

        var resilienceBonus = Math.Clamp((int)Math.Round((shelterSupport * 0.35) + (storageSupport * 0.20) + (toolSupport * 0.25) + (textileSupport * 0.20), MidpointRounding.AwayFromZero), 0, 30);
        var surplusScore = Math.Clamp((int)Math.Round((allStores.Total / (double)totalPopulation) * 25.0 + (storageSupport * 0.25), MidpointRounding.AwayFromZero), 0, 100);
        var deficitScore = Math.Clamp(100 - (int)Math.Round((shelterSupport * 0.35) + (storageSupport * 0.20) + (toolSupport * 0.25) + (textileSupport * 0.20), MidpointRounding.AwayFromZero), 0, 100);

        return new MaterialProductionState
        {
            ShelterSupport = shelterSupport,
            StorageSupport = storageSupport,
            ToolSupport = toolSupport,
            TextileSupport = textileSupport,
            ResilienceBonus = resilienceBonus,
            SurplusScore = surplusScore,
            DeficitScore = deficitScore,
            ConditionSummary = deficitScore >= 65
                ? "Practical support is falling behind the polity's needs."
                : surplusScore >= 45
                    ? "Practical support is helping hold living conditions steady."
                    : "Practical support is mixed but still workable."
        };
    }

    private static bool HasPolityWideSupportShortage(MaterialProductionState production, IReadOnlyList<Settlement> activeSettlements)
    {
        var averageSupport = GetAverageSupport(production);
        var weakSupportCategories = CountSupportsAtOrBelow(production, 40);
        var severeSupportCategories = CountSupportsAtOrBelow(production, 30);
        var weakSettlements = activeSettlements.Count(settlement => settlement.MaterialSupport <= 35);
        var majorityWeakSettlements = activeSettlements.Count > 0 && weakSettlements >= Math.Max(1, (activeSettlements.Count + 1) / 2);

        return production.DeficitScore >= 55 &&
               (severeSupportCategories >= 1 ||
                weakSupportCategories >= 2 ||
                averageSupport <= 45 ||
                majorityWeakSettlements);
    }

    private static bool HasPolityWideSupportSurplus(MaterialProductionState production, IReadOnlyList<Settlement> activeSettlements)
    {
        var averageSupport = GetAverageSupport(production);
        var strongSupportCategories = CountSupportsAtOrAbove(production, 55);
        var weakSupportCategories = CountSupportsAtOrBelow(production, 35);
        var strongSettlements = activeSettlements.Count(settlement => settlement.MaterialSupport >= 60);
        var majorityStrongSettlements = activeSettlements.Count == 0 || strongSettlements >= Math.Max(1, (activeSettlements.Count + 1) / 2);

        return production.SurplusScore >= 30 &&
               averageSupport >= 58 &&
               strongSupportCategories >= 3 &&
               weakSupportCategories == 0 &&
               majorityStrongSettlements;
    }

    private static void ApplyPolitySupportHistory(Polity polity, bool hasDeficit, bool hasSurplus)
    {
        if (hasDeficit)
        {
            polity.MaterialShortageMonths++;
            polity.MaterialSurplusMonths = 0;
            return;
        }

        if (hasSurplus)
        {
            polity.MaterialSurplusMonths++;
            polity.MaterialShortageMonths = 0;
            return;
        }

        polity.MaterialShortageMonths = Math.Max(0, polity.MaterialShortageMonths - 1);
        polity.MaterialSurplusMonths = Math.Max(0, polity.MaterialSurplusMonths - 1);
    }

    private static int GetAverageSupport(MaterialProductionState production)
    {
        return (int)Math.Round(
            (production.ShelterSupport +
             production.StorageSupport +
             production.ToolSupport +
             production.TextileSupport) / 4.0,
            MidpointRounding.AwayFromZero);
    }

    private static int CountSupportsAtOrBelow(MaterialProductionState production, int threshold)
    {
        return CountSupports(production, value => value <= threshold);
    }

    private static int CountSupportsAtOrAbove(MaterialProductionState production, int threshold)
    {
        return CountSupports(production, value => value >= threshold);
    }

    private static int CountSupports(MaterialProductionState production, Func<int, bool> predicate)
    {
        var supports = new[]
        {
            production.ShelterSupport,
            production.StorageSupport,
            production.ToolSupport,
            production.TextileSupport
        };

        return supports.Count(predicate);
    }

    private static void ApplyStoreDecay(MaterialStockpile stores, MaterialProductionState production)
    {
        Consume(stores, MaterialResource.Timber, Math.Max(1, production.ShelterSupport / 30));
        Consume(stores, MaterialResource.Fiber, Math.Max(0, production.TextileSupport / 35));
        Consume(stores, MaterialResource.Clay, Math.Max(0, production.StorageSupport / 40));
        Consume(stores, MaterialResource.Stone, Math.Max(0, production.ToolSupport / 45));
        Consume(stores, MaterialResource.Hides, Math.Max(0, production.TextileSupport / 45));
    }

    private static void Consume(MaterialStockpile stores, MaterialResource resource, int amount)
    {
        stores.Set(resource, Math.Max(0, stores.Get(resource) - Math.Max(0, amount)));
    }

    private static void MoveShare(MaterialStockpile from, MaterialStockpile to, MaterialResource resource, int totalTransferTarget)
    {
        var current = from.Get(resource);
        if (current <= 0)
        {
            return;
        }

        var share = Math.Min(current, Math.Max(1, totalTransferTarget / 5));
        from.Set(resource, current - share);
        to.Add(resource, share);
    }

    private static MaterialStockpile Merge(MaterialStockpile first, MaterialStockpile second)
    {
        return new MaterialStockpile
        {
            Timber = first.Timber + second.Timber,
            Stone = first.Stone + second.Stone,
            Fiber = first.Fiber + second.Fiber,
            Clay = first.Clay + second.Clay,
            Hides = first.Hides + second.Hides
        };
    }

    private static Settlement? ResolvePrimarySite(Polity polity)
    {
        return polity.Settlements
            .Where(settlement => settlement.IsActive)
            .OrderByDescending(settlement => settlement.IsPrimary)
            .ThenByDescending(settlement => settlement.MaterialSupport)
            .ThenBy(settlement => settlement.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static MaterialEconomyChange BuildChange(
        Polity polity,
        Settlement? settlement,
        Region? region,
        MaterialEconomyChangeKind kind,
        string message,
        MaterialStockpile extracted,
        MaterialProductionState production)
    {
        return new MaterialEconomyChange
        {
            PolityId = polity.Id,
            PolityName = polity.Name,
            SettlementId = settlement?.Id ?? string.Empty,
            SettlementName = settlement?.Name ?? string.Empty,
            RegionId = region?.Id ?? settlement?.RegionId ?? polity.CoreRegionId,
            RegionName = region?.Name ?? settlement?.RegionId ?? polity.CoreRegionId,
            Kind = kind,
            Message = message,
            Extracted = extracted.Clone(),
            Production = production.Clone()
        };
    }

    private static string SummarizeTopResource(MaterialStockpile stores)
    {
        var top = stores.AsDictionary()
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key)
            .FirstOrDefault();
        return top.Value <= 0
            ? "material"
            : top.Key switch
            {
                MaterialResource.Timber => "timber",
                MaterialResource.Stone => "stone",
                MaterialResource.Fiber => "fiber",
                MaterialResource.Clay => "clay",
                MaterialResource.Hides => "hide",
                _ => "material"
            };
    }

    private static int ScoreBiome(Biome biome, int forest, int plains, int highlands, int desert, int wetlands)
    {
        return biome switch
        {
            Biome.Forest => forest,
            Biome.Plains => plains,
            Biome.Highlands => highlands,
            Biome.Desert => desert,
            Biome.Wetlands => wetlands,
            Biome.Tundra => Math.Max(1, plains - 2),
            _ => 2
        };
    }

    private static int Scale(double value, double multiplier)
    {
        return (int)Math.Round(value * multiplier, MidpointRounding.AwayFromZero);
    }
}
