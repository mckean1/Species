using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

// Enacted laws stay deliberately simple in this phase: each month they refresh
// enforcement/compliance from current conditions, then apply scaled pressure effects.
public sealed class EnactedLawSystem
{
    private readonly IReadOnlyDictionary<string, EnactedLawEffect> effectsByDefinitionId =
        new Dictionary<string, EnactedLawEffect>(StringComparer.Ordinal)
        {
            ["ban-hunting"] = new(12, 0, -3, 0, 4),
            ["require-warrior-oaths"] = new(0, 0, -8, 0, 2),
            ["forbid-blood-feuds"] = new(0, 0, -10, -4, 0),
            ["reserve-sacred-grounds"] = new(0, 0, -2, 5, 0),
            ["grant-market-rights"] = new(-6, 0, 0, 0, -4),
            ["open-grain-stores"] = new(-12, 0, 0, 3, 0),
            ["restrict-private-retainers"] = new(0, 0, -8, 0, 2),
            ["expand-council-seats"] = new(0, 0, 0, -3, -5),
            ["initiate-curfew"] = new(0, 0, -9, 0, 5),
            ["raise-war-levy"] = new(10, 0, -10, 0, 4),
            ["establish-public-executions"] = new(0, 0, -7, 0, 8),
            ["close-city-gates"] = new(4, 0, -6, 0, 10),
            ["forbid-foreign-worship"] = new(0, 0, -5, 0, 8),
            ["mandate-holy-rites"] = new(2, 0, -4, 0, 4),
            ["burn-heretical-texts"] = new(0, 0, -3, 0, 8),
            ["ban-funeral-excess"] = new(-5, 0, 0, 0, 1),
            ["end-curfew"] = new(0, 0, 7, 0, -5),
            ["reopen-city-gates"] = new(-3, 0, 5, 0, -8),
            ["permit-foreign-worship"] = new(0, 0, 4, 0, -7),
            ["end-public-executions"] = new(0, 0, 5, 0, -6)
        };

