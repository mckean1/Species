using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

// Enacted laws are polity-owned, but their monthly effects still route through
// member population groups because groups remain the pressure-bearing units.
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
        if (world.PopulationGroups.Count == 0 || world.Polities.Count == 0)
        {
            return world;
        }

        var updatedPolities = new List<Polity>(world.Polities.Count);
        var effectsByPolityId = new Dictionary<string, EnactedLawEffect>(StringComparer.Ordinal);

        foreach (var polity in world.Polities)
        {
            var updatedPolity = polity.Clone();
            var context = PolityData.BuildContext(world, updatedPolity);
            if (context is null || updatedPolity.EnactedLaws.Count == 0)
            {
                updatedPolities.Add(updatedPolity);
                effectsByPolityId[updatedPolity.Id] = new EnactedLawEffect(0, 0, 0, 0, 0);
                continue;
            }

            UpdateLawEffectiveness(updatedPolity, context);
            effectsByPolityId[updatedPolity.Id] = CombineEffects(updatedPolity);
            updatedPolities.Add(updatedPolity);
        }

        var updatedGroups = world.PopulationGroups
            .Select(group =>
            {
                var updatedGroup = CloneGroup(group);
                var combinedEffect = effectsByPolityId.GetValueOrDefault(group.PolityId, new EnactedLawEffect(0, 0, 0, 0, 0));
                updatedGroup.Pressures = new PressureState
                {
                    FoodPressure = Clamp(updatedGroup.Pressures.FoodPressure + combinedEffect.FoodPressureModifier),
                    WaterPressure = Clamp(updatedGroup.Pressures.WaterPressure + combinedEffect.WaterPressureModifier),
                    ThreatPressure = Clamp(updatedGroup.Pressures.ThreatPressure + combinedEffect.ThreatPressureModifier),
                    OvercrowdingPressure = Clamp(updatedGroup.Pressures.OvercrowdingPressure + combinedEffect.OvercrowdingPressureModifier),
                    MigrationPressure = Clamp(updatedGroup.Pressures.MigrationPressure + combinedEffect.MigrationPressureModifier)
                };

                return updatedGroup;
            })
            .ToArray();

        return new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, updatedPolities, world.FocalPolityId);
    }

    private void UpdateLawEffectiveness(Polity polity, PolityContext context)
    {
        foreach (var enactedLaw in polity.EnactedLaws.Where(law => law.IsActive))
        {
            var targetEnforcement = ResolveTargetEnforcement(polity, context.Pressures, enactedLaw);
            var targetCompliance = ResolveTargetCompliance(polity, context.Pressures, enactedLaw, targetEnforcement);
            enactedLaw.EnforcementStrength = DriftToward(enactedLaw.EnforcementStrength, targetEnforcement);
            enactedLaw.ComplianceLevel = DriftToward(enactedLaw.ComplianceLevel, targetCompliance);
        }
    }

    private EnactedLawEffect CombineEffects(Polity polity)
    {
        var total = new EnactedLawEffect(0, 0, 0, 0, 0);
        foreach (var enactedLaw in polity.EnactedLaws.Where(law => law.IsActive))
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

    private static int ResolveTargetEnforcement(Polity polity, PressureState pressures, EnactedLaw law)
    {
        var behavior = GovernmentFormLawBehaviorCatalog.Get(polity.GovernmentForm);
        var baseline = behavior.EnforcementTendency;

        baseline += law.Category switch
        {
            LawProposalCategory.Order => 8,
            LawProposalCategory.Punishment => 10,
            LawProposalCategory.Military => 8,
            LawProposalCategory.Faith => polity.GovernmentForm == GovernmentForm.Theocracy ? 10 : 2,
            LawProposalCategory.Custom => polity.GovernmentForm is GovernmentForm.TribalClanRule or GovernmentForm.Confederation ? 6 : -2,
            LawProposalCategory.Trade => polity.GovernmentForm == GovernmentForm.MerchantRule ? 7 : -4,
            LawProposalCategory.Movement => polity.GovernmentForm == GovernmentForm.MerchantRule ? 5 : -3,
            LawProposalCategory.Food => pressures.FoodPressure >= 60 ? -6 : 2,
            _ => 0
        };

        baseline += pressures.ThreatPressure / 10;
        baseline += pressures.MigrationPressure / 16;
        baseline -= pressures.OvercrowdingPressure / 14;
        baseline += behavior.ExtremityAllowance;

        if (polity.GovernmentForm == GovernmentForm.ImperialBureaucracy &&
            law.Category is LawProposalCategory.Order or LawProposalCategory.Trade)
        {
            baseline += 8;
        }

        if (polity.GovernmentForm == GovernmentForm.FeudalRule &&
            law.Category is LawProposalCategory.Military or LawProposalCategory.Custom)
        {
            baseline += 6;
        }

        if (law.Category == LawProposalCategory.Faith)
        {
            baseline += pressures.MigrationPressure / 10;
        }

        if (law.Category is LawProposalCategory.Trade or LawProposalCategory.Movement)
        {
            baseline -= pressures.MigrationPressure / 8;
        }

        return Clamp(baseline);
    }

    private static int ResolveTargetCompliance(Polity polity, PressureState pressures, EnactedLaw law, int enforcementStrength)
    {
        var behavior = GovernmentFormLawBehaviorCatalog.Get(polity.GovernmentForm);
        var baseline = behavior.ComplianceTendency;

        baseline += law.Category switch
        {
            LawProposalCategory.Custom => polity.GovernmentForm is GovernmentForm.TribalClanRule or GovernmentForm.Confederation ? 14 : 6,
            LawProposalCategory.Faith => polity.GovernmentForm == GovernmentForm.Theocracy ? 12 : 2,
            LawProposalCategory.Food => pressures.FoodPressure >= 55 ? -12 : 3,
            LawProposalCategory.Trade => polity.GovernmentForm == GovernmentForm.MerchantRule ? 8 : pressures.MigrationPressure >= 50 ? -6 : 2,
            LawProposalCategory.Movement => pressures.MigrationPressure >= 50 ? -8 : 0,
            LawProposalCategory.Punishment => enforcementStrength >= 60 ? 4 : -4,
            LawProposalCategory.Order => enforcementStrength >= 55 ? 4 : 0,
            _ => 0
        };

        baseline += enforcementStrength / 8;
        baseline -= pressures.FoodPressure / 10;
        baseline -= pressures.OvercrowdingPressure / 12;

        if (law.Category == LawProposalCategory.Faith)
        {
            baseline += pressures.ThreatPressure / 16;
            baseline += pressures.MigrationPressure / 12;
        }

        if (polity.GovernmentForm == GovernmentForm.Republic &&
            law.Category is LawProposalCategory.Order or LawProposalCategory.Custom)
        {
            baseline += 6;
        }

        if (polity.GovernmentForm == GovernmentForm.DespoticRule &&
            law.Category is LawProposalCategory.Order or LawProposalCategory.Punishment)
        {
            baseline -= 8;
        }

        if (polity.GovernmentForm == GovernmentForm.Confederation &&
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
            PolityId = group.PolityId,
            CurrentRegionId = group.CurrentRegionId,
            OriginRegionId = group.OriginRegionId,
            Population = group.Population,
            StoredFood = group.StoredFood,
            SubsistenceMode = group.SubsistenceMode,
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
            AdvancementEvidence = group.AdvancementEvidence.Clone()
        };
    }

    private static int Clamp(int value)
    {
        return Math.Clamp(value, 0, 100);
    }
}
