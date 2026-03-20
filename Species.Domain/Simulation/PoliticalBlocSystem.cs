using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

// Political blocs stay polity-level and lightweight in this phase. They refresh
// monthly from current laws, conditions, and government form, then feed proposal pressure.
public sealed class PoliticalBlocSystem
{
    public World Run(World world)
    {
        if (world.PopulationGroups.Count == 0)
        {
            return world;
        }

        var updatedGroups = new List<PopulationGroup>(world.PopulationGroups.Count);
        foreach (var group in world.PopulationGroups)
        {
            var updatedGroup = CloneGroup(group);
            EnsureBlocs(updatedGroup);
            UpdateBlocs(updatedGroup);
            updatedGroups.Add(updatedGroup);
        }

        return new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle);
    }

    public static void EnsureBlocs(PopulationGroup group)
    {
        if (group.PoliticalBlocs.Count == Enum.GetValues<ProposalBackingSource>().Length)
        {
            return;
        }

        var existing = group.PoliticalBlocs.ToDictionary(bloc => bloc.Source);
        var fallbackBlocs = PoliticalBlocCatalog.CreateInitialBlocs(group.GovernmentForm)
            .ToDictionary(bloc => bloc.Source);
        group.PoliticalBlocs.Clear();

        foreach (var source in Enum.GetValues<ProposalBackingSource>())
        {
            if (existing.TryGetValue(source, out var bloc))
            {
                group.PoliticalBlocs.Add(bloc);
                continue;
            }

            group.PoliticalBlocs.Add(fallbackBlocs[source]);
        }
    }

    private static void UpdateBlocs(PopulationGroup group)
    {
        foreach (var bloc in group.PoliticalBlocs)
        {
            var targetInfluence = ResolveTargetInfluence(group, bloc.Source);
            var targetSatisfaction = ResolveTargetSatisfaction(group, bloc.Source);
            bloc.Influence = DriftToward(bloc.Influence, targetInfluence);
            bloc.Satisfaction = DriftToward(bloc.Satisfaction, targetSatisfaction);
        }
    }

    private static int ResolveTargetInfluence(PopulationGroup group, ProposalBackingSource source)
    {
        var behavior = GovernmentFormLawBehaviorCatalog.Get(group.GovernmentForm);
        var activeLaws = group.EnactedLaws.Where(law => law.IsActive).ToArray();
        var baseline = 16 + (behavior.GetBackingSourceWeight(source) * 6);

        baseline += source switch
        {
            ProposalBackingSource.Priests => CountAlignment(activeLaws, source) * 3 + (group.GovernmentForm == GovernmentForm.Theocracy ? 8 : 0) + group.Pressures.MigrationPressure / 12,
            ProposalBackingSource.Warriors => CountAlignment(activeLaws, source) * 3 + group.Pressures.ThreatPressure / 8,
            ProposalBackingSource.Merchants => CountAlignment(activeLaws, source) * 3 + Math.Max(0, 55 - group.Pressures.FoodPressure) / 10 + (group.StoredFood > group.Population ? 6 : 0),
            ProposalBackingSource.Elders => CountAlignment(activeLaws, source) * 3 + Math.Max(0, 55 - group.Pressures.MigrationPressure) / 10,
            ProposalBackingSource.CommonFolk => CountAlignment(activeLaws, source) * 2 + group.Pressures.FoodPressure / 10 + group.Pressures.OvercrowdingPressure / 14,
            ProposalBackingSource.FrontierSettlers => CountAlignment(activeLaws, source) * 3 + group.Pressures.ThreatPressure / 12 + group.Pressures.MigrationPressure / 9,
            _ => 0
        };

        return Math.Clamp(baseline, 10, 95);
    }

    private static int ResolveTargetSatisfaction(PopulationGroup group, ProposalBackingSource source)
    {
        var activeLaws = group.EnactedLaws.Where(law => law.IsActive).ToArray();
        var baseline = 50 + (CountAlignment(activeLaws, source) * 2);

        baseline += source switch
        {
            ProposalBackingSource.Priests => ResolvePriestSatisfaction(group, activeLaws),
            ProposalBackingSource.Warriors => ResolveWarriorSatisfaction(group, activeLaws),
            ProposalBackingSource.Merchants => ResolveMerchantSatisfaction(group, activeLaws),
            ProposalBackingSource.Elders => ResolveElderSatisfaction(group, activeLaws),
            ProposalBackingSource.CommonFolk => ResolveCommonSatisfaction(group, activeLaws),
            ProposalBackingSource.FrontierSettlers => ResolveFrontierSatisfaction(group, activeLaws),
            _ => 0
        };

        return Math.Clamp(baseline, 5, 95);
    }

    private static int ResolvePriestSatisfaction(PopulationGroup group, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = group.GovernmentForm == GovernmentForm.Theocracy ? 8 : 0;
        score += CountByCategory(activeLaws, LawProposalCategory.Faith) * 6;
        score += CountByCategory(activeLaws, LawProposalCategory.Symbolic) * 4;
        score -= activeLaws.Count(law => law.DefinitionId == "permit-foreign-worship") * 10;
        score += group.Pressures.ThreatPressure / 20;
        return score;
    }

    private static int ResolveWarriorSatisfaction(PopulationGroup group, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = group.Pressures.ThreatPressure / 8;
        score += CountByCategory(activeLaws, LawProposalCategory.Military) * 6;
        score += CountByCategory(activeLaws, LawProposalCategory.Order) * 3;
        score += CountByCategory(activeLaws, LawProposalCategory.Punishment) * 2;
        score -= activeLaws.Count(law => law.DefinitionId is "restrict-private-retainers" or "limit-emergency-decrees") * 8;
        return score;
    }

    private static int ResolveMerchantSatisfaction(PopulationGroup group, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = CountByCategory(activeLaws, LawProposalCategory.Trade) * 6;
        score += CountByCategory(activeLaws, LawProposalCategory.Movement) * 4;
        score += Math.Max(0, 50 - group.Pressures.ThreatPressure) / 10;
        score -= activeLaws.Count(law => law.DefinitionId is "close-city-gates" or "initiate-curfew") * 10;
        score -= group.Pressures.MigrationPressure / 16;
        return score;
    }

    private static int ResolveElderSatisfaction(PopulationGroup group, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = CountByCategory(activeLaws, LawProposalCategory.Custom) * 6;
        score += CountByCategory(activeLaws, LawProposalCategory.Order) * 3;
        score -= CountDisruptiveReforms(activeLaws) * 6;
        score -= group.Pressures.MigrationPressure / 14;
        return score;
    }

    private static int ResolveCommonSatisfaction(PopulationGroup group, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = CountByCategory(activeLaws, LawProposalCategory.Food) * 5;
        score += activeLaws.Count(law => law.DefinitionId is "open-grain-stores" or "grant-market-rights" or "expand-civic-assembly") * 8;
        score -= activeLaws.Count(law => law.DefinitionId is "raise-war-levy" or "establish-public-executions" or "authorize-secret-arrests" or "impose-emergency-rule") * 8;
        score -= group.Pressures.FoodPressure / 6;
        score -= group.Pressures.OvercrowdingPressure / 12;
        return score;
    }

    private static int ResolveFrontierSatisfaction(PopulationGroup group, IReadOnlyList<EnactedLaw> activeLaws)
    {
        var score = CountByCategory(activeLaws, LawProposalCategory.Movement) * 5;
        score += CountByCategory(activeLaws, LawProposalCategory.Military) * 4;
        score += group.Pressures.ThreatPressure / 12;
        score += group.Pressures.MigrationPressure / 12;
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

    private static PopulationGroup CloneGroup(PopulationGroup group)
    {
        return new PopulationGroup
        {
            Id = group.Id,
            Name = group.Name,
            SpeciesId = group.SpeciesId,
            CurrentRegionId = group.CurrentRegionId,
            OriginRegionId = group.OriginRegionId,
            Population = group.Population,
            StoredFood = group.StoredFood,
            SubsistenceMode = group.SubsistenceMode,
            GovernmentForm = group.GovernmentForm,
            Pressures = new PressureState
            {
                FoodPressure = group.Pressures.FoodPressure,
                WaterPressure = group.Pressures.WaterPressure,
                ThreatPressure = group.Pressures.ThreatPressure,
                OvercrowdingPressure = group.Pressures.OvercrowdingPressure,
                MigrationPressure = group.Pressures.MigrationPressure
            },
            LastRegionId = group.LastRegionId,
            MonthsSinceLastMove = group.MonthsSinceLastMove,
            KnownRegionIds = new HashSet<string>(group.KnownRegionIds, StringComparer.Ordinal),
            KnownDiscoveryIds = new HashSet<string>(group.KnownDiscoveryIds, StringComparer.Ordinal),
            DiscoveryEvidence = group.DiscoveryEvidence.Clone(),
            LearnedAdvancementIds = new HashSet<string>(group.LearnedAdvancementIds, StringComparer.Ordinal),
            AdvancementEvidence = group.AdvancementEvidence.Clone(),
            ActiveLawProposal = group.ActiveLawProposal?.Clone(),
            LawProposalHistory = group.LawProposalHistory.Select(proposal => proposal.Clone()).ToList(),
            EnactedLaws = group.EnactedLaws.Select(law => law.Clone()).ToList(),
            PoliticalBlocs = group.PoliticalBlocs.Select(bloc => bloc.Clone()).ToList()
        };
    }
}