    public World Run(World world)
    {
        if (world.PopulationGroups.Count == 0)
        {
            return world;
        }

        var updatedGroups = new List<PopulationGroup>(world.PopulationGroups.Count);
        foreach (var group in world.PopulationGroups)
        {
            if (group.EnactedLaws.Count == 0)
            {
                updatedGroups.Add(CloneGroup(group));
                continue;
            }

            var updatedGroup = CloneGroup(group);
            UpdateLawEffectiveness(updatedGroup);
            var combinedEffect = CombineEffects(updatedGroup);
            updatedGroup.Pressures = new PressureState
            {
                FoodPressure = Clamp(updatedGroup.Pressures.FoodPressure + combinedEffect.FoodPressureModifier),
                WaterPressure = Clamp(updatedGroup.Pressures.WaterPressure + combinedEffect.WaterPressureModifier),
                ThreatPressure = Clamp(updatedGroup.Pressures.ThreatPressure + combinedEffect.ThreatPressureModifier),
                OvercrowdingPressure = Clamp(updatedGroup.Pressures.OvercrowdingPressure + combinedEffect.OvercrowdingPressureModifier),
                MigrationPressure = Clamp(updatedGroup.Pressures.MigrationPressure + combinedEffect.MigrationPressureModifier)
            };

            updatedGroups.Add(updatedGroup);
        }

        return new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle);
    }

    private void UpdateLawEffectiveness(PopulationGroup group)
    {
        foreach (var enactedLaw in group.EnactedLaws.Where(law => law.IsActive))
        {
            var targetEnforcement = ResolveTargetEnforcement(group, enactedLaw);
            var targetCompliance = ResolveTargetCompliance(group, enactedLaw, targetEnforcement);
            enactedLaw.EnforcementStrength = DriftToward(enactedLaw.EnforcementStrength, targetEnforcement);
            enactedLaw.ComplianceLevel = DriftToward(enactedLaw.ComplianceLevel, targetCompliance);
        }
    }

    private EnactedLawEffect CombineEffects(PopulationGroup group)
    {
        var total = new EnactedLawEffect(0, 0, 0, 0, 0);
        foreach (var enactedLaw in group.EnactedLaws.Where(law => law.IsActive))
        {
            if (!effectsByDefinitionId.TryGetValue(enactedLaw.DefinitionId, out var effect))
            {
                continue;
            }

            var scale = ResolveEffectScale(enactedLaw);

            total = new EnactedLawEffect(
                total.FoodPressureModifier + Scale(effect.FoodPressureModifier, scale),
                total.WaterPressureModifier + Scale(effect.WaterPressureModifier, scale),
                total.ThreatPressureModifier + Scale(effect.ThreatPressureModifier, scale),
                total.OvercrowdingPressureModifier + Scale(effect.OvercrowdingPressureModifier, scale),
                total.MigrationPressureModifier + Scale(effect.MigrationPressureModifier, scale));
        }

        return total;
    }

    private static int ResolveTargetEnforcement(PopulationGroup group, EnactedLaw law)
    {
        var behavior = GovernmentFormLawBehaviorCatalog.Get(group.GovernmentForm);
        var baseline = behavior.EnforcementTendency;

        baseline += law.Category switch
        {
            LawProposalCategory.Order => 8,
            LawProposalCategory.Punishment => 10,
            LawProposalCategory.Military => 8,
            LawProposalCategory.Faith => group.GovernmentForm == GovernmentForm.Theocracy ? 10 : 2,
            LawProposalCategory.Custom => group.GovernmentForm is GovernmentForm.TribalClanRule or GovernmentForm.Confederation ? 6 : -2,
            LawProposalCategory.Trade => group.GovernmentForm == GovernmentForm.MerchantRule ? 7 : -4,
            LawProposalCategory.Movement => group.GovernmentForm == GovernmentForm.MerchantRule ? 5 : -3,
            LawProposalCategory.Food => group.Pressures.FoodPressure >= 60 ? -6 : 2,
            _ => 0
        };

        baseline += group.Pressures.ThreatPressure / 10;
        baseline += group.Pressures.MigrationPressure / 16;
        baseline -= group.Pressures.OvercrowdingPressure / 14;
        baseline += behavior.ExtremityAllowance;

        if (group.GovernmentForm == GovernmentForm.ImperialBureaucracy &&
            law.Category is LawProposalCategory.Order or LawProposalCategory.Trade)
        {
            baseline += 8;
        }

        if (group.GovernmentForm == GovernmentForm.FeudalRule &&
            law.Category is LawProposalCategory.Military or LawProposalCategory.Custom)
        {
            baseline += 6;
        }

        if (law.Category == LawProposalCategory.Faith)
        {
            baseline += group.Pressures.MigrationPressure / 10;
        }

        if (law.Category is LawProposalCategory.Trade or LawProposalCategory.Movement)
        {
            baseline -= group.Pressures.MigrationPressure / 8;
        }

        return Clamp(baseline);
    }

    private static int ResolveTargetCompliance(PopulationGroup group, EnactedLaw law, int enforcementStrength)
    {
        var behavior = GovernmentFormLawBehaviorCatalog.Get(group.GovernmentForm);
        var baseline = behavior.ComplianceTendency;

        baseline += law.Category switch
        {
            LawProposalCategory.Custom => group.GovernmentForm is GovernmentForm.TribalClanRule or GovernmentForm.Confederation ? 14 : 6,
            LawProposalCategory.Faith => group.GovernmentForm == GovernmentForm.Theocracy ? 12 : 2,
            LawProposalCategory.Food => group.Pressures.FoodPressure >= 55 ? -12 : 3,
            LawProposalCategory.Trade => group.GovernmentForm == GovernmentForm.MerchantRule ? 8 : group.Pressures.MigrationPressure >= 50 ? -6 : 2,
            LawProposalCategory.Movement => group.Pressures.MigrationPressure >= 50 ? -8 : 0,
            LawProposalCategory.Punishment => enforcementStrength >= 60 ? 4 : -4,
            LawProposalCategory.Order => enforcementStrength >= 55 ? 4 : 0,
            _ => 0
        };

        baseline += enforcementStrength / 8;
        baseline -= group.Pressures.FoodPressure / 10;
        baseline -= group.Pressures.OvercrowdingPressure / 12;

        if (law.Category == LawProposalCategory.Faith)
        {
            baseline += group.Pressures.ThreatPressure / 16;
            baseline += group.Pressures.MigrationPressure / 12;
        }

        if (group.GovernmentForm == GovernmentForm.Republic &&
            law.Category is LawProposalCategory.Order or LawProposalCategory.Custom)
        {
            baseline += 6;
        }

        if (group.GovernmentForm == GovernmentForm.DespoticRule &&
            law.Category is LawProposalCategory.Order or LawProposalCategory.Punishment)
        {
            baseline -= 8;
        }

        if (group.GovernmentForm == GovernmentForm.Confederation &&
            law.Category == LawProposalCategory.Custom)
        {
            baseline += 8;
        }

        return Clamp(baseline);
    }

    private static double ResolveEffectScale(EnactedLaw law)
    {
        var enforcement = law.EnforcementStrength / 100.0;
        var compliance = law.ComplianceLevel / 100.0;
        return Math.Clamp((enforcement * 0.55) + (compliance * 0.45), 0.0, 1.0);
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

    private static int Clamp(int value)
    {
        return Math.Clamp(value, 0, 100);
    }
}
