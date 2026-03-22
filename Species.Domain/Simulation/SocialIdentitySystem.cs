using Species.Domain.Catalogs;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class SocialIdentitySystem
{
    private readonly SocialTraditionCatalog traditionCatalog = new();

    public SocialIdentityResult Run(World world)
    {
        if (world.Polities.Count == 0)
        {
            return new SocialIdentityResult(world, Array.Empty<SocialIdentityChange>());
        }

        var updatedPolities = new List<Polity>(world.Polities.Count);
        var changes = new List<SocialIdentityChange>();
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);

        foreach (var polity in world.Polities)
        {
            var updatedPolity = polity.Clone();
            var context = PolityData.BuildContext(world, updatedPolity);
            if (context is null)
            {
                updatedPolities.Add(updatedPolity);
                continue;
            }

            var coreRegion = regionsById.GetValueOrDefault(context.CoreRegionId);
            UpdateMemory(updatedPolity, context, coreRegion);
            var previousTraditions = updatedPolity.SocialIdentity.TraditionIds.ToHashSet(StringComparer.Ordinal);
            UpdateIdentity(updatedPolity, context, coreRegion);
            PromoteTraditions(updatedPolity, context, coreRegion, previousTraditions, changes);
            updatedPolities.Add(updatedPolity);
        }

        return new SocialIdentityResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, world.PopulationGroups, world.Chronicle, updatedPolities, world.FocalPolityId),
            changes);
    }

    private void UpdateMemory(Polity polity, PolityContext context, Region? coreRegion)
    {
        var activeSettlements = polity.Settlements.Where(settlement => settlement.IsActive).ToArray();
        var currentPresences = polity.RegionalPresences.Where(presence => presence.IsCurrent).ToArray();
        var frontierPresences = currentPresences.Count(presence => !string.Equals(presence.RegionId, context.CoreRegionId, StringComparison.Ordinal));
        var seasonalUse = currentPresences.Count(presence => presence.Kind == PolityPresenceKind.Seasonal);
        var hardship = context.Pressures.Food.EffectiveValue >= 55 ||
                       context.MaterialShortageMonths >= 2 ||
                       context.Pressures.Migration.EffectiveValue >= 60;
        var surplus = context.MaterialSurplusMonths >= 2 &&
                      context.TotalStoredFood > Math.Max(1, context.TotalPopulation / 2);
        var coordinated = context.Governance.Governability >= 60 &&
                          context.Governance.Authority >= 55 &&
                          polity.EnactedLaws.Any(law => law.IsActive && law.ComplianceLevel >= 55);

        polity.SocialMemory.SettlementContinuityMonths = activeSettlements.Length > 0 &&
                                                         context.AnchoringKind is PolityAnchoringKind.SemiRooted or PolityAnchoringKind.Anchored
            ? polity.SocialMemory.SettlementContinuityMonths + 1
            : 0;
        polity.SocialMemory.SeasonalMobilityMonths = context.AnchoringKind is PolityAnchoringKind.Mobile or PolityAnchoringKind.Seasonal ||
                                                     seasonalUse > 0 ||
                                                     (activeSettlements.Length == 0 && currentPresences.Length > 1)
            ? polity.SocialMemory.SeasonalMobilityMonths + 1
            : Math.Max(0, polity.SocialMemory.SeasonalMobilityMonths - 1);
        polity.SocialMemory.HardshipMonths = hardship
            ? polity.SocialMemory.HardshipMonths + 1
            : Math.Max(0, polity.SocialMemory.HardshipMonths - 1);
        polity.SocialMemory.SurplusMonths = surplus
            ? polity.SocialMemory.SurplusMonths + 1
            : Math.Max(0, polity.SocialMemory.SurplusMonths - 1);
        polity.SocialMemory.CoordinatedGovernanceMonths = coordinated
            ? polity.SocialMemory.CoordinatedGovernanceMonths + 1
            : Math.Max(0, polity.SocialMemory.CoordinatedGovernanceMonths - 1);
        polity.SocialMemory.PeripheralStrainMonths = context.Governance.PeripheralStrain >= 35 || frontierPresences > 0
            ? polity.SocialMemory.PeripheralStrainMonths + 1
            : Math.Max(0, polity.SocialMemory.PeripheralStrainMonths - 1);
        polity.SocialMemory.RiverSettlementMonths = coreRegion?.WaterAvailability == WaterAvailability.High && activeSettlements.Length > 0
            ? polity.SocialMemory.RiverSettlementMonths + 1
            : Math.Max(0, polity.SocialMemory.RiverSettlementMonths - 1);
        polity.SocialMemory.FrontierExposureMonths = frontierPresences > 0 ||
                                                     activeSettlements.Any(settlement => !string.Equals(settlement.RegionId, context.CoreRegionId, StringComparison.Ordinal))
            ? polity.SocialMemory.FrontierExposureMonths + 1
            : Math.Max(0, polity.SocialMemory.FrontierExposureMonths - 1);
    }

    private static void UpdateIdentity(Polity polity, PolityContext context, Region? coreRegion)
    {
        var activeSettlements = polity.Settlements.Count(settlement => settlement.IsActive);
        var currentPresences = polity.RegionalPresences.Count(presence => presence.IsCurrent);
        var frontierExposure = polity.SocialMemory.FrontierExposureMonths;

        polity.SocialIdentity.Rootedness = Clamp(
            (polity.SocialMemory.SettlementContinuityMonths * 4) +
            (polity.SocialMemory.RiverSettlementMonths * 3) +
            (activeSettlements * 8) +
            (context.AnchoringKind == PolityAnchoringKind.Anchored ? 18 : context.AnchoringKind == PolityAnchoringKind.SemiRooted ? 10 : 0) -
            (polity.SocialMemory.SeasonalMobilityMonths * 2));

        polity.SocialIdentity.Mobility = Clamp(
            (polity.SocialMemory.SeasonalMobilityMonths * 5) +
            (currentPresences * 5) +
            (context.AnchoringKind == PolityAnchoringKind.Mobile ? 18 : context.AnchoringKind == PolityAnchoringKind.Seasonal ? 12 : 0) -
            (polity.SocialMemory.SettlementContinuityMonths * 2));

        polity.SocialIdentity.Communalism = Clamp(
            (polity.SocialMemory.SurplusMonths * 4) +
            (polity.SocialMemory.SettlementContinuityMonths * 2) +
            (context.Governance.Legitimacy / 3) +
            (HasLaw(polity, GovernanceLawCatalog.SharedGovernanceId) ? 12 : 0) +
            (HasTradition(polity, SocialTraditionCatalog.StorageDisciplineId) ? 8 : 0));

        polity.SocialIdentity.AutonomyOrientation = Clamp(
            (polity.SocialMemory.PeripheralStrainMonths * 4) +
            (frontierExposure * 3) +
            (polity.SocialIdentity.Mobility / 4) +
            (HasLaw(polity, GovernanceLawCatalog.LocalStoreAutonomyId) ? 12 : 0) +
            (HasLaw(polity, GovernanceLawCatalog.OpenMovementId) ? 10 : 0) -
            (HasLaw(polity, GovernanceLawCatalog.StrengthenCentralAuthorityId) ? 8 : 0));

        polity.SocialIdentity.OrderOrientation = Clamp(
            (polity.SocialMemory.CoordinatedGovernanceMonths * 5) +
            (context.Governance.Authority / 2) +
            (HasLaw(polity, GovernanceLawCatalog.StrengthenCentralAuthorityId) ? 10 : 0) +
            (HasLaw(polity, GovernanceLawCatalog.CentralizeStoresId) ? 8 : 0) +
            (HasTradition(polity, SocialTraditionCatalog.CoordinatedOrderId) ? 10 : 0) -
            (polity.SocialIdentity.AutonomyOrientation / 5));

        polity.SocialIdentity.FrontierDistinctiveness = Clamp(
            (frontierExposure * 5) +
            (polity.SocialMemory.PeripheralStrainMonths * 4) +
            (currentPresences > 1 ? 8 : 0) +
            (coreRegion is not null && coreRegion.WaterAvailability == WaterAvailability.High ? 4 : 0));
    }

    private void PromoteTraditions(
        Polity polity,
        PolityContext context,
        Region? coreRegion,
        IReadOnlySet<string> previousTraditions,
        ICollection<SocialIdentityChange> changes)
    {
        TryUnlock(polity, previousTraditions, changes, SocialTraditionCatalog.StorageDisciplineId,
            polity.SocialMemory.HardshipMonths >= 6 &&
            (polity.SocialMemory.SurplusMonths >= 2 || context.MaterialProduction.StorageSupport >= 45));

        TryUnlock(polity, previousTraditions, changes, SocialTraditionCatalog.SeasonalReturnId,
            polity.SocialMemory.SeasonalMobilityMonths >= 8 &&
            polity.SocialIdentity.Mobility >= 55);

        TryUnlock(polity, previousTraditions, changes, SocialTraditionCatalog.FrontierAutonomyId,
            polity.SocialMemory.FrontierExposureMonths >= 6 &&
            polity.SocialMemory.PeripheralStrainMonths >= 4 &&
            polity.SocialIdentity.AutonomyOrientation >= 55);

        TryUnlock(polity, previousTraditions, changes, SocialTraditionCatalog.RootedHeartlandId,
            polity.SocialMemory.SettlementContinuityMonths >= 12 &&
            polity.SocialIdentity.Rootedness >= 60 &&
            (coreRegion?.WaterAvailability == WaterAvailability.High || coreRegion?.Fertility >= 0.60));

        TryUnlock(polity, previousTraditions, changes, SocialTraditionCatalog.CoordinatedOrderId,
            polity.SocialMemory.CoordinatedGovernanceMonths >= 6 &&
            polity.SocialIdentity.OrderOrientation >= 60 &&
            context.Governance.Governability >= 60);
    }

    private void TryUnlock(
        Polity polity,
        IReadOnlySet<string> previousTraditions,
        ICollection<SocialIdentityChange> changes,
        string traditionId,
        bool eligible)
    {
        if (!eligible || polity.SocialIdentity.TraditionIds.Contains(traditionId, StringComparer.Ordinal))
        {
            return;
        }

        polity.SocialIdentity.TraditionIds.Add(traditionId);
        if (previousTraditions.Contains(traditionId))
        {
            return;
        }

        var definition = traditionCatalog.GetById(traditionId);
        if (definition is null)
        {
            return;
        }

        changes.Add(new SocialIdentityChange
        {
            PolityId = polity.Id,
            PolityName = polity.Name,
            TraditionId = definition.Id,
            TraditionName = definition.Name,
            Message = string.Format(definition.IdentityChangeTemplate, polity.Name)
        });
    }

    private static bool HasLaw(Polity polity, string lawId)
    {
        return polity.EnactedLaws.Any(law => law.IsActive && string.Equals(law.DefinitionId, lawId, StringComparison.Ordinal));
    }

    private static bool HasTradition(Polity polity, string traditionId)
    {
        return polity.SocialIdentity.TraditionIds.Contains(traditionId, StringComparer.Ordinal);
    }

    private static int Clamp(int value)
    {
        return Math.Clamp(value, 0, 100);
    }
}
