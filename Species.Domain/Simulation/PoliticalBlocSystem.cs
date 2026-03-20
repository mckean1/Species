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
            ProposalBackingSource.Priests => CountAlignment(activeLaws, source) * 3 + (polity.GovernmentForm == GovernmentForm.Theocracy ? 8 : 0) + context.Pressures.MigrationPressure / 12,
            ProposalBackingSource.Warriors => CountAlignment(activeLaws, source) * 3 + context.Pressures.ThreatPressure / 8,
            ProposalBackingSource.Merchants => CountAlignment(activeLaws, source) * 3 + Math.Max(0, 55 - context.Pressures.FoodPressure) / 10 + (context.TotalStoredFood > context.TotalPopulation ? 6 : 0),
            ProposalBackingSource.Elders => CountAlignment(activeLaws, source) * 3 + Math.Max(0, 55 - context.Pressures.MigrationPressure) / 10,
            ProposalBackingSource.CommonFolk => CountAlignment(activeLaws, source) * 2 + context.Pressures.FoodPressure / 10 + context.Pressures.OvercrowdingPressure / 14,
            ProposalBackingSource.FrontierSettlers => CountAlignment(activeLaws, source) * 3 + context.Pressures.ThreatPressure / 12 + context.Pressures.MigrationPressure / 9,
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
        score += CountByCategory(activeLaws, LawProposalCategory.Faith) * 6;
        score += CountByCategory(activeLaws, LawProposalCategory.Symbolic) * 4;
        score -= activeLaws.Count(law => law.DefinitionId == "permit-foreign-worship") * 10;
        score += context.Pressures.ThreatPressure / 20;
        return score;
    }

    private static int ResolveWarriorSatisfaction(PolityContext context, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = context.Pressures.ThreatPressure / 8;
        score += CountByCategory(activeLaws, LawProposalCategory.Military) * 6;
        score += CountByCategory(activeLaws, LawProposalCategory.Order) * 3;
        score += CountByCategory(activeLaws, LawProposalCategory.Punishment) * 2;
        score -= activeLaws.Count(law => law.DefinitionId is "restrict-private-retainers" or "limit-emergency-decrees") * 8;
        return score;
    }

    private static int ResolveMerchantSatisfaction(PolityContext context, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = CountByCategory(activeLaws, LawProposalCategory.Trade) * 6;
        score += CountByCategory(activeLaws, LawProposalCategory.Movement) * 4;
        score += Math.Max(0, 50 - context.Pressures.ThreatPressure) / 10;
        score -= activeLaws.Count(law => law.DefinitionId is "close-city-gates" or "initiate-curfew") * 10;
        score -= context.Pressures.MigrationPressure / 16;
        return score;
    }

    private static int ResolveElderSatisfaction(PolityContext context, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = CountByCategory(activeLaws, LawProposalCategory.Custom) * 6;
        score += CountByCategory(activeLaws, LawProposalCategory.Order) * 3;
        score -= CountDisruptiveReforms(activeLaws) * 6;
        score -= context.Pressures.MigrationPressure / 14;
        return score;
    }

    private static int ResolveCommonSatisfaction(PolityContext context, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = CountByCategory(activeLaws, LawProposalCategory.Food) * 5;
        score += activeLaws.Count(law => law.DefinitionId is "open-grain-stores" or "grant-market-rights" or "expand-civic-assembly") * 8;
        score -= activeLaws.Count(law => law.DefinitionId is "raise-war-levy" or "establish-public-executions" or "authorize-secret-arrests" or "impose-emergency-rule") * 8;
        score -= context.Pressures.FoodPressure / 6;
        score -= context.Pressures.OvercrowdingPressure / 12;
        return score;
    }

    private static int ResolveFrontierSatisfaction(PolityContext context, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = CountByCategory(activeLaws, LawProposalCategory.Movement) * 5;
        score += CountByCategory(activeLaws, LawProposalCategory.Military) * 4;
        score += context.Pressures.ThreatPressure / 12;
        score += context.Pressures.MigrationPressure / 12;
        score -= activeLaws.Count(law => law.DefinitionId is "close-city-gates" or "ban-hunting") * 7;
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

    private static int CountDisruptiveReforms(IEnumerable<EnactedLaw> activeLaws)
    {
        return activeLaws.Count(law => law.DefinitionId is "permit-foreign-worship" or "expand-civic-assembly" or "expand-council-seats");
    }

    private static int DriftToward(int current, int target)
    {
        return Math.Clamp((current * 2 + target) / 3, 0, 100);
    }
}
