using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Knowledge;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class FaunaSimulationSystem
{
    public FaunaSimulationResult Run(
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var stateByRegionId = world.Regions.ToDictionary(
            region => region.Id,
            region => new MutableRegionState(region),
            StringComparer.Ordinal);
        var changes = new List<FaunaPopulationChange>();

        foreach (var region in world.Regions)
        {
            var state = stateByRegionId[region.Id];

            foreach (var faunaEntry in OrderFaunaEntries(region.Ecosystem.FaunaPopulations, faunaCatalog))
            {
                var species = faunaCatalog.GetById(faunaEntry.Key);
                if (species is null || species.IsExtinct)
                {
                    continue;
                }

                var currentPopulation = state.FaunaPopulations.GetValueOrDefault(species.Id);
                if (currentPopulation <= 0)
                {
                    continue;
                }

                var profile = ResolveFaunaProfile(state, species);
                var traits = profile.Traits;
                var startingPopulation = faunaEntry.Value;

                // Causal order:
                // 1. resolve actual diet access from current regional ecology
                // 2. convert feeding success into hunger / food-stress carryover
                // 3. derive births and deaths from fedness vs shortage
                // 4. let persistent shortage spill into migration pressure and movement
                var habitatSupport = ResolveHabitatSupport(region, species, traits);
                var foodNeeded = ResolveRequiredIntake(species, traits) * currentPopulation;
                var feedingEfficiency = ResolveFeedingEfficiency(species, traits);
                var consumption = ConsumeDiet(foodNeeded, feedingEfficiency, species, currentPopulation, traits, state, floraCatalog, faunaCatalog);
                var fulfillmentRatio = foodNeeded <= 0.0f
                    ? 1.0f
                    : Math.Clamp(consumption.FoodConsumed / foodNeeded, 0.0f, 1.0f);
                var foodShortfall = Math.Max(0.0f, foodNeeded - consumption.FoodConsumed);
                var feedingMomentum = ResolveFeedingMomentum(profile.FeedingMomentum, fulfillmentRatio);
                var hungerPressure = FoodStressModel.ResolveHungerPressure(profile.HungerPressure, fulfillmentRatio, FaunaSimulationConstants.HungerRiseRate, FaunaSimulationConstants.HungerDecayRate);
                var shortageMonths = FoodStressModel.ResolveShortageMonths(profile.ShortageMonths, fulfillmentRatio);
                var foodStressState = FoodStressModel.ResolveState(fulfillmentRatio, hungerPressure, shortageMonths);
                var births = ResolveBirths(currentPopulation, species, habitatSupport, feedingMomentum, foodStressState);
                var deaths = ResolveDeaths(currentPopulation, species, hungerPressure, shortageMonths, habitatSupport, traits, foodStressState, fulfillmentRatio);
                var postMortalityPopulation = Math.Max(0, currentPopulation + births - deaths);
                var migrationPressure = ResolveMigrationPressure(species, hungerPressure, shortageMonths, foodStressState);
                var migrationPlan = ResolveMigration(region, stateByRegionId, species, postMortalityPopulation, migrationPressure, shortageMonths, floraCatalog, faunaCatalog);
                var finalPopulation = Math.Max(0, postMortalityPopulation - migrationPlan.MigratedPopulation);

                if (finalPopulation > 0)
                {
                    state.FaunaPopulations[species.Id] = finalPopulation;
                }
                else
                {
                    state.FaunaPopulations.Remove(species.Id);
                }

                profile.FeedingMomentum = feedingMomentum;
                profile.HungerPressure = hungerPressure;
                profile.ShortageMonths = shortageMonths;
                profile.FoodStressState = foodStressState;
                profile.LastPopulation = finalPopulation;
                profile.IsExtinct = finalPopulation <= 0;
                profile.ViabilityScore = Math.Clamp((int)Math.Round((feedingMomentum * 55.0f) + (habitatSupport * 25.0f) + ((1.0f - hungerPressure) * 20.0f), MidpointRounding.AwayFromZero), 0, 100);

                if (migrationPlan.MigratedPopulation > 0)
                {
                    ApplyMigration(stateByRegionId, species, profile, migrationPlan);
                }

                changes.Add(new FaunaPopulationChange
                {
                    RegionId = region.Id,
                    RegionName = region.Name,
                    FaunaSpeciesId = species.Id,
                    FaunaSpeciesName = species.Name,
                    PreviousPopulation = startingPopulation,
                    NewPopulation = finalPopulation,
                    Births = births,
                    Deaths = deaths,
                    FoodNeeded = MathF.Round(foodNeeded, 2),
                    FoodConsumed = MathF.Round(consumption.FoodConsumed, 2),
                    FoodShortfall = MathF.Round(foodShortfall, 2),
                    FulfillmentRatio = MathF.Round(fulfillmentRatio, 2),
                    HungerPressure = MathF.Round(hungerPressure, 2),
                    ShortageMonths = shortageMonths,
                    FoodStressState = foodStressState.ToString(),
                    HabitatSupport = MathF.Round(habitatSupport, 2),
                    BiologicalFit = MathF.Round(consumption.FoodSourceFit, 2),
                    MigrationPressure = MathF.Round(migrationPressure, 2),
                    MigratedOut = migrationPlan.MigratedPopulation,
                    Outcome = ResolveOutcome(startingPopulation, finalPopulation, migrationPlan.MigratedPopulation),
                    ConsumedFloraSummary = BuildConsumptionSummary(consumption.ConsumedFloraPopulations),
                    ConsumedFaunaSummary = BuildConsumptionSummary(consumption.ConsumedFaunaPopulations),
                    PrimaryCause = ResolvePrimaryCause(fulfillmentRatio, hungerPressure, habitatSupport, migrationPlan.MigratedPopulation, finalPopulation)
                });
            }
        }

        var updatedRegions = world.Regions
            .Select(region => stateByRegionId[region.Id].ToRegion())
            .ToArray();

        return new FaunaSimulationResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, updatedRegions, world.PopulationGroups, world.Chronicle, world.Polities, world.FocalPolityId),
            changes);
    }

    private static IEnumerable<KeyValuePair<string, int>> OrderFaunaEntries(
        IReadOnlyDictionary<string, int> faunaPopulations,
        FaunaSpeciesCatalog faunaCatalog)
    {
        return faunaPopulations
            .OrderBy(entry => ResolveDietOrder(faunaCatalog.GetById(entry.Key)?.DietCategory ?? DietCategory.Omnivore))
            .ThenBy(entry => entry.Key, StringComparer.Ordinal);
    }

    private static int ResolveDietOrder(DietCategory category)
    {
        return category switch
        {
            DietCategory.Herbivore => 0,
            DietCategory.Omnivore => 1,
            DietCategory.Carnivore => 2,
            _ => 1
        };
    }

    private static RegionalBiologicalProfile ResolveFaunaProfile(MutableRegionState state, FaunaSpeciesDefinition species)
    {
        if (state.FaunaProfiles.TryGetValue(species.Id, out var existing))
        {
            return existing;
        }

        var created = new RegionalBiologicalProfile
        {
            SpeciesId = species.Id,
            RegionId = state.Region.Id,
            Traits = species.BaselineTraits.Clone(),
            ViabilityScore = 50,
            LastPopulation = state.FaunaPopulations.GetValueOrDefault(species.Id)
        };
        state.FaunaProfiles[species.Id] = created;
        return created;
    }

    private static float ResolveHabitatSupport(Region region, FaunaSpeciesDefinition species, BiologicalTraitProfile traits)
    {
        if (!species.SupportedWaterAvailabilities.Contains(region.WaterAvailability))
        {
            return FaunaSimulationConstants.UnsupportedWaterHabitatSupport;
        }

        var fertilityFit = GetFertilityFit((float)region.Fertility, species.HabitatFertilityMin, species.HabitatFertilityMax);
        var biomeFit = species.CoreBiomes.Contains(region.Biome)
            ? FaunaSimulationConstants.CoreBiomeHabitatSupport
            : FaunaSimulationConstants.NonCoreBiomeHabitatSupport;
        var climateFit = ResolveClimateFit(region, traits);
        var mobilityFit = 0.82f + ((traits.Mobility / 100.0f) * 0.18f);

        return Math.Clamp((fertilityFit * 0.26f) + (biomeFit * 0.24f) + (climateFit * 0.28f) + (mobilityFit * 0.10f) + (species.RegionalAbundance * 0.12f), 0.12f, 1.20f);
    }

    private static float ResolveRequiredIntake(FaunaSpeciesDefinition species, BiologicalTraitProfile traits)
    {
        var intake = species.RequiredIntake;
        intake *= 0.86f + ((traits.BodySize / 100.0f) * 0.42f);
        intake *= 0.94f + ((1.0f - species.FeedingEfficiency) * 0.12f);
        return Math.Max(0.05f, intake);
    }

    private static float ResolveFeedingEfficiency(FaunaSpeciesDefinition species, BiologicalTraitProfile traits)
    {
        return Math.Clamp(
            FaunaSimulationConstants.MinimumFeedingEfficiency +
            (species.FeedingEfficiency * 0.30f) +
            ((traits.Flexibility / 100.0f) * 0.12f) +
            ((traits.Mobility / 100.0f) * 0.08f),
            0.45f,
            1.20f);
    }

    private static ConsumptionResult ConsumeDiet(
        float foodNeeded,
        float feedingEfficiency,
        FaunaSpeciesDefinition species,
        int currentPopulation,
        BiologicalTraitProfile predatorTraits,
        MutableRegionState state,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var consumedFlora = new Dictionary<string, int>(StringComparer.Ordinal);
        var consumedFauna = new Dictionary<string, int>(StringComparer.Ordinal);
        var preferredLinks = species.DietLinks.Where(link => !link.IsFallback).ToArray();
        var fallbackLinks = species.DietLinks.Where(link => link.IsFallback).ToArray();

        var preferred = ConsumeDietLinks(foodNeeded, feedingEfficiency, species, currentPopulation, predatorTraits, preferredLinks, state, floraCatalog, faunaCatalog, includeFallbackPenalty: false);
        MergeInto(consumedFlora, preferred.ConsumedFlora);
        MergeInto(consumedFauna, preferred.ConsumedFauna);

        var remainingNeed = Math.Max(0.0f, foodNeeded - preferred.FoodConsumed);
        var fallback = remainingNeed <= 0.01f
            ? SourceConsumption.Empty()
            : ConsumeDietLinks(remainingNeed, feedingEfficiency, species, currentPopulation, predatorTraits, fallbackLinks, state, floraCatalog, faunaCatalog, includeFallbackPenalty: true);
        MergeInto(consumedFlora, fallback.ConsumedFlora);
        MergeInto(consumedFauna, fallback.ConsumedFauna);

        var totalFoodConsumed = preferred.FoodConsumed + fallback.FoodConsumed;
        var weightedFit = (preferred.FoodConsumed * preferred.SourceFit) + (fallback.FoodConsumed * fallback.SourceFit);
        var fit = totalFoodConsumed <= 0.0f ? 0.0f : weightedFit / totalFoodConsumed;

        return new ConsumptionResult(totalFoodConsumed, fit, consumedFlora, consumedFauna);
    }

    private static SourceConsumption ConsumeDietLinks(
        float requestedFood,
        float feedingEfficiency,
        FaunaSpeciesDefinition species,
        int currentPopulation,
        BiologicalTraitProfile predatorTraits,
        IReadOnlyList<FaunaDietLink> dietLinks,
        MutableRegionState state,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        bool includeFallbackPenalty)
    {
        if (requestedFood <= 0.0f || dietLinks.Count == 0)
        {
            return SourceConsumption.Empty();
        }

        var consumedFlora = new Dictionary<string, int>(StringComparer.Ordinal);
        var consumedFauna = new Dictionary<string, int>(StringComparer.Ordinal);
        var foodConsumed = 0.0f;
        var fitWeightedFood = 0.0f;
        var remainingWeight = dietLinks.Sum(link => link.Weight);
        var remainingFood = requestedFood;

        foreach (var link in dietLinks.OrderByDescending(link => link.Weight).ThenBy(link => link.TargetSpeciesId, StringComparer.Ordinal))
        {
            if (remainingFood <= 0.01f || remainingWeight <= 0.0f)
            {
                break;
            }

            var requestedFromLink = remainingFood * (link.Weight / remainingWeight);
            var linkConsumption = ConsumeDietLink(requestedFromLink, feedingEfficiency, species, currentPopulation, predatorTraits, link, state, floraCatalog, faunaCatalog, includeFallbackPenalty);
            foodConsumed += linkConsumption.FoodConsumed;
            fitWeightedFood += linkConsumption.FoodConsumed * linkConsumption.SourceFit;
            remainingFood = Math.Max(0.0f, remainingFood - linkConsumption.FoodConsumed);
            remainingWeight -= link.Weight;

            MergeInto(consumedFlora, linkConsumption.ConsumedFlora);
            MergeInto(consumedFauna, linkConsumption.ConsumedFauna);
        }

        var sourceFit = foodConsumed <= 0.0f ? 0.0f : fitWeightedFood / foodConsumed;
        return new SourceConsumption(foodConsumed, sourceFit, consumedFlora, consumedFauna);
    }

    private static SourceConsumption ConsumeDietLink(
        float requestedFood,
        float feedingEfficiency,
        FaunaSpeciesDefinition predator,
        int predatorPopulation,
        BiologicalTraitProfile predatorTraits,
        FaunaDietLink link,
        MutableRegionState state,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        bool includeFallbackPenalty)
    {
        return link.TargetKind switch
        {
            FaunaDietTargetKind.FloraSpecies => ConsumeFloraTarget(requestedFood, feedingEfficiency, link, state.Region, state.FloraPopulations, floraCatalog, includeFallbackPenalty),
            FaunaDietTargetKind.FaunaSpecies => ConsumePreyTarget(requestedFood, feedingEfficiency, predator, predatorPopulation, predatorTraits, link, state, faunaCatalog, includeFallbackPenalty),
            FaunaDietTargetKind.ScavengePool => ConsumeScavengePool(requestedFood, feedingEfficiency, predator, predatorPopulation, predatorTraits, state, faunaCatalog, includeFallbackPenalty),
            _ => SourceConsumption.Empty()
        };
    }

    private static SourceConsumption ConsumeFloraTarget(
        float requestedFood,
        float feedingEfficiency,
        FaunaDietLink link,
        Region region,
        IDictionary<string, int> floraPopulations,
        FloraSpeciesCatalog floraCatalog,
        bool includeFallbackPenalty)
    {
        if (requestedFood <= 0.0f || !floraPopulations.TryGetValue(link.TargetSpeciesId, out var availablePopulation) || availablePopulation <= 0)
        {
            return SourceConsumption.Empty();
        }

        var flora = floraCatalog.GetById(link.TargetSpeciesId);
        if (flora is null)
        {
            return SourceConsumption.Empty();
        }

        var foodPerPopulation = SubsistenceSupportModel.ResolveFloraFoodPerPopulation(region, flora) *
                                feedingEfficiency *
                                FaunaSimulationConstants.FloraConversionEfficiency;
        if (includeFallbackPenalty)
        {
            foodPerPopulation *= 1.0f - FaunaSimulationConstants.FallbackDietPenalty;
        }

        var units = ResolveUnitsToConsume(requestedFood, availablePopulation, foodPerPopulation);
        if (units <= 0)
        {
            return SourceConsumption.Empty();
        }

        floraPopulations[link.TargetSpeciesId] -= units;
        if (floraPopulations[link.TargetSpeciesId] <= 0)
        {
            floraPopulations.Remove(link.TargetSpeciesId);
        }

        var consumedFlora = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [link.TargetSpeciesId] = units
        };
        var fit = includeFallbackPenalty ? 0.72f : 1.00f;
        return new SourceConsumption(units * foodPerPopulation, fit, consumedFlora, new Dictionary<string, int>(StringComparer.Ordinal));
    }

    private static SourceConsumption ConsumePreyTarget(
        float requestedFood,
        float feedingEfficiency,
        FaunaSpeciesDefinition predator,
        int predatorPopulation,
        BiologicalTraitProfile predatorTraits,
        FaunaDietLink link,
        MutableRegionState state,
        FaunaSpeciesCatalog faunaCatalog,
        bool includeFallbackPenalty)
    {
        if (requestedFood <= 0.0f ||
            string.Equals(link.TargetSpeciesId, predator.Id, StringComparison.Ordinal) ||
            !state.FaunaPopulations.TryGetValue(link.TargetSpeciesId, out var preyPopulation) ||
            preyPopulation <= 0)
        {
            return SourceConsumption.Empty();
        }

        var preySpecies = faunaCatalog.GetById(link.TargetSpeciesId);
        if (preySpecies is null)
        {
            return SourceConsumption.Empty();
        }

        var preyTraits = state.FaunaProfiles.GetValueOrDefault(preySpecies.Id)?.Traits ?? preySpecies.BaselineTraits;
        var accessibleShare = ResolveAccessiblePreyShare(predator, predatorTraits, preySpecies, preyTraits, preyPopulation, predatorPopulation);
        var accessiblePopulation = Math.Max(0, (int)MathF.Floor(preyPopulation * accessibleShare));
        if (accessiblePopulation <= 0)
        {
            return SourceConsumption.Empty();
        }

        var foodPerPopulation = preySpecies.FoodYield *
                                feedingEfficiency *
                                FaunaSimulationConstants.PreyConversionEfficiency *
                                Math.Clamp(preySpecies.PredatorVulnerability + (accessibleShare * 0.25f), 0.12f, 1.10f);
        if (includeFallbackPenalty)
        {
            foodPerPopulation *= 1.0f - FaunaSimulationConstants.FallbackDietPenalty;
        }

        var units = ResolveUnitsToConsume(requestedFood, accessiblePopulation, foodPerPopulation);
        if (units <= 0)
        {
            return SourceConsumption.Empty();
        }

        state.FaunaPopulations[preySpecies.Id] -= units;
        if (state.FaunaPopulations[preySpecies.Id] <= 0)
        {
            state.FaunaPopulations.Remove(preySpecies.Id);
        }

        var consumedFauna = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [preySpecies.Id] = units
        };
        var fit = includeFallbackPenalty ? 0.58f + (accessibleShare * 0.18f) : 0.74f + (accessibleShare * 0.20f);
        return new SourceConsumption(units * foodPerPopulation, Math.Clamp(fit, 0.0f, 1.0f), new Dictionary<string, int>(StringComparer.Ordinal), consumedFauna);
    }

    private static SourceConsumption ConsumeScavengePool(
        float requestedFood,
        float feedingEfficiency,
        FaunaSpeciesDefinition predator,
        int predatorPopulation,
        BiologicalTraitProfile predatorTraits,
        MutableRegionState state,
        FaunaSpeciesCatalog faunaCatalog,
        bool includeFallbackPenalty)
    {
        if (requestedFood <= 0.0f)
        {
            return SourceConsumption.Empty();
        }

        var candidates = state.FaunaPopulations
            .Where(entry => !string.Equals(entry.Key, predator.Id, StringComparison.Ordinal) && entry.Value > 0)
            .Select(entry =>
            {
                var preySpecies = faunaCatalog.GetById(entry.Key);
                if (preySpecies is null)
                {
                    return null;
                }

                var preyTraits = state.FaunaProfiles.GetValueOrDefault(preySpecies.Id)?.Traits ?? preySpecies.BaselineTraits;
                var accessibleShare = ResolveAccessiblePreyShare(predator, predatorTraits, preySpecies, preyTraits, entry.Value, predatorPopulation) *
                                      FaunaSimulationConstants.ScavengeAccessibleShareMultiplier;
                return new ScavengeCandidate(preySpecies, entry.Value, accessibleShare);
            })
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderByDescending(candidate => candidate.AccessibleShare)
            .ToArray();

        if (candidates.Length == 0)
        {
            return SourceConsumption.Empty();
        }

        var consumedFauna = new Dictionary<string, int>(StringComparer.Ordinal);
        var foodConsumed = 0.0f;
        foreach (var candidate in candidates)
        {
            if (foodConsumed >= requestedFood - 0.01f)
            {
                break;
            }

            var accessiblePopulation = Math.Max(0, (int)MathF.Floor(candidate.AvailablePopulation * candidate.AccessibleShare));
            if (accessiblePopulation <= 0)
            {
                continue;
            }

            var foodPerPopulation = candidate.Species.FoodYield *
                                    feedingEfficiency *
                                    FaunaSimulationConstants.ScavengeConversionEfficiency *
                                    Math.Clamp(candidate.Species.PredatorVulnerability + (candidate.AccessibleShare * 0.15f), 0.08f, 0.85f);
            if (includeFallbackPenalty)
            {
                foodPerPopulation *= 1.0f - FaunaSimulationConstants.FallbackDietPenalty;
            }

            var units = ResolveUnitsToConsume(requestedFood - foodConsumed, accessiblePopulation, foodPerPopulation);
            if (units <= 0)
            {
                continue;
            }

            state.FaunaPopulations[candidate.Species.Id] -= units;
            if (state.FaunaPopulations[candidate.Species.Id] <= 0)
            {
                state.FaunaPopulations.Remove(candidate.Species.Id);
            }

            consumedFauna[candidate.Species.Id] = consumedFauna.TryGetValue(candidate.Species.Id, out var existing)
                ? existing + units
                : units;
            foodConsumed += units * foodPerPopulation;
        }

        return foodConsumed <= 0.0f
            ? SourceConsumption.Empty()
            : new SourceConsumption(foodConsumed, includeFallbackPenalty ? 0.42f : 0.50f, new Dictionary<string, int>(StringComparer.Ordinal), consumedFauna);
    }

    private static int ResolveUnitsToConsume(float requestedFood, int availablePopulation, float foodPerPopulation)
    {
        if (requestedFood <= 0.0f || availablePopulation <= 0 || foodPerPopulation <= 0.0f)
        {
            return 0;
        }

        return Math.Min(availablePopulation, Math.Max(0, (int)MathF.Ceiling(requestedFood / foodPerPopulation)));
    }

    private static float ResolveAccessiblePreyShare(
        FaunaSpeciesDefinition predator,
        BiologicalTraitProfile predatorTraits,
        FaunaSpeciesDefinition prey,
        BiologicalTraitProfile preyTraits,
        int preyPopulation,
        int predatorPopulation)
    {
        var abundanceFactor = Math.Min(0.18f, preyPopulation / 600.0f);
        var lowPreyRefuge = Math.Max(0.0f, (120.0f - Math.Min(120.0f, preyPopulation)) / 260.0f);
        var predatorPressureRefuge = Math.Max(0.0f, (predatorPopulation - preyPopulation) / 500.0f);
        var encounter = FaunaSimulationConstants.EncounterFrictionBase +
                        (predator.FeedingEfficiency * 0.16f) +
                        ((predatorTraits.Mobility / 100.0f) * 0.18f) +
                        ((predatorTraits.Flexibility / 100.0f) * 0.10f) +
                        abundanceFactor;
        var refuge = FaunaSimulationConstants.PreyRefugeBase +
                     ((preyTraits.Defense / 100.0f) * 0.16f) +
                     ((preyTraits.Mobility / 100.0f) * 0.10f) +
                     predatorPressureRefuge +
                     lowPreyRefuge;
        var accessibleShare = encounter - refuge + (prey.PredatorVulnerability * 0.20f);
        return Math.Clamp(accessibleShare, FaunaSimulationConstants.PreyAccessiblePopulationFloor, 0.68f);
    }

    private static float ResolveFeedingMomentum(float currentMomentum, float fulfillmentRatio)
    {
        var rate = fulfillmentRatio >= currentMomentum
            ? FaunaSimulationConstants.FeedingMomentumRiseRate
            : FaunaSimulationConstants.FeedingMomentumDecayRate;
        return ClampNormalized(currentMomentum + ((fulfillmentRatio - currentMomentum) * rate));
    }

    private static int ResolveBirths(int currentPopulation, FaunaSpeciesDefinition species, float habitatSupport, float feedingMomentum, FoodStressState foodStressState)
    {
        if (feedingMomentum < FaunaSimulationConstants.ReproductionFedThreshold || foodStressState != FoodStressState.FedStable)
        {
            return 0;
        }

        var momentumExcess = (feedingMomentum - FaunaSimulationConstants.ReproductionFedThreshold) /
                             Math.Max(0.01f, 1.0f - FaunaSimulationConstants.ReproductionFedThreshold);
        var reproductionFactor = species.ReproductionRate *
                                 Math.Clamp(momentumExcess, 0.0f, 1.0f) *
                                 habitatSupport *
                                 FaunaSimulationConstants.ReproductionWeight;
        return Math.Max(0, (int)MathF.Round(currentPopulation * reproductionFactor, MidpointRounding.AwayFromZero));
    }

    private static int ResolveDeaths(int currentPopulation, FaunaSpeciesDefinition species, float hungerPressure, int shortageMonths, float habitatSupport, BiologicalTraitProfile traits, FoodStressState foodStressState, float usableFoodRatio)
    {
        var mortality = Math.Max(0.0f, hungerPressure - FaunaSimulationConstants.MortalityHungerThreshold) * species.MortalitySensitivity * FaunaSimulationConstants.HungerMortalityWeight;
        mortality += shortageMonths * FaunaSimulationConstants.ShortageMortalityWeight * species.MortalitySensitivity;
        mortality += Math.Max(0.0f, 0.35f - habitatSupport) * species.PredatorVulnerability * FaunaSimulationConstants.PredatorRiskPenaltyWeight;

        if (foodStressState == FoodStressState.SevereShortage)
        {
            mortality += FaunaSimulationConstants.SevereShortageMortalityWeight * species.MortalitySensitivity;
        }
        else if (foodStressState == FoodStressState.Starvation)
        {
            mortality += FaunaSimulationConstants.StarvationMortalityWeight * species.MortalitySensitivity;
            if (usableFoodRatio <= 0.05f)
            {
                mortality += FaunaSimulationConstants.NoFoodMortalityBonus;
            }
        }

        mortality *= 0.92f + ((100 - traits.Resilience) / 100.0f * 0.18f);

        return Math.Min(currentPopulation, Math.Max(0, (int)MathF.Round(currentPopulation * mortality, MidpointRounding.AwayFromZero)));
    }

    private static float ResolveMigrationPressure(
        FaunaSpeciesDefinition species,
        float hungerPressure,
        int shortageMonths,
        FoodStressState foodStressState)
    {
        if (species.Mobility <= 0.0f)
        {
            return 0.0f;
        }

        var stressBonus = foodStressState switch
        {
            FoodStressState.SevereShortage => FaunaSimulationConstants.MigrationPressureFoodStressBonus,
            FoodStressState.Starvation => FaunaSimulationConstants.MigrationPressureFoodStressBonus * 1.5f,
            _ => 0.0f
        };

        return ClampNormalized(
            (hungerPressure * FaunaSimulationConstants.MigrationPressureHungerWeight) +
            (Math.Min(shortageMonths, 4) * FaunaSimulationConstants.MigrationPressureShortageWeight) +
            stressBonus +
            (species.Mobility * 0.12f));
    }

    private static MigrationPlan ResolveMigration(
        Region region,
        IReadOnlyDictionary<string, MutableRegionState> stateByRegionId,
        FaunaSpeciesDefinition species,
        int populationAfterMortality,
        float migrationPressure,
        int shortageMonths,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        if (populationAfterMortality <= FaunaSimulationConstants.MinimumMigrationPopulation ||
            (migrationPressure < FaunaSimulationConstants.MigrationHungerThreshold &&
             shortageMonths < FaunaSimulationConstants.MigrationShortageMonthsThreshold) ||
            species.Mobility <= 0.0f)
        {
            return MigrationPlan.None;
        }

        var currentScore = ScoreRegionForSpecies(region, species, stateByRegionId[region.Id], floraCatalog, faunaCatalog);
        var bestTarget = region.NeighborIds
            .Select(id => stateByRegionId.GetValueOrDefault(id))
            .Where(state => state is not null)
            .Cast<MutableRegionState>()
            .Select(state => new { State = state, Score = ScoreRegionForSpecies(state.Region, species, state, floraCatalog, faunaCatalog) })
            .OrderByDescending(item => item.Score)
            .FirstOrDefault();

        if (bestTarget is null || bestTarget.Score <= currentScore + FaunaSimulationConstants.MigrationScoreDeltaRequired)
        {
            return MigrationPlan.None;
        }

        var share = Math.Min(
            FaunaSimulationConstants.MigrationShareCap,
            (migrationPressure * 0.24f) +
            (Math.Min(shortageMonths, 4) * 0.03f) +
            (species.Mobility * FaunaSimulationConstants.MigrationShareWeight));
        var migratedPopulation = Math.Min(
            populationAfterMortality - 1,
            Math.Max(FaunaSimulationConstants.MinimumMigrationPopulation, (int)MathF.Round(populationAfterMortality * share, MidpointRounding.AwayFromZero)));

        return migratedPopulation <= 0
            ? MigrationPlan.None
            : new MigrationPlan(bestTarget.State.Region.Id, migratedPopulation);
    }

    private static float ScoreRegionForSpecies(
        Region region,
        FaunaSpeciesDefinition species,
        MutableRegionState state,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var habitatSupport = ResolveHabitatSupport(region, species, state.FaunaProfiles.GetValueOrDefault(species.Id)?.Traits ?? species.BaselineTraits);
        var dietSupport = ResolveRegionalDietSupport(species, state, floraCatalog, faunaCatalog);
        return (float)Math.Round((habitatSupport * 0.42f) + (Math.Min(1.0, dietSupport / Math.Max(1.0, species.RequiredIntake * 110.0)) * 0.58f), 2);
    }

    private static float ResolveRegionalDietSupport(
        FaunaSpeciesDefinition species,
        MutableRegionState state,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        // Migration and viability scoring should mirror the actual monthly food web:
        // preferred links define the core diet, while fallback links only count toward any
        // remaining unmet support budget after the preferred path is evaluated.
        var preferredLinks = species.DietLinks.Where(link => !link.IsFallback).ToArray();
        var fallbackLinks = species.DietLinks.Where(link => link.IsFallback).ToArray();
        var preferredSupport = ResolveDietSupport(preferredLinks, state, floraCatalog, faunaCatalog, includeFallbackPenalty: false);
        var remainingSupportNeed = Math.Max(0.0f, (species.RequiredIntake * 110.0f) - preferredSupport);
        var fallbackSupport = remainingSupportNeed <= 0.01f
            ? 0.0f
            : Math.Min(
                remainingSupportNeed,
                ResolveDietSupport(fallbackLinks, state, floraCatalog, faunaCatalog, includeFallbackPenalty: true));
        return preferredSupport + fallbackSupport;
    }

    private static float ResolveDietSupport(
        IReadOnlyList<FaunaDietLink> links,
        MutableRegionState state,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        bool includeFallbackPenalty)
    {
        var totalWeight = links.Sum(link => link.Weight);
        if (links.Count == 0 || totalWeight <= 0.0f)
        {
            return 0.0f;
        }

        var support = 0.0f;
        foreach (var link in links)
        {
            var linkSupport = ResolveDietLinkSupport(link, state, floraCatalog, faunaCatalog, includeFallbackPenalty);
            support += (float)(linkSupport * (link.Weight / totalWeight));
        }

        return support;
    }

    private static double ResolveDietLinkSupport(
        FaunaDietLink link,
        MutableRegionState state,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        bool includeFallbackPenalty)
    {
        var linkSupport = link.TargetKind switch
        {
            FaunaDietTargetKind.FloraSpecies => ResolveFloraSupport(state.Region, link.TargetSpeciesId, state.FloraPopulations, floraCatalog),
            FaunaDietTargetKind.FaunaSpecies => ResolvePreySupport(link.TargetSpeciesId, state.FaunaPopulations, faunaCatalog),
            FaunaDietTargetKind.ScavengePool => ResolveScavengeSupport(state.FaunaPopulations, faunaCatalog),
            _ => 0.0
        };

        if (includeFallbackPenalty)
        {
            linkSupport *= 1.0 - FaunaSimulationConstants.FallbackDietPenalty;
        }

        return linkSupport;
    }

    private static double ResolveFloraSupport(
        Region region,
        string floraSpeciesId,
        IReadOnlyDictionary<string, int> floraPopulations,
        FloraSpeciesCatalog floraCatalog)
    {
        if (!floraPopulations.TryGetValue(floraSpeciesId, out var population))
        {
            return 0.0;
        }

        var flora = floraCatalog.GetById(floraSpeciesId);
        return flora is null ? 0.0 : population * SubsistenceSupportModel.ResolveFloraSupportPerPopulation(region, flora);
    }

    private static double ResolvePreySupport(
        string faunaSpeciesId,
        IReadOnlyDictionary<string, int> faunaPopulations,
        FaunaSpeciesCatalog faunaCatalog)
    {
        if (!faunaPopulations.TryGetValue(faunaSpeciesId, out var population))
        {
            return 0.0;
        }

        var fauna = faunaCatalog.GetById(faunaSpeciesId);
        return fauna is null ? 0.0 : population * fauna.FoodYield * fauna.PredatorVulnerability * FaunaSimulationConstants.PreySupportAccessMultiplier;
    }

    private static double ResolveScavengeSupport(
        IReadOnlyDictionary<string, int> faunaPopulations,
        FaunaSpeciesCatalog faunaCatalog)
    {
        return faunaPopulations.Sum(entry =>
        {
            var prey = faunaCatalog.GetById(entry.Key);
            return prey is null ? 0.0 : entry.Value * prey.FoodYield * FaunaSimulationConstants.ScavengeSupportShareMultiplier;
        });
    }

    private static void ApplyMigration(
        IReadOnlyDictionary<string, MutableRegionState> stateByRegionId,
        FaunaSpeciesDefinition species,
        RegionalBiologicalProfile sourceProfile,
        MigrationPlan plan)
    {
        if (!stateByRegionId.TryGetValue(plan.TargetRegionId, out var targetState))
        {
            return;
        }

        targetState.FaunaPopulations[species.Id] = targetState.FaunaPopulations.TryGetValue(species.Id, out var existing)
            ? existing + plan.MigratedPopulation
            : plan.MigratedPopulation;

        if (!targetState.FaunaProfiles.TryGetValue(species.Id, out var targetProfile))
        {
            targetProfile = new RegionalBiologicalProfile
            {
                SpeciesId = species.Id,
                RegionId = targetState.Region.Id,
                Traits = sourceProfile.Traits.Clone(),
                ViabilityScore = sourceProfile.ViabilityScore,
                LastPopulation = plan.MigratedPopulation,
                HungerPressure = Math.Max(0.0f, sourceProfile.HungerPressure * 0.70f),
                ShortageMonths = Math.Max(0, sourceProfile.ShortageMonths - 1),
                FeedingMomentum = sourceProfile.FeedingMomentum * 0.85f
            };
            targetState.FaunaProfiles[species.Id] = targetProfile;
        }
        else
        {
            targetProfile.LastPopulation = targetState.FaunaPopulations[species.Id];
            targetProfile.HungerPressure = Math.Max(targetProfile.HungerPressure, sourceProfile.HungerPressure * 0.60f);
            targetProfile.ShortageMonths = Math.Max(targetProfile.ShortageMonths, sourceProfile.ShortageMonths - 1);
            targetProfile.FeedingMomentum = Math.Max(targetProfile.FeedingMomentum, sourceProfile.FeedingMomentum * 0.80f);
        }
    }

    private static float ResolveClimateFit(Region region, BiologicalTraitProfile traits)
    {
        var coldDemand = region.Biome switch
        {
            Biome.Tundra => 82,
            Biome.Highlands => 62,
            _ => 34
        };
        var heatDemand = region.Biome switch
        {
            Biome.Desert => 84,
            Biome.Wetlands => 54,
            Biome.Plains => 50,
            _ => 34
        };
        var droughtDemand = region.WaterAvailability switch
        {
            WaterAvailability.Low => 78,
            WaterAvailability.Medium => 44,
            _ => 20
        };

        var coldFit = 1.0f - MathF.Abs(traits.ColdTolerance - coldDemand) / 100.0f;
        var heatFit = 1.0f - MathF.Abs(traits.HeatTolerance - heatDemand) / 100.0f;
        var droughtFit = 1.0f - MathF.Abs(traits.DroughtTolerance - droughtDemand) / 100.0f;
        return Math.Clamp((coldFit * 0.34f) + (heatFit * 0.34f) + (droughtFit * 0.32f), 0.20f, 1.00f);
    }

    private static float GetFertilityFit(float fertility, float preferredMin, float preferredMax)
    {
        if (fertility >= preferredMin && fertility <= preferredMax)
        {
            return 1.0f;
        }

        var distance = fertility < preferredMin ? preferredMin - fertility : fertility - preferredMax;
        return ClampNormalized(1.0f - (distance / 0.40f));
    }

    private static string ResolveOutcome(int previousPopulation, int newPopulation, int migratedOut)
    {
        if (newPopulation == 0 && previousPopulation > 0)
        {
            return "WentExtinct";
        }

        if (migratedOut > 0 && newPopulation < previousPopulation)
        {
            return "Migrated";
        }

        if (newPopulation > previousPopulation)
        {
            return "Grew";
        }

        if (newPopulation < previousPopulation)
        {
            return "Declined";
        }

        return "Stabilized";
    }

    private static string ResolvePrimaryCause(float fulfillmentRatio, float hungerPressure, float habitatSupport, int migratedOut, int finalPopulation)
    {
        if (finalPopulation <= 0)
        {
            return "Collapse after prolonged shortage";
        }

        if (migratedOut > 0)
        {
            return "Shortage-driven migration";
        }

        if (hungerPressure >= 0.85f)
        {
            return "Starvation from unusable food shortfall";
        }

        if (hungerPressure >= 0.55f)
        {
            return "Persistent hunger pressure";
        }

        if (fulfillmentRatio < 0.70f)
        {
            return "Food web shortfall";
        }

        if (habitatSupport < 0.40f)
        {
            return "Weak habitat support";
        }

        return fulfillmentRatio >= 0.95f ? "Well fed in suitable habitat" : "Near ecological equilibrium";
    }

    private static string BuildConsumptionSummary(IReadOnlyDictionary<string, int> consumed)
    {
        return consumed.Count == 0
            ? "none"
            : string.Join(", ", consumed.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => $"{entry.Key}:{entry.Value}"));
    }

    private static void MergeInto(IDictionary<string, int> target, IReadOnlyDictionary<string, int> source)
    {
        foreach (var entry in source)
        {
            target[entry.Key] = target.TryGetValue(entry.Key, out var existing) ? existing + entry.Value : entry.Value;
        }
    }

    private static float ClampNormalized(float value)
    {
        return MathF.Round(Math.Clamp(value, 0.0f, 1.0f), 2);
    }

    private sealed class MutableRegionState
    {
        public MutableRegionState(Region region)
        {
            Region = region;
            FloraPopulations = new Dictionary<string, int>(region.Ecosystem.FloraPopulations, StringComparer.Ordinal);
            FaunaPopulations = new Dictionary<string, int>(region.Ecosystem.FaunaPopulations, StringComparer.Ordinal);
            FloraProfiles = region.Ecosystem.FloraProfiles.ToDictionary(entry => entry.Key, entry => entry.Value.Clone(), StringComparer.Ordinal);
            FaunaProfiles = region.Ecosystem.FaunaProfiles.ToDictionary(entry => entry.Key, entry => entry.Value.Clone(), StringComparer.Ordinal);
        }

        public Region Region { get; }
        public Dictionary<string, int> FloraPopulations { get; }
        public Dictionary<string, int> FaunaPopulations { get; }
        public Dictionary<string, RegionalBiologicalProfile> FloraProfiles { get; }
        public Dictionary<string, RegionalBiologicalProfile> FaunaProfiles { get; }

        public Region ToRegion()
        {
            return new Region(
                Region.Id,
                Region.Name,
                Region.Fertility,
                Region.Biome,
                Region.WaterAvailability,
                Region.NeighborIds,
                new RegionEcosystem(
                    Region.Ecosystem.ProtoLifeSubstrate,
                    FloraPopulations,
                    FaunaPopulations,
                    FloraProfiles,
                    FaunaProfiles,
                    Region.Ecosystem.FossilRecords.ToArray(),
                    Region.Ecosystem.BiologicalHistoryRecords.ToArray()),
                Region.MaterialProfile.Clone());
        }
    }

    private sealed record ConsumptionResult(
        float FoodConsumed,
        float FoodSourceFit,
        IReadOnlyDictionary<string, int> ConsumedFloraPopulations,
        IReadOnlyDictionary<string, int> ConsumedFaunaPopulations);

    private sealed record SourceConsumption(
        float FoodConsumed,
        float SourceFit,
        IReadOnlyDictionary<string, int> ConsumedFlora,
        IReadOnlyDictionary<string, int> ConsumedFauna)
    {
        public static SourceConsumption Empty()
        {
            return new SourceConsumption(
                0.0f,
                0.0f,
                new Dictionary<string, int>(StringComparer.Ordinal),
                new Dictionary<string, int>(StringComparer.Ordinal));
        }
    }

    private sealed record MigrationPlan(string TargetRegionId, int MigratedPopulation)
    {
        public static MigrationPlan None { get; } = new(string.Empty, 0);
    }

    private sealed record ScavengeCandidate(FaunaSpeciesDefinition Species, int AvailablePopulation, float AccessibleShare);
}
