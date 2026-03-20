using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

// Political blocs remain lightweight and polity-owned. Monthly updates react to
// polity law state plus the aggregate condition of member population groups.
public sealed class PoliticalBlocSystem
{
    public World Run(World world)
    {
        if (world.Polities.Count == 0)
        {
            return world;
        }

        var updatedPolities = world.Polities.Select(polity =>
        {
            var updatedPolity = polity.Clone();
            EnsureBlocs(updatedPolity);
            var context = PolityData.BuildContext(world, updatedPolity);
            if (context is not null)
            {
                UpdateBlocs(updatedPolity, context);
            }

            return updatedPolity;
        }).ToArray();

        return new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, world.PopulationGroups, world.Chronicle, updatedPolities, world.FocalPolityId);
    }

    public static void EnsureBlocs(Polity polity)
    {
        if (polity.PoliticalBlocs.Count == Enum.GetValues<ProposalBackingSource>().Length)
        {
            return;
        }

        var existing = polity.PoliticalBlocs.ToDictionary(bloc => bloc.Source);
        var fallbackBlocs = PoliticalBlocCatalog.CreateInitialBlocs(polity.GovernmentForm)
            .ToDictionary(bloc => bloc.Source);
        polity.PoliticalBlocs.Clear();

        foreach (var source in Enum.GetValues<ProposalBackingSource>())
        {
            if (existing.TryGetValue(source, out var bloc))
            {
                polity.PoliticalBlocs.Add(bloc);
                continue;
            }

            polity.PoliticalBlocs.Add(fallbackBlocs[source]);
        }
    }

    private static void UpdateBlocs(Polity polity, PolityContext context)
    {
        foreach (var bloc in polity.PoliticalBlocs)
        {
            var targetInfluence = ResolveTargetInfluence(polity, context, bloc.Source);
            var targetSatisfaction = ResolveTargetSatisfaction(polity, context, bloc.Source);
            bloc.Influence = DriftToward(bloc.Influence, targetInfluence);
            bloc.Satisfaction = DriftToward(bloc.Satisfaction, targetSatisfaction);
        }
    }

    private static int ResolveTargetInfluence(Polity polity, PolityContext context, ProposalBackingSource source)
    {
        var behavior = GovernmentFormLawBehaviorCatalog.Get(polity.GovernmentForm);
        var activeLaws = polity.EnactedLaws.Where(law => law.IsActive).ToArray();
        var baseline = 16 + (behavior.GetBackingSourceWeight(source) * 6);

        baseline += source switch
        {
            ProposalBackingSource.Priests => CountAlignment(activeLaws, source) * 3 + (polity.GovernmentForm == GovernmentForm.Theocracy ? 8 : 0) + context.Governance.Authority / 14,
            ProposalBackingSource.Warriors => CountAlignment(activeLaws, source) * 3 + context.Pressures.Threat.EffectiveValue / 8 + context.ExternalPressure.Threat / 6 + context.Governance.Authority / 12 + context.SocialIdentity.OrderOrientation / 14,
            ProposalBackingSource.Merchants => CountAlignment(activeLaws, source) * 3 + Math.Max(0, 55 - context.Pressures.Food.EffectiveValue) / 10 + (context.TotalStoredFood > context.TotalPopulation ? 6 : 0) + context.MaterialProduction.StorageSupport / 18 + context.ExternalPressure.Cooperation / 8 + context.ScaleState.CompositeComplexity / 10 + context.SocialIdentity.Mobility / 16,
            ProposalBackingSource.Elders => CountAlignment(activeLaws, source) * 3 + Math.Max(0, 55 - context.Pressures.Migration.EffectiveValue) / 10 + context.Governance.Legitimacy / 16 + context.SocialIdentity.Rootedness / 14,
            ProposalBackingSource.CommonFolk => CountAlignment(activeLaws, source) * 2 + context.Pressures.Food.EffectiveValue / 10 + context.Pressures.Overcrowding.EffectiveValue / 14 + context.MaterialProduction.DeficitScore / 18 + Math.Max(0, 55 - context.Governance.Legitimacy) / 10 + context.SocialIdentity.Communalism / 16,
            ProposalBackingSource.FrontierSettlers => CountAlignment(activeLaws, source) * 3 + context.Pressures.Threat.EffectiveValue / 12 + context.ExternalPressure.FrontierFriction / 6 + context.ExternalPressure.RaidPressure / 7 + context.Pressures.Migration.EffectiveValue / 9 + context.MaterialProduction.DeficitScore / 16 + context.Governance.PeripheralStrain / 8 + context.SocialIdentity.FrontierDistinctiveness / 10,
            _ => 0
        };

        return Math.Clamp(baseline, 10, 95);
    }

    private static int ResolveTargetSatisfaction(Polity polity, PolityContext context, ProposalBackingSource source)
    {
        var activeLaws = polity.EnactedLaws.Where(law => law.IsActive).ToArray();
        var baseline = 50 + (CountAlignment(activeLaws, source) * 2);

        baseline += source switch
        {
            ProposalBackingSource.Priests => ResolvePriestSatisfaction(polity, context, activeLaws),
            ProposalBackingSource.Warriors => ResolveWarriorSatisfaction(context, activeLaws),
            ProposalBackingSource.Merchants => ResolveMerchantSatisfaction(context, activeLaws),
            ProposalBackingSource.Elders => ResolveElderSatisfaction(context, activeLaws),
            ProposalBackingSource.CommonFolk => ResolveCommonSatisfaction(context, activeLaws),
            ProposalBackingSource.FrontierSettlers => ResolveFrontierSatisfaction(context, activeLaws),
            _ => 0
        };

        return Math.Clamp(baseline, 5, 95);
    }

    private static int ResolvePriestSatisfaction(Polity polity, PolityContext context, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = polity.GovernmentForm == GovernmentForm.Theocracy ? 8 : 0;
        score += CountByCategory(activeLaws, LawProposalCategory.Order) * 2;
        score += context.Governance.Authority / 12;
        score += context.SocialIdentity.OrderOrientation / 18;
        score += context.Pressures.Threat.EffectiveValue / 20;
        return score;
    }

    private static int ResolveWarriorSatisfaction(PolityContext context, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = context.Pressures.Threat.EffectiveValue / 8;
        score += context.ExternalPressure.Threat / 7;
        score += context.ExternalPressure.RaidPressure / 8;
        score += CountByCategory(activeLaws, LawProposalCategory.Order) * 4;
        score += CountByCategory(activeLaws, LawProposalCategory.Order) * 3;
        score += activeLaws.Count(law => law.DefinitionId is GovernanceLawCatalog.CrisisMovementRestrictionId or GovernanceLawCatalog.StrengthenCentralAuthorityId) * 7;
        score -= activeLaws.Count(law => law.DefinitionId == GovernanceLawCatalog.SharedGovernanceId) * 6;
        score -= context.ScaleState.FragmentationRisk / 12;
        score += context.SocialIdentity.OrderOrientation / 10;
        return score;
    }

    private static int ResolveMerchantSatisfaction(PolityContext context, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = CountByCategory(activeLaws, LawProposalCategory.Movement) * 4;
        score += CountByCategory(activeLaws, LawProposalCategory.Movement) * 4;
        score += Math.Max(0, 50 - context.Pressures.Threat.EffectiveValue) / 10;
        score += context.ExternalPressure.Cooperation / 8;
        score += context.MaterialProduction.StorageSupport / 10;
        score += context.ScaleState.CompositeComplexity / 12;
        score += activeLaws.Count(law => law.DefinitionId is GovernanceLawCatalog.OpenMovementId or GovernanceLawCatalog.LocalStoreAutonomyId) * 7;
        score -= activeLaws.Count(law => law.DefinitionId is GovernanceLawCatalog.CrisisMovementRestrictionId or GovernanceLawCatalog.CentralizeStoresId) * 8;
        score -= context.Pressures.Migration.EffectiveValue / 16;
        score += context.SocialIdentity.Mobility / 12;
        return score;
    }

    private static int ResolveElderSatisfaction(PolityContext context, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = CountByCategory(activeLaws, LawProposalCategory.Custom) * 6;
        score += CountByCategory(activeLaws, LawProposalCategory.Order) * 3;
        score += activeLaws.Count(law => law.DefinitionId is GovernanceLawCatalog.SharedGovernanceId or GovernanceLawCatalog.LocalStoreAutonomyId) * 5;
        score -= activeLaws.Count(law => law.DefinitionId == GovernanceLawCatalog.StrengthenCentralAuthorityId) * 7;
        score -= context.Pressures.Migration.EffectiveValue / 14;
        score -= context.ExternalPressure.RaidPressure / 14;
        score -= context.ScaleState.Centralization / 16;
        score += context.SocialIdentity.Rootedness / 12;
        return score;
    }

    private static int ResolveCommonSatisfaction(PolityContext context, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = CountByCategory(activeLaws, LawProposalCategory.Food) * 5;
        score += activeLaws.Count(law => law.DefinitionId is GovernanceLawCatalog.CentralizeStoresId or GovernanceLawCatalog.SharedGovernanceId) * 5;
        score -= activeLaws.Count(law => law.DefinitionId is GovernanceLawCatalog.ExtractionObligationId or GovernanceLawCatalog.CrisisMovementRestrictionId) * 7;
        score -= context.Pressures.Food.EffectiveValue / 6;
        score -= context.ExternalPressure.RaidPressure / 8;
        score -= context.ScaleState.OverextensionPressure / 10;
        score -= context.Pressures.Overcrowding.EffectiveValue / 12;
        score -= context.MaterialProduction.DeficitScore / 8;
        score += context.Governance.Legitimacy / 12;
        score += context.SocialIdentity.Communalism / 12;
        return score;
    }

    private static int ResolveFrontierSatisfaction(PolityContext context, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = CountByCategory(activeLaws, LawProposalCategory.Movement) * 5;
        score += CountByCategory(activeLaws, LawProposalCategory.Order) * 3;
        score += context.Pressures.Threat.EffectiveValue / 12;
        score += context.ExternalPressure.FrontierFriction / 7;
        score += context.ExternalPressure.RaidPressure / 8;
        score += context.ScaleState.AutonomyTolerance / 12;
        score += context.Pressures.Migration.EffectiveValue / 12;
        score += context.MaterialProduction.ToolSupport / 12;
        score += activeLaws.Count(law => law.DefinitionId is GovernanceLawCatalog.OpenMovementId or GovernanceLawCatalog.FrontierIntegrationId) * 7;
        score -= activeLaws.Count(law => law.DefinitionId is GovernanceLawCatalog.CentralizeStoresId or GovernanceLawCatalog.ExtractionObligationId or GovernanceLawCatalog.CrisisMovementRestrictionId) * 7;
        score -= context.Governance.PeripheralStrain / 10;
        score += context.SocialIdentity.FrontierDistinctiveness / 10;
        score += context.SocialIdentity.AutonomyOrientation / 12;
        return score;
    }

    private static int CountAlignment(IEnumerable<EnactedLaw> activeLaws, ProposalBackingSource source)
    {
        return activeLaws.Count(law => PoliticalBlocCatalog.GetCategoryWeight(source, law.Category) >= 7);
    }

    private static int CountByCategory(IEnumerable<EnactedLaw> activeLaws, LawProposalCategory category)
    {
        return activeLaws.Count(law => law.Category == category);
    }
    private static int DriftToward(int current, int target)
    {
        return Math.Clamp((current * 2 + target) / 3, 0, 100);
    }
}
