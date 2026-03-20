using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class FaunaSimulationSystem
{
    public FaunaSimulationResult Run(
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var updatedRegions = new List<Region>(world.Regions.Count);
        var changes = new List<FaunaPopulationChange>();

        foreach (var region in world.Regions)
        {
            var mutableFlora = new Dictionary<string, int>(region.Ecosystem.FloraPopulations, StringComparer.Ordinal);
            var mutableFauna = new Dictionary<string, int>(region.Ecosystem.FaunaPopulations, StringComparer.Ordinal);

            foreach (var faunaEntry in region.Ecosystem.FaunaPopulations.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                var species = faunaCatalog.GetById(faunaEntry.Key);
                if (species is null)
                {
                    continue;
                }

                var startingPopulation = faunaEntry.Value;
                var currentPopulation = mutableFauna.TryGetValue(species.Id, out var currentValue)
                    ? currentValue
                    : 0;
                var habitatSupport = GetHabitatSupport(region, species);
                var foodNeeded = currentPopulation * species.FoodRequirement;
                var consumption = species.DietCategory switch
                {
                    DietCategory.Herbivore => ConsumeFloraFood(foodNeeded, mutableFlora, floraCatalog),
                    DietCategory.Carnivore => ConsumeFaunaFood(foodNeeded, species.Id, mutableFauna, faunaCatalog),
                    DietCategory.Omnivore => ConsumeOmnivoreFood(foodNeeded, species.Id, mutableFlora, mutableFauna, floraCatalog, faunaCatalog),
                    _ => FoodConsumptionResult.Empty()
                };

                var fulfillmentRatio = foodNeeded <= 0.0f
                    ? 1.0f
                    : MathF.Min(1.0f, consumption.FoodConsumed / foodNeeded);
                var newPopulation = AdjustPopulation(currentPopulation, fulfillmentRatio, habitatSupport, species.ReproductionRate);

                if (newPopulation > 0)
                {
                    mutableFauna[species.Id] = newPopulation;
                }
                else
                {
                    mutableFauna.Remove(species.Id);
                }

                changes.Add(new FaunaPopulationChange
                {
                    RegionId = region.Id,
                    RegionName = region.Name,
                    FaunaSpeciesId = species.Id,
                    FaunaSpeciesName = species.Name,
                    PreviousPopulation = startingPopulation,
                    NewPopulation = newPopulation,
                    FoodNeeded = MathF.Round(foodNeeded, 2),
                    FoodConsumed = MathF.Round(consumption.FoodConsumed, 2),
                    FulfillmentRatio = MathF.Round(fulfillmentRatio, 2),
                    HabitatSupport = MathF.Round(habitatSupport, 2),
                    Outcome = ResolveOutcome(startingPopulation, newPopulation),
                    ConsumedFloraSummary = BuildConsumptionSummary(consumption.ConsumedFloraPopulations),
                    ConsumedFaunaSummary = BuildConsumptionSummary(consumption.ConsumedFaunaPopulations),
                    PrimaryCause = ResolvePrimaryCause(habitatSupport, fulfillmentRatio, startingPopulation, newPopulation)
                });
            }

            updatedRegions.Add(new Region(
                region.Id,
                region.Name,
                region.Fertility,
                region.Biome,
                region.WaterAvailability,
                region.NeighborIds,
                new RegionEcosystem(mutableFlora, mutableFauna)));
        }

        return new FaunaSimulationResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, updatedRegions, world.PopulationGroups, world.Chronicle, world.Polities, world.FocalPolityId),
            changes);
    }

    private static float GetHabitatSupport(Region region, FaunaSpeciesDefinition species)
    {
        if (!species.SupportedWaterAvailabilities.Contains(region.WaterAvailability))
        {
            return FaunaSimulationConstants.UnsupportedWaterHabitatSupport;
        }

        return species.CoreBiomes.Contains(region.Biome)
            ? FaunaSimulationConstants.CoreBiomeHabitatSupport
            : FaunaSimulationConstants.NonCoreBiomeHabitatSupport;
    }

    private static FoodConsumptionResult ConsumeOmnivoreFood(
        float totalNeed,
        string predatorSpeciesId,
        IDictionary<string, int> floraPopulations,
        IDictionary<string, int> faunaPopulations,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var plantNeed = totalNeed * FaunaSimulationConstants.OmnivorePlantShare;
        var animalNeed = totalNeed - plantNeed;
        var floraConsumption = ConsumeFloraFood(plantNeed, floraPopulations, floraCatalog);
        var faunaConsumption = ConsumeFaunaFood(animalNeed, predatorSpeciesId, faunaPopulations, faunaCatalog);

        var remainingNeed = totalNeed - floraConsumption.FoodConsumed - faunaConsumption.FoodConsumed;
        if (remainingNeed > 0.01f)
        {
            var fallbackFlora = ConsumeFloraFood(remainingNeed, floraPopulations, floraCatalog);
            remainingNeed -= fallbackFlora.FoodConsumed;
            floraConsumption = floraConsumption.Combine(fallbackFlora);

            if (remainingNeed > 0.01f)
            {
                var fallbackFauna = ConsumeFaunaFood(remainingNeed, predatorSpeciesId, faunaPopulations, faunaCatalog);
                faunaConsumption = faunaConsumption.Combine(fallbackFauna);
            }
        }

        return floraConsumption.MergeWith(faunaConsumption);
    }

    private static FoodConsumptionResult ConsumeFloraFood(
        float requestedFood,
        IDictionary<string, int> floraPopulations,
        FloraSpeciesCatalog floraCatalog)
    {
        var candidates = floraPopulations
            .Where(entry => entry.Value > 0)
            .Select(entry =>
            {
                var species = floraCatalog.GetById(entry.Key);
                return species is null
                    ? null
                    : new FoodSource(entry.Key, entry.Value, species.FoodValue);
            })
            .Where(source => source is not null)
            .Select(source => source!)
            .ToList();

        return ConsumeFromPool(requestedFood, candidates, floraPopulations, isFaunaPool: false);
    }

    private static FoodConsumptionResult ConsumeFaunaFood(
        float requestedFood,
        string predatorSpeciesId,
        IDictionary<string, int> faunaPopulations,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var candidates = faunaPopulations
            .Where(entry => entry.Key != predatorSpeciesId && entry.Value > 0)
            .Select(entry =>
            {
                var species = faunaCatalog.GetById(entry.Key);
                return species is null
                    ? null
                    : new FoodSource(entry.Key, entry.Value, species.FoodYield);
            })
            .Where(source => source is not null)
            .Select(source => source!)
            .ToList();

        return ConsumeFromPool(requestedFood, candidates, faunaPopulations, isFaunaPool: true);
    }

    private static FoodConsumptionResult ConsumeFromPool(
        float requestedFood,
        IReadOnlyList<FoodSource> candidates,
        IDictionary<string, int> mutablePopulations,
        bool isFaunaPool)
    {
        if (requestedFood <= 0.0f || candidates.Count == 0)
        {
            return FoodConsumptionResult.Empty();
        }

        var availableFood = candidates.Sum(candidate => candidate.AvailablePopulation * candidate.FoodPerPopulation);
        if (availableFood <= 0.0f)
        {
            return FoodConsumptionResult.Empty();
        }

        var targetFood = MathF.Min(requestedFood, availableFood);
        var consumedCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var foodConsumed = 0.0f;

        foreach (var candidate in candidates)
        {
            var desiredFoodShare = targetFood * ((candidate.AvailablePopulation * candidate.FoodPerPopulation) / availableFood);
            var desiredPopulationConsumption = desiredFoodShare / candidate.FoodPerPopulation;
            var consumedPopulation = Math.Min(candidate.AvailablePopulation, (int)MathF.Floor(desiredPopulationConsumption));
            if (consumedPopulation <= 0)
            {
                continue;
            }

            mutablePopulations[candidate.Id] -= consumedPopulation;
            if (mutablePopulations[candidate.Id] <= 0)
            {
                mutablePopulations.Remove(candidate.Id);
            }

            consumedCounts[candidate.Id] = consumedPopulation;
            foodConsumed += consumedPopulation * candidate.FoodPerPopulation;
        }

        var remainingFood = targetFood - foodConsumed;
        if (remainingFood > 0.01f)
        {
            foreach (var candidate in candidates.OrderByDescending(candidate => candidate.FoodPerPopulation))
            {
                var remainingPopulation = mutablePopulations.TryGetValue(candidate.Id, out var remainingValue)
                    ? remainingValue
                    : 0;
                if (remainingPopulation <= 0)
                {
                    continue;
                }

                mutablePopulations[candidate.Id] = remainingPopulation - 1;
                if (mutablePopulations[candidate.Id] <= 0)
                {
                    mutablePopulations.Remove(candidate.Id);
                }

                consumedCounts[candidate.Id] = consumedCounts.TryGetValue(candidate.Id, out var existingCount)
                    ? existingCount + 1
                    : 1;
                foodConsumed += candidate.FoodPerPopulation;
                remainingFood -= candidate.FoodPerPopulation;

                if (remainingFood <= 0.01f)
                {
                    break;
                }
            }
        }

        return isFaunaPool
            ? FoodConsumptionResult.ForFauna(foodConsumed, consumedCounts)
            : FoodConsumptionResult.ForFlora(foodConsumed, consumedCounts);
    }

    private static int AdjustPopulation(
        int currentPopulation,
        float fulfillmentRatio,
        float habitatSupport,
        float reproductionRate)
    {
        var targetMultiplier =
            (fulfillmentRatio * FaunaSimulationConstants.FoodFulfillmentWeight) +
            (habitatSupport * FaunaSimulationConstants.HabitatSupportWeight) +
            (fulfillmentRatio * reproductionRate * FaunaSimulationConstants.ReproductionGrowthWeight);
        var targetPopulation = Math.Max(0, (int)MathF.Round(currentPopulation * targetMultiplier, MidpointRounding.AwayFromZero));
        var adjustmentRate = MathF.Max(
            FaunaSimulationConstants.MinimumAdjustmentRate,
            (reproductionRate * FaunaSimulationConstants.ReproductionAdjustmentWeight) + FaunaSimulationConstants.MinimumAdjustmentRate);
        var updatedValue = currentPopulation + ((targetPopulation - currentPopulation) * adjustmentRate);
        var nextPopulation = Math.Max(0, (int)MathF.Round(updatedValue, MidpointRounding.AwayFromZero));

        if (targetPopulation == 0 && nextPopulation <= FaunaSimulationConstants.ExtinctionThresholdPopulation)
        {
            return 0;
        }

        return nextPopulation;
    }

    private static string ResolveOutcome(int previousPopulation, int newPopulation)
    {
        if (newPopulation == 0 && previousPopulation > 0)
        {
            return "WentExtinct";
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

    private static string ResolvePrimaryCause(float habitatSupport, float fulfillmentRatio, int previousPopulation, int newPopulation)
    {
        if (newPopulation == 0 && previousPopulation > 0)
        {
            return "Regional extinction";
        }

        if (fulfillmentRatio < 0.40f)
        {
            return "Food shortfall";
        }

        if (habitatSupport < 0.40f)
        {
            return "Weak habitat support";
        }

        if (newPopulation > previousPopulation)
        {
            return "Well fed in suitable habitat";
        }

        if (newPopulation < previousPopulation)
        {
            return "Below replacement level";
        }

        return "Near equilibrium";
    }

    private static string BuildConsumptionSummary(IReadOnlyDictionary<string, int> consumed)
    {
        return consumed.Count == 0
            ? "none"
            : string.Join(", ", consumed
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => $"{entry.Key}:{entry.Value}"));
    }

    private sealed record FoodSource(string Id, int AvailablePopulation, float FoodPerPopulation);

    private sealed class FoodConsumptionResult
    {
        private FoodConsumptionResult(
            float foodConsumed,
            IReadOnlyDictionary<string, int> consumedFloraPopulations,
            IReadOnlyDictionary<string, int> consumedFaunaPopulations)
        {
            FoodConsumed = foodConsumed;
            ConsumedFloraPopulations = consumedFloraPopulations;
            ConsumedFaunaPopulations = consumedFaunaPopulations;
        }

        public float FoodConsumed { get; }

        public IReadOnlyDictionary<string, int> ConsumedFloraPopulations { get; }

        public IReadOnlyDictionary<string, int> ConsumedFaunaPopulations { get; }

        public static FoodConsumptionResult Empty()
        {
            return new FoodConsumptionResult(
                0.0f,
                new Dictionary<string, int>(StringComparer.Ordinal),
                new Dictionary<string, int>(StringComparer.Ordinal));
        }

        public static FoodConsumptionResult ForFlora(float foodConsumed, IReadOnlyDictionary<string, int> consumedFloraPopulations)
        {
            return new FoodConsumptionResult(foodConsumed, consumedFloraPopulations, new Dictionary<string, int>(StringComparer.Ordinal));
        }

        public static FoodConsumptionResult ForFauna(float foodConsumed, IReadOnlyDictionary<string, int> consumedFaunaPopulations)
        {
            return new FoodConsumptionResult(foodConsumed, new Dictionary<string, int>(StringComparer.Ordinal), consumedFaunaPopulations);
        }

        public FoodConsumptionResult Combine(FoodConsumptionResult other)
        {
            return new FoodConsumptionResult(
                FoodConsumed + other.FoodConsumed,
                MergeDictionaries(ConsumedFloraPopulations, other.ConsumedFloraPopulations),
                MergeDictionaries(ConsumedFaunaPopulations, other.ConsumedFaunaPopulations));
        }

        public FoodConsumptionResult MergeWith(FoodConsumptionResult other)
        {
            return Combine(other);
        }

        private static IReadOnlyDictionary<string, int> MergeDictionaries(
            IReadOnlyDictionary<string, int> left,
            IReadOnlyDictionary<string, int> right)
        {
            var merged = new Dictionary<string, int>(left, StringComparer.Ordinal);

            foreach (var entry in right)
            {
                merged[entry.Key] = merged.TryGetValue(entry.Key, out var existingValue)
                    ? existingValue + entry.Value
                    : entry.Value;
            }

            return merged;
        }
    }
}
