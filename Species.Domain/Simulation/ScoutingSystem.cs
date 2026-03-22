using Species.Domain.Catalogs;
using Species.Domain.Discovery;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class ScoutingSystem
{
    private const int ScoutFoodCost = 1;

    public ScoutingResult Run(
        World world,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var updatedGroups = world.PopulationGroups
            .Select(CloneGroup)
            .ToArray();
        var groupsById = updatedGroups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var updatedPolities = world.Polities
            .Select(polity => polity.Clone())
            .ToArray();
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var changes = new List<DiscoveryChange>();

        foreach (var polity in updatedPolities.OrderBy(polity => polity.Id, StringComparer.Ordinal))
        {
            var polityWorld = new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, updatedPolities, world.FocalPolityId);
            var context = PolityData.BuildContext(polityWorld, polity);
            if (context is null || context.LeadGroup is null)
            {
                continue;
            }

            var motives = ResolveMotives(context, polity);
            var canScout = CanScout(context, motives);
            if (!canScout)
            {
                continue;
            }

            var candidates = BuildCandidates(context, polity, regionsById, discoveryCatalog, floraCatalog, faunaCatalog);
            if (candidates.Count == 0)
            {
                continue;
            }

            var target = candidates
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.TargetRegion.Id, StringComparer.Ordinal)
                .First();

            SpendScoutCost(context, polity, groupsById);
            var outcome = ResolveOutcome(world, polity.Id, target.TargetRegion.Id, target.Score);
            var unlocked = ApplyOutcome(
                polity,
                context,
                target,
                outcome,
                groupsById,
                discoveryCatalog,
                floraCatalog,
                faunaCatalog);

            changes.Add(new DiscoveryChange
            {
                GroupId = polity.Id,
                GroupName = polity.Name,
                KnownDiscoveriesSummary = string.Join(", ", context.KnownDiscoveryIds.OrderBy(id => id, StringComparer.Ordinal).Take(6)),
                EvidenceSummary = $"Scouting target={target.TargetRegion.Id} outcome={outcome.Kind} motives=[{string.Join(", ", motives)}] cost=food:{ScoutFoodCost}",
                CheckSummary = $"Scouted {target.TargetRegion.Name} from {target.SourceRegion.Name} because {string.Join(", ", motives)}.",
                UnlockedDiscoveriesSummary = unlocked.Count == 0 ? "none" : string.Join(", ", unlocked.Select(item => item.Label)),
                DecisionEffectSummary = unlocked.Count == 0
                    ? "Scouting produced no durable finding this month."
                    : string.Join(" ", unlocked.Select(item => item.Effect)),
                UnlockedEntries = unlocked
                    .Select(item => new DiscoveryUnlock
                    {
                        Name = item.Label,
                        IsEncounter = item.IsEncounter,
                        IsScoutSourced = true,
                        RegionName = item.RegionName
                    })
                    .ToArray()
            });
        }

        return new ScoutingResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, updatedPolities, world.FocalPolityId),
            changes.Where(change => !string.Equals(change.UnlockedDiscoveriesSummary, "none", StringComparison.OrdinalIgnoreCase)).ToArray());
    }

    private static IReadOnlyList<string> ResolveMotives(PolityContext context, Polity polity)
    {
        var motives = new List<string>();
        if (context.Pressures.Food.EffectiveValue >= 40)
        {
            motives.Add("food pressure");
        }

        if (context.Pressures.Migration.EffectiveValue >= 40)
        {
            motives.Add("movement pressure");
        }

        if (context.MaterialShortageMonths > 0 || context.MaterialProduction.DeficitScore >= 45)
        {
            motives.Add("resource need");
        }

        if (context.ExternalPressure.Threat >= 30 ||
            polity.InterPolityRelations.Any(relation => relation.Hostility >= 35 || relation.RaidPressure >= 30))
        {
            motives.Add("neighbor pressure");
        }

        if (context.MaterialProduction.SurplusScore >= 25 || context.Pressures.Food.EffectiveValue <= 35)
        {
            motives.Add("opportunity pull");
        }

        if (context.Pressures.Curiosity.DisplayValue >= 10 &&
            context.AnchoringKind is PolityAnchoringKind.SemiRooted or PolityAnchoringKind.Anchored &&
            context.Pressures.Food.EffectiveValue < 75 &&
            context.Pressures.Migration.EffectiveValue < 75)
        {
            motives.Add("curiosity pressure");
        }

        return motives;
    }

    private static bool CanScout(PolityContext context, IReadOnlyList<string> motives)
    {
        if (motives.Count == 0)
        {
            return false;
        }

        if (context.TotalPopulation < 20)
        {
            return false;
        }

        if (context.Pressures.Food.EffectiveValue >= 85 || context.Pressures.Migration.EffectiveValue >= 85)
        {
            return false;
        }

        return context.TotalStoredFood > 0 ||
               context.FoodAccounting.EndingTotalStores > 0 ||
               context.MaterialProduction.SurplusScore >= 20 ||
               context.AnchoringKind is PolityAnchoringKind.SemiRooted or PolityAnchoringKind.Anchored;
    }

    private static IReadOnlyList<ScoutCandidate> BuildCandidates(
        PolityContext context,
        Polity polity,
        IReadOnlyDictionary<string, Region> regionsById,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var baseRegionIds = polity.RegionalPresences
            .Where(presence => presence.IsCurrent || presence.Kind is PolityPresenceKind.Seasonal or PolityPresenceKind.Habitation or PolityPresenceKind.Core)
            .Select(presence => presence.RegionId)
            .Concat(context.MemberGroups.Select(group => group.CurrentRegionId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var candidates = new List<ScoutCandidate>();

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

                var fullyKnown = IsFullyKnown(context, targetRegion, discoveryCatalog, floraCatalog, faunaCatalog);
                if (fullyKnown)
                {
                    continue;
                }

                var score = ScoreCandidate(context, targetRegion, discoveryCatalog);
                candidates.Add(new ScoutCandidate(sourceRegion, targetRegion, score));
            }
        }

        return candidates
            .GroupBy(candidate => candidate.TargetRegion.Id, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(item => item.Score).First())
            .ToArray();
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

    private static float ScoreCandidate(PolityContext context, Region targetRegion, DiscoveryCatalog discoveryCatalog)
    {
        var unknownBonus = context.KnownRegionIds.Contains(targetRegion.Id) ? 12.0f : 24.0f;
        var foodWeight = context.Pressures.Food.EffectiveValue / 5.0f;
        var migrationWeight = context.Pressures.Migration.EffectiveValue / 6.0f;
        var resourceWeight = Math.Max(context.MaterialProduction.DeficitScore, context.ExternalPressure.Threat) / 8.0f;
        var curiosityWeight = context.Pressures.Curiosity.DisplayValue / 3.0f;
        var opportunityWeight = (float)((targetRegion.Fertility * 14.0) + targetRegion.MaterialProfile.ToolPotential + targetRegion.MaterialProfile.StoragePotential) / 8.0f;
        var knownGap = 0.0f;
        if (!context.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalWaterSourcesDiscoveryId(targetRegion.Id)))
        {
            knownGap += 6.0f;
        }

        if (!discoveryCatalog.IsLocalRegionDiscoveryKnown(context.KnownDiscoveryIds, targetRegion.Id))
        {
            knownGap += 6.0f;
        }

        if (!context.KnownRegionIds.Contains(targetRegion.Id) && context.Pressures.Curiosity.DisplayValue >= 25)
        {
            knownGap += 8.0f;
        }

        return unknownBonus + foodWeight + migrationWeight + resourceWeight + curiosityWeight + opportunityWeight + knownGap;
    }

    private static void SpendScoutCost(PolityContext context, Polity polity, IDictionary<string, PopulationGroup> groupsById)
    {
        if (context.LeadGroup is not null &&
            groupsById.TryGetValue(context.LeadGroup.Id, out var leadGroup) &&
            leadGroup.StoredFood >= ScoutFoodCost)
        {
            leadGroup.StoredFood -= ScoutFoodCost;
            return;
        }

        var primarySettlement = polity.Settlements.FirstOrDefault(settlement => settlement.IsActive && settlement.IsPrimary);
        if (primarySettlement is not null && primarySettlement.StoredFood >= ScoutFoodCost)
        {
            primarySettlement.StoredFood -= ScoutFoodCost;
        }
    }

    private static ScoutOutcome ResolveOutcome(World world, string polityId, string targetRegionId, float score)
    {
        var roll = ResolveRoll(world.Seed, world.CurrentYear, world.CurrentMonth, polityId, targetRegionId);
        var strength = Math.Clamp((int)Math.Round(score, MidpointRounding.AwayFromZero), 0, 100);
        if (roll < Math.Max(10, 28 - (strength / 5)))
        {
            return new ScoutOutcome("failure", 0.0f);
        }

        if (roll < Math.Max(28, 50 - (strength / 8)))
        {
            return new ScoutOutcome("no findings", 0.25f);
        }

        if (roll < Math.Max(65, 78 - (strength / 10)))
        {
            return new ScoutOutcome("partial", 0.60f);
        }

        return new ScoutOutcome("success", 1.00f);
    }

    private static List<ScoutUnlock> ApplyOutcome(
        Polity polity,
        PolityContext context,
        ScoutCandidate target,
        ScoutOutcome outcome,
        IDictionary<string, PopulationGroup> groupsById,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var unlocked = new List<ScoutUnlock>();
        foreach (var group in context.MemberGroups)
        {
            if (groupsById.TryGetValue(group.Id, out var updatedGroup))
            {
                updatedGroup.KnownRegionIds.Add(target.TargetRegion.Id);
            }
        }

        if (outcome.Kind is "failure" or "no findings")
        {
            AccelerateSpeciesAwareness(polity, target.TargetRegion, floraCatalog, faunaCatalog, partialOnly: true);
            return unlocked;
        }

        AddDiscoveryToGroups(context.MemberGroups, groupsById, discoveryCatalog.GetRouteDiscoveryId(target.SourceRegion.Id, target.TargetRegion.Id));
        unlocked.Add(new ScoutUnlock(
            $"{target.SourceRegion.Name} to {target.TargetRegion.Name} Route",
            "Scouting adds a reachable route to polity decision-making.",
            IsEncounter: false,
            target.TargetRegion.Name));

        if (!context.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalWaterSourcesDiscoveryId(target.TargetRegion.Id)))
        {
            AddDiscoveryToGroups(context.MemberGroups, groupsById, discoveryCatalog.GetLocalWaterSourcesDiscoveryId(target.TargetRegion.Id));
            unlocked.Add(new ScoutUnlock(
                $"{target.TargetRegion.Name} water sources",
                "Scouting adds local water expectations to polity decision-making.",
                IsEncounter: false,
                target.TargetRegion.Name));
        }

        if (outcome.FindingStrength >= 0.60f &&
            !discoveryCatalog.IsLocalRegionDiscoveryKnown(context.KnownDiscoveryIds, target.TargetRegion.Id))
        {
            AddDiscoveryToGroups(context.MemberGroups, groupsById, discoveryCatalog.GetLocalRegionDiscoveryId(target.TargetRegion.Id));
            unlocked.Add(new ScoutUnlock(
                target.TargetRegion.Name,
                "Scouting adds the region itself to polity decision-making.",
                IsEncounter: false,
                target.TargetRegion.Name));
        }

        if (target.TargetRegion.MaterialProfile.Opportunities.Stone > 0 &&
            !context.KnownDiscoveryIds.Contains(DiscoveryCatalog.ToolStoneId))
        {
            AddDiscoveryToGroups(context.MemberGroups, groupsById, DiscoveryCatalog.ToolStoneId);
            unlocked.Add(new ScoutUnlock(
                "tool stone",
                "Scouting adds tool stone as a meaningful practical resource.",
                IsEncounter: false,
                target.TargetRegion.Name));
        }

        unlocked.AddRange(ApplyFloraFindings(polity, target.TargetRegion, floraCatalog, outcome.FindingStrength));
        unlocked.AddRange(ApplyFaunaFindings(polity, target.TargetRegion, faunaCatalog, outcome.FindingStrength));
        unlocked.AddRange(ApplySapientFindings(polity, target.TargetRegion, context.MemberGroups, groupsById));

        return unlocked;
    }

    private static IReadOnlyList<ScoutUnlock> ApplyFloraFindings(Polity polity, Region targetRegion, FloraSpeciesCatalog floraCatalog, float findingStrength)
    {
        var findings = new List<ScoutUnlock>();
        foreach (var floraEntry in targetRegion.Ecosystem.FloraPopulations
                     .Where(entry => entry.Value > 0)
                     .OrderByDescending(entry => ScoreSpeciesVisibility(entry.Value, floraCatalog.GetById(entry.Key)?.Conspicuousness ?? 0.0f))
                     .Take(findingStrength >= 1.0f ? 2 : 1))
        {
            var flora = floraCatalog.GetById(floraEntry.Key);
            if (flora is null || ScoreSpeciesVisibility(floraEntry.Value, flora.Conspicuousness) < 25.0f)
            {
                continue;
            }

            var state = GetOrCreateSpeciesAwareness(polity, SpeciesClass.Flora, flora.Id);
            if (state.CurrentStage == DiscoveryStage.Discovered)
            {
                continue;
            }

            state.EncounterProgress = 100.0f;
            state.DiscoveryProgress = Math.Min(100.0f, state.DiscoveryProgress + (findingStrength >= 1.0f ? 100.0f : 55.0f));
            if (state.CurrentStage == DiscoveryStage.Discovered)
            {
                findings.Add(new ScoutUnlock(
                    flora.Name,
                    "Scouting reveals meaningful local flora to the polity.",
                    IsEncounter: false,
                    targetRegion.Name));
            }
        }

        return findings;
    }

    private static IReadOnlyList<ScoutUnlock> ApplyFaunaFindings(Polity polity, Region targetRegion, FaunaSpeciesCatalog faunaCatalog, float findingStrength)
    {
        var findings = new List<ScoutUnlock>();
        foreach (var faunaEntry in targetRegion.Ecosystem.FaunaPopulations
                     .Where(entry => entry.Value > 0)
                     .OrderByDescending(entry => ScoreSpeciesVisibility(entry.Value, faunaCatalog.GetById(entry.Key)?.Conspicuousness ?? 0.0f))
                     .Take(findingStrength >= 1.0f ? 2 : 1))
        {
            var fauna = faunaCatalog.GetById(faunaEntry.Key);
            if (fauna is null || ScoreSpeciesVisibility(faunaEntry.Value, fauna.Conspicuousness) < 22.0f)
            {
                continue;
            }

            var state = GetOrCreateSpeciesAwareness(polity, SpeciesClass.Fauna, fauna.Id);
            if (state.CurrentStage == DiscoveryStage.Discovered)
            {
                continue;
            }

            state.EncounterProgress = 100.0f;
            state.DiscoveryProgress = Math.Min(100.0f, state.DiscoveryProgress + (findingStrength >= 1.0f ? 100.0f : 50.0f));
            if (state.CurrentStage == DiscoveryStage.Discovered)
            {
                findings.Add(new ScoutUnlock(
                    fauna.Name,
                    "Scouting reveals meaningful local fauna to the polity.",
                    IsEncounter: false,
                    targetRegion.Name));
            }
        }

        return findings;
    }

    private static IReadOnlyList<ScoutUnlock> ApplySapientFindings(
        Polity polity,
        Region targetRegion,
        IReadOnlyList<PopulationGroup> memberGroups,
        IDictionary<string, PopulationGroup> groupsById)
    {
        var findings = new List<ScoutUnlock>();
        var encounteredSpecies = memberGroups
            .Select(group => group.PolityId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var otherGroup in groupsById.Values
                     .Where(group => group.SpeciesClass == SpeciesClass.Sapient)
                     .Where(group => string.Equals(group.CurrentRegionId, targetRegion.Id, StringComparison.Ordinal))
                     .Where(group => !encounteredSpecies.Contains(group.PolityId))
                     .OrderByDescending(group => group.Population)
                     .Take(1))
        {
            var state = GetOrCreateSpeciesAwareness(polity, SpeciesClass.Sapient, otherGroup.SpeciesId);
            if (state.CurrentStage >= DiscoveryStage.Encountered)
            {
                continue;
            }

            state.EncounterProgress = 100.0f;
            findings.Add(new ScoutUnlock(
                BuildSpeciesName(otherGroup.SpeciesId),
                "Scouting extends sapient encounter range without granting deeper capability.",
                IsEncounter: true,
                targetRegion.Name));
        }

        return findings;
    }

    private static void AccelerateSpeciesAwareness(Polity polity, Region targetRegion, FloraSpeciesCatalog floraCatalog, FaunaSpeciesCatalog faunaCatalog, bool partialOnly)
    {
        foreach (var floraEntry in targetRegion.Ecosystem.FloraPopulations.Where(entry => entry.Value > 0).Take(1))
        {
            var flora = floraCatalog.GetById(floraEntry.Key);
            if (flora is null)
            {
                continue;
            }

            var state = GetOrCreateSpeciesAwareness(polity, SpeciesClass.Flora, flora.Id);
            state.EncounterProgress = Math.Min(100.0f, state.EncounterProgress + 20.0f);
            if (!partialOnly)
            {
                state.DiscoveryProgress = Math.Min(100.0f, state.DiscoveryProgress + 20.0f);
            }
        }

        foreach (var faunaEntry in targetRegion.Ecosystem.FaunaPopulations.Where(entry => entry.Value > 0).Take(1))
        {
            var fauna = faunaCatalog.GetById(faunaEntry.Key);
            if (fauna is null)
            {
                continue;
            }

            var state = GetOrCreateSpeciesAwareness(polity, SpeciesClass.Fauna, fauna.Id);
            state.EncounterProgress = Math.Min(100.0f, state.EncounterProgress + 20.0f);
            if (!partialOnly)
            {
                state.DiscoveryProgress = Math.Min(100.0f, state.DiscoveryProgress + 20.0f);
            }
        }
    }

    private static void AddDiscoveryToGroups(IEnumerable<PopulationGroup> memberGroups, IDictionary<string, PopulationGroup> groupsById, string discoveryId)
    {
        foreach (var group in memberGroups)
        {
            if (groupsById.TryGetValue(group.Id, out var updatedGroup))
            {
                updatedGroup.KnownDiscoveryIds.Add(discoveryId);
            }
        }
    }

    private static PolitySpeciesAwarenessState GetOrCreateSpeciesAwareness(Polity polity, SpeciesClass speciesClass, string speciesId)
    {
        var state = polity.SpeciesAwareness.FirstOrDefault(item =>
            item.SpeciesClass == speciesClass &&
            string.Equals(item.SpeciesId, speciesId, StringComparison.Ordinal));
        if (state is not null)
        {
            return state;
        }

        state = new PolitySpeciesAwarenessState
        {
            SpeciesClass = speciesClass,
            SpeciesId = speciesId
        };
        polity.SpeciesAwareness.Add(state);
        return state;
    }

    private static int ResolveRoll(int seed, int year, int month, string polityId, string targetRegionId)
    {
        unchecked
        {
            var hash = seed;
            foreach (var ch in $"{year}|{month}|{polityId}|{targetRegionId}|scouting")
            {
                hash = (hash * 31) + ch;
            }

            return Math.Abs(hash % 100);
        }
    }

    private static float ScoreSpeciesVisibility(int population, float conspicuousness)
    {
        return (float)Math.Clamp((Math.Log10(population + 1.0) * 22.0) + (conspicuousness * 40.0), 0.0, 100.0);
    }

    private static string BuildSpeciesName(string speciesId)
    {
        return speciesId switch
        {
            "species-human" => "Human",
            _ => string.Join(' ', speciesId
                .Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries)
                .Where(part => !string.Equals(part, "species", StringComparison.OrdinalIgnoreCase))
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]))
        };
    }

    private static PopulationGroup CloneGroup(PopulationGroup group)
    {
        return new PopulationGroup
        {
            Id = group.Id,
            Name = group.Name,
            SpeciesId = group.SpeciesId,
            SpeciesClass = group.SpeciesClass,
            PolityId = group.PolityId,
            CurrentRegionId = group.CurrentRegionId,
            OriginRegionId = group.OriginRegionId,
            Population = group.Population,
            StoredFood = group.StoredFood,
            FoodAccounting = group.FoodAccounting.Clone(),
            HungerPressure = group.HungerPressure,
            ShortageMonths = group.ShortageMonths,
            FoodStressState = group.FoodStressState,
            SubsistencePreference = group.SubsistencePreference,
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

    private sealed record ScoutCandidate(Region SourceRegion, Region TargetRegion, float Score);

    private sealed record ScoutOutcome(string Kind, float FindingStrength);

    private sealed record ScoutUnlock(string Label, string Effect, bool IsEncounter, string RegionName);
}
