using Species.Domain.Catalogs;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class EnactedLawSystem
{
    private readonly IReadOnlyDictionary<string, EnactedLawEffect> effectsByDefinitionId =
        new Dictionary<string, EnactedLawEffect>(StringComparer.Ordinal)
        {
            [GovernanceLawCatalog.CentralizeStoresId] = new(-5, 0, 3, 2, 4),
            [GovernanceLawCatalog.LocalStoreAutonomyId] = new(0, 0, -1, -2, -4),
            [GovernanceLawCatalog.ExtractionObligationId] = new(4, 0, 2, 3, 2),
            [GovernanceLawCatalog.EaseExtractionBurdenId] = new(1, 0, -1, -2, -2),
            [GovernanceLawCatalog.CrisisMovementRestrictionId] = new(0, 0, -5, 1, -8),
            [GovernanceLawCatalog.OpenMovementId] = new(0, 0, 1, 0, 4),
            [GovernanceLawCatalog.StrengthenCentralAuthorityId] = new(0, 0, -4, 1, 2),
            [GovernanceLawCatalog.SharedGovernanceId] = new(-1, 0, -2, -2, -1),
            [GovernanceLawCatalog.FrontierIntegrationId] = new(-2, 0, -1, -1, -3)
        };

    public World Run(World world)
    {
        if (world.PopulationGroups.Count == 0 || world.Polities.Count == 0)
        {
            return world;
        }

        var updatedPolities = new List<Polity>(world.Polities.Count);
        var polityStates = new Dictionary<string, Polity>(StringComparer.Ordinal);

        foreach (var polity in world.Polities)
        {
            var updatedPolity = polity.Clone();
            var context = PolityData.BuildContext(world, updatedPolity);
            if (context is not null)
            {
                UpdateGovernance(updatedPolity, context);
                UpdateLawEffectiveness(updatedPolity, context);
            }

            polityStates[updatedPolity.Id] = updatedPolity;
            updatedPolities.Add(updatedPolity);
        }

        var updatedGroups = world.PopulationGroups
            .Select(group => ApplyLawEffects(group, polityStates.GetValueOrDefault(group.PolityId)))
            .ToArray();

        return new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, updatedPolities, world.FocalPolityId);
    }

    private void UpdateGovernance(Polity polity, PolityContext context)
    {
        var behavior = GovernmentFormLawBehaviorCatalog.Get(polity.GovernmentForm);
        var activeLaws = polity.EnactedLaws.Where(law => law.IsActive).ToArray();
        var activeSettlementCount = polity.Settlements.Count(settlement => settlement.IsActive);
        var peripheralSiteCount = polity.Settlements.Count(settlement => settlement.IsActive && !string.Equals(settlement.RegionId, context.CoreRegionId, StringComparison.Ordinal));
        var peripheralPresenceCount = polity.RegionalPresences.Count(presence => presence.IsCurrent && !string.Equals(presence.RegionId, context.CoreRegionId, StringComparison.Ordinal));
        var shortagePressure = Math.Max(context.Pressures.Food.EffectiveValue, context.MaterialProduction.DeficitScore);
        var crisisPressure = Math.Max(context.Pressures.Threat.EffectiveValue, context.Pressures.Migration.EffectiveValue);
        var dissatisfiedBlocs = polity.PoliticalBlocs.Count(bloc => bloc.Satisfaction <= 40);

        var peripheralStrain = (peripheralSiteCount * 10) + (peripheralPresenceCount * 6) + (context.MaterialShortageMonths * 6);
        peripheralStrain += context.ExternalPressure.FrontierFriction / 2;
        peripheralStrain += context.ExternalPressure.RaidPressure / 3;
        peripheralStrain += context.ScaleState.DistanceStrain / 4;
        peripheralStrain += context.ScaleState.CompositeComplexity / 5;
        if (HasLaw(activeLaws, GovernanceLawCatalog.CentralizeStoresId) || HasLaw(activeLaws, GovernanceLawCatalog.ExtractionObligationId))
        {
            peripheralStrain += 10;
        }

        if (HasLaw(activeLaws, GovernanceLawCatalog.OpenMovementId) || HasLaw(activeLaws, GovernanceLawCatalog.FrontierIntegrationId))
        {
            peripheralStrain -= 8;
        }

        var legitimacy = behavior.LegitimacyBaseline
            - (shortagePressure / 5)
            - (dissatisfiedBlocs * 4)
            - (peripheralStrain / 6)
            - (context.ExternalPressure.RaidPressure / 6)
            - (context.ScaleState.OverextensionPressure / 8)
            + (context.SocialIdentity.Communalism / 10)
            + (HasLaw(activeLaws, GovernanceLawCatalog.SharedGovernanceId) ? 8 : 0)
            + (HasLaw(activeLaws, GovernanceLawCatalog.LocalStoreAutonomyId) ? 6 : 0)
            - (HasLaw(activeLaws, GovernanceLawCatalog.CrisisMovementRestrictionId) ? 6 : 0)
            - (HasLaw(activeLaws, GovernanceLawCatalog.ExtractionObligationId) ? 7 : 0);

        var cohesion = behavior.CohesionBaseline
            - (context.Pressures.Migration.EffectiveValue / 6)
            - (peripheralStrain / 7)
            - (context.ExternalPressure.RaidPressure / 8)
            - (context.ScaleState.FragmentationRisk / 8)
            + (context.SocialIdentity.Rootedness / 12)
            + (activeSettlementCount >= 1 ? 4 : 0)
            + (HasLaw(activeLaws, GovernanceLawCatalog.SharedGovernanceId) ? 6 : 0)
            + (HasLaw(activeLaws, GovernanceLawCatalog.FrontierIntegrationId) ? 8 : 0)
            - (HasLaw(activeLaws, GovernanceLawCatalog.StrengthenCentralAuthorityId) ? context.SocialIdentity.AutonomyOrientation / 14 : 0)
            - (HasLaw(activeLaws, GovernanceLawCatalog.StrengthenCentralAuthorityId) ? 4 : 0);

        var authority = behavior.AuthorityBaseline
            + (crisisPressure / 7)
            + (context.ExternalPressure.Threat / 8)
            + (context.ScaleState.Centralization / 10)
            + (context.SocialIdentity.OrderOrientation / 10)
            + (HasLaw(activeLaws, GovernanceLawCatalog.StrengthenCentralAuthorityId) ? 10 : 0)
            + (HasLaw(activeLaws, GovernanceLawCatalog.CentralizeStoresId) ? 6 : 0)
            + (HasLaw(activeLaws, GovernanceLawCatalog.CrisisMovementRestrictionId) ? 7 : 0)
            - (HasLaw(activeLaws, GovernanceLawCatalog.SharedGovernanceId) ? 5 : 0)
            - (Math.Max(0, 50 - legitimacy) / 3);

        polity.Governance.Legitimacy = Clamp(legitimacy);
        polity.Governance.Cohesion = Clamp(cohesion);
        polity.Governance.Authority = Clamp(authority);
        polity.Governance.PeripheralStrain = Clamp(peripheralStrain);
        polity.Governance.Governability = Clamp((int)Math.Round(
            (polity.Governance.Legitimacy * 0.40) +
            (polity.Governance.Cohesion * 0.30) +
            (polity.Governance.Authority * 0.30) -
            (polity.Governance.PeripheralStrain * 0.20),
            MidpointRounding.AwayFromZero));
    }

    private void UpdateLawEffectiveness(Polity polity, PolityContext context)
    {
        foreach (var enactedLaw in polity.EnactedLaws.Where(law => law.IsActive))
        {
            var targetEnforcement = ResolveTargetEnforcement(polity, context, enactedLaw);
            var targetCompliance = ResolveTargetCompliance(polity, context, enactedLaw, targetEnforcement);
            enactedLaw.EnforcementStrength = DriftToward(enactedLaw.EnforcementStrength, targetEnforcement);
            enactedLaw.ComplianceLevel = DriftToward(enactedLaw.ComplianceLevel, targetCompliance);
            enactedLaw.CoreEffectiveness = Clamp((int)Math.Round((enactedLaw.EnforcementStrength * 0.60) + (polity.Governance.Authority * 0.25) + (polity.Governance.Legitimacy * 0.15), MidpointRounding.AwayFromZero));
            enactedLaw.PeripheralEffectiveness = Clamp((int)Math.Round((enactedLaw.ComplianceLevel * 0.45) + (polity.Governance.Cohesion * 0.30) + (polity.Governance.Legitimacy * 0.25) - (polity.Governance.PeripheralStrain * 0.20), MidpointRounding.AwayFromZero));
            enactedLaw.ResistanceLevel = Clamp(100 - (int)Math.Round((enactedLaw.ComplianceLevel * 0.55) + (polity.Governance.Legitimacy * 0.20) + (polity.Governance.Cohesion * 0.25), MidpointRounding.AwayFromZero));
        }
    }

    private PopulationGroup ApplyLawEffects(PopulationGroup group, Polity? polity)
    {
        var updatedGroup = CloneGroup(group);
        if (polity is null)
        {
            return updatedGroup;
        }

        var total = new EnactedLawEffect(0, 0, 0, 0, 0);
        foreach (var enactedLaw in polity.EnactedLaws.Where(law => law.IsActive))
        {
            if (!effectsByDefinitionId.TryGetValue(enactedLaw.DefinitionId, out var effect))
            {
                continue;
            }

            var scale = ResolveEffectScale(enactedLaw, polity, updatedGroup);
            total = new EnactedLawEffect(
                total.FoodPressureModifier + Scale(effect.FoodPressureModifier, scale),
                total.WaterPressureModifier + Scale(effect.WaterPressureModifier, scale),
                total.ThreatPressureModifier + Scale(effect.ThreatPressureModifier, scale),
                total.OvercrowdingPressureModifier + Scale(effect.OvercrowdingPressureModifier, scale),
                total.MigrationPressureModifier + Scale(effect.MigrationPressureModifier, scale));
        }

        updatedGroup.Pressures = updatedGroup.Pressures.Clone();
        updatedGroup.Pressures.Food = PressureMath.ApplyRawAdjustment(PressureDefinitions.Food, updatedGroup.Pressures.Food, total.FoodPressureModifier);
        updatedGroup.Pressures.Water = PressureMath.ApplyRawAdjustment(PressureDefinitions.Water, updatedGroup.Pressures.Water, total.WaterPressureModifier);
        updatedGroup.Pressures.Threat = PressureMath.ApplyRawAdjustment(PressureDefinitions.Threat, updatedGroup.Pressures.Threat, total.ThreatPressureModifier);
        updatedGroup.Pressures.Overcrowding = PressureMath.ApplyRawAdjustment(PressureDefinitions.Overcrowding, updatedGroup.Pressures.Overcrowding, total.OvercrowdingPressureModifier);
        updatedGroup.Pressures.Migration = PressureMath.ApplyRawAdjustment(PressureDefinitions.Migration, updatedGroup.Pressures.Migration, total.MigrationPressureModifier);

        return updatedGroup;
    }

    private static int ResolveTargetEnforcement(Polity polity, PolityContext context, EnactedLaw law)
    {
        var behavior = GovernmentFormLawBehaviorCatalog.Get(polity.GovernmentForm);
        var baseline = behavior.EnforcementTendency + (polity.Governance.Authority / 8) + (context.Pressures.Threat.EffectiveValue / 10);
        baseline += context.ExternalPressure.Threat / 10;
        baseline += context.ScaleState.Centralization / 12;
        baseline -= Math.Max(0, 50 - polity.Governance.Legitimacy) / 4;
        baseline -= polity.Governance.PeripheralStrain / 10;
        baseline -= context.ScaleState.DistanceStrain / 10;
        baseline += context.SocialIdentity.OrderOrientation / 16;

        baseline += law.DefinitionId switch
        {
            GovernanceLawCatalog.CentralizeStoresId => 8,
            GovernanceLawCatalog.ExtractionObligationId => 6,
            GovernanceLawCatalog.CrisisMovementRestrictionId => 8,
            GovernanceLawCatalog.StrengthenCentralAuthorityId => 10,
            GovernanceLawCatalog.SharedGovernanceId => -4,
            GovernanceLawCatalog.LocalStoreAutonomyId => -3,
            GovernanceLawCatalog.OpenMovementId => -2,
            GovernanceLawCatalog.FrontierIntegrationId => 2,
            _ => 0
        };

        return Clamp(baseline);
    }

    private static int ResolveTargetCompliance(Polity polity, PolityContext context, EnactedLaw law, int enforcementStrength)
    {
        var behavior = GovernmentFormLawBehaviorCatalog.Get(polity.GovernmentForm);
        var baseline = behavior.ComplianceTendency + (polity.Governance.Legitimacy / 6) + (polity.Governance.Cohesion / 7);
        baseline += enforcementStrength / 10;
        baseline -= context.Pressures.Food.EffectiveValue / 9;
        baseline -= polity.Governance.PeripheralStrain / 9;
        baseline -= context.ExternalPressure.RaidPressure / 10;
        baseline -= context.ScaleState.DistanceStrain / 10;
        baseline -= context.ScaleState.CompositeComplexity / 12;
        baseline += context.SocialIdentity.Communalism / 14;

        baseline += law.DefinitionId switch
        {
            GovernanceLawCatalog.LocalStoreAutonomyId => 8,
            GovernanceLawCatalog.OpenMovementId => 7,
            GovernanceLawCatalog.SharedGovernanceId => 8,
            GovernanceLawCatalog.FrontierIntegrationId => 6,
            GovernanceLawCatalog.CentralizeStoresId => -4,
            GovernanceLawCatalog.ExtractionObligationId => -8,
            GovernanceLawCatalog.CrisisMovementRestrictionId => -7,
            GovernanceLawCatalog.StrengthenCentralAuthorityId => -3,
            _ => 0
        };

        baseline += law.DefinitionId switch
        {
            GovernanceLawCatalog.CentralizeStoresId => -context.SocialIdentity.AutonomyOrientation / 12 + (HasTradition(context, SocialTraditionCatalog.StorageDisciplineId) ? 6 : 0),
            GovernanceLawCatalog.LocalStoreAutonomyId => context.SocialIdentity.AutonomyOrientation / 10 + context.SocialIdentity.FrontierDistinctiveness / 14,
            GovernanceLawCatalog.CrisisMovementRestrictionId => -context.SocialIdentity.Mobility / 10 - (HasTradition(context, SocialTraditionCatalog.SeasonalReturnId) ? 6 : 0),
            GovernanceLawCatalog.OpenMovementId => context.SocialIdentity.Mobility / 10,
            GovernanceLawCatalog.StrengthenCentralAuthorityId => context.SocialIdentity.OrderOrientation / 12 - context.SocialIdentity.AutonomyOrientation / 12,
            GovernanceLawCatalog.SharedGovernanceId => context.SocialIdentity.Communalism / 10 + context.SocialIdentity.AutonomyOrientation / 14,
            GovernanceLawCatalog.FrontierIntegrationId => context.SocialIdentity.FrontierDistinctiveness / 12,
            _ => 0
        };

        return Clamp(baseline);
    }

    private static double ResolveEffectScale(EnactedLaw law, Polity polity, PopulationGroup group)
    {
        var isCore = string.Equals(group.CurrentRegionId, polity.CoreRegionId, StringComparison.Ordinal) ||
                     string.Equals(group.CurrentRegionId, polity.HomeRegionId, StringComparison.Ordinal);
        var locality = isCore ? law.CoreEffectiveness : law.PeripheralEffectiveness;
        var scale = locality / 100.0;
        scale *= 0.65 + (law.ImpactScale * 0.12);
        return Math.Clamp(scale, 0.0, 1.25);
    }

    private static bool HasLaw(IEnumerable<EnactedLaw> laws, string definitionId)
    {
        return laws.Any(law => law.IsActive && string.Equals(law.DefinitionId, definitionId, StringComparison.Ordinal));
    }

    private static bool HasTradition(PolityContext context, string traditionId)
    {
        return context.SocialIdentity.TraditionIds.Contains(traditionId, StringComparer.Ordinal);
    }

    private static int Scale(int value, double scale)
    {
        return (int)Math.Round(value * scale, MidpointRounding.AwayFromZero);
    }

    private static int DriftToward(int current, int target)
    {
        return Clamp((current * 2 + target) / 3);
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
            Pressures = group.Pressures.Clone(),
            LastRegionId = group.LastRegionId,
            MonthsSinceLastMove = group.MonthsSinceLastMove,
            KnownRegionIds = new HashSet<string>(group.KnownRegionIds, StringComparer.Ordinal),
            KnownDiscoveryIds = new HashSet<string>(group.KnownDiscoveryIds, StringComparer.Ordinal),
            DiscoveryEvidence = group.DiscoveryEvidence.Clone(),
            LearnedAdvancementIds = new HashSet<string>(group.LearnedAdvancementIds, StringComparer.Ordinal),
            AdvancementEvidence = group.AdvancementEvidence.Clone()
        };
    }

    private static int Clamp(int value)
    {
        return Math.Clamp(value, 0, 100);
    }

}
