using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class GroupSurvivalSystem
{
    public GroupSurvivalResult Run(
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var mutableRegions = world.Regions.ToDictionary(
            region => region.Id,
            region => new MutableRegionState(
                region,
                new Dictionary<string, int>(region.Ecosystem.FloraPopulations, StringComparer.Ordinal),
                new Dictionary<string, int>(region.Ecosystem.FaunaPopulations, StringComparer.Ordinal)),
            StringComparer.Ordinal);

        var updatedGroups = new List<PopulationGroup>(world.PopulationGroups.Count);
        var changes = new List<GroupSurvivalChange>(world.PopulationGroups.Count);

        foreach (var group in world.PopulationGroups.OrderBy(group => group.Id, StringComparer.Ordinal))
        {
            if (!mutableRegions.TryGetValue(group.CurrentRegionId, out var regionState))
            {
                updatedGroups.Add(CloneGroup(group));
                continue;
            }

            var monthlyFoodNeed = CalculateMonthlyFoodNeed(group.Population);
            var storedFoodBefore = group.StoredFood;
            var primaryAction = ResolvePrimaryAction(group.SubsistenceMode);
            var fallbackAction = ResolveFallbackAction(group.SubsistenceMode);

            var primaryAcquisition = AcquireFood(primaryAction, monthlyFoodNeed, regionState, floraCatalog, faunaCatalog);
            var remainingNeed = Math.Max(0, monthlyFoodNeed - primaryAcquisition.FoodGained);
            var fallbackAcquisition = remainingNeed > 0
                ? AcquireFood(fallbackAction, remainingNeed, regionState, floraCatalog, faunaCatalog)
                : AcquisitionResult.Empty(fallbackAction);

            var totalFoodAcquired = primaryAcquisition.FoodGained + fallbackAcquisition.FoodGained;
            var availableFood = totalFoodAcquired + storedFoodBefore;
            var consumedForNeed = Math.Min(monthlyFoodNeed, availableFood);
            var shortage = Math.Max(0, monthlyFoodNeed - consumedForNeed);
            var storedFoodAfter = Math.Max(0, availableFood - consumedForNeed);
            var starvationLoss = CalculateStarvationLoss(group.Population, monthlyFoodNeed, shortage);
            var finalPopulation = Math.Max(0, group.Population - starvationLoss);

            changes.Add(new GroupSurvivalChange
            {
                GroupId = group.Id,
                GroupName = group.Name,
                CurrentRegionId = group.CurrentRegionId,
                CurrentRegionName = regionState.Region.Name,
                SubsistenceMode = group.SubsistenceMode.ToString(),
                StartingPopulation = group.Population,
                MonthlyFoodNeed = monthlyFoodNeed,
                PrimaryAction = primaryAction,
                PrimaryFoodGained = primaryAcquisition.FoodGained,
                PrimarySummary = primaryAcquisition.Summary,
                FallbackAction = fallbackAction,
                FallbackFoodGained = fallbackAcquisition.FoodGained,
                FallbackSummary = fallbackAcquisition.Summary,
                TotalFoodAcquired = totalFoodAcquired,
                StoredFoodBefore = storedFoodBefore,
                StoredFoodAfter = storedFoodAfter,
                Shortage = shortage,
                StarvationLoss = starvationLoss,
                FinalPopulation = finalPopulation,
                Outcome = ResolveOutcome(group.Population, finalPopulation),
                SurvivalReason = ResolveSurvivalReason(monthlyFoodNeed, totalFoodAcquired, storedFoodBefore, shortage, starvationLoss)
            });

            if (finalPopulation > 0)
            {
                var updatedGroup = CloneGroup(group);
                updatedGroup.StoredFood = storedFoodAfter;
                updatedGroup.Population = finalPopulation;
                updatedGroups.Add(updatedGroup);
            }
        }

        var updatedRegions = world.Regions
            .Select(region => mutableRegions[region.Id].ToRegion())
            .ToArray();

        return new GroupSurvivalResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, updatedRegions, updatedGroups),
            changes);
    }

    private static int CalculateMonthlyFoodNeed(int population)
    {
        return (int)MathF.Ceiling(population * GroupSurvivalConstants.FoodNeedPerPopulationUnit);
    }

    private static string ResolvePrimaryAction(SubsistenceMode subsistenceMode)
    {
        return subsistenceMode switch
        {
            SubsistenceMode.Hunter => "Hunt",
            SubsistenceMode.Gatherer => "Gather",
            SubsistenceMode.Mixed => "Gather",
            _ => "Gather"
        };
    }

    private static string ResolveFallbackAction(SubsistenceMode subsistenceMode)
    {
        return subsistenceMode switch
        {
            SubsistenceMode.Hunter => "Gather",
            SubsistenceMode.Gatherer => "Hunt",
            SubsistenceMode.Mixed => "Hunt",
            _ => "Hunt"
        };
    }

    private static AcquisitionResult AcquireFood(
        string action,
        int neededFood,
        MutableRegionState regionState,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        return action switch
        {
            "Gather" => GatherFood(neededFood, regionState, floraCatalog),
            "Hunt" => HuntFood(neededFood, regionState, faunaCatalog),
            _ => AcquisitionResult.Empty(action)
        };
    }

    private static AcquisitionResult GatherFood(
        int neededFood,
        MutableRegionState regionState,
        FloraSpeciesCatalog floraCatalog)
    {
        var foodSources = regionState.FloraPopulations
            .Where(entry => entry.Value > 0)
            .Select(entry =>
            {
                var species = floraCatalog.GetById(entry.Key);
                return species is null
                    ? null
                    : new FoodSource(entry.Key, entry.Value, Math.Max(1, (int)MathF.Round(species.FoodValue * GroupSurvivalConstants.FoodUnitScale, MidpointRounding.AwayFromZero)));
            })
            .Where(source => source is not null)
            .Select(source => source!)
            .ToArray();

        return ConsumeSources("Gather", neededFood, foodSources, regionState.FloraPopulations);
    }

    private static AcquisitionResult HuntFood(
        int neededFood,
        MutableRegionState regionState,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var foodSources = regionState.FaunaPopulations
            .Where(entry => entry.Value > 0)
            .Select(entry =>
            {
                var species = faunaCatalog.GetById(entry.Key);
                return species is null
                    ? null
                    : new FoodSource(entry.Key, entry.Value, Math.Max(1, (int)MathF.Round(species.FoodYield * GroupSurvivalConstants.FoodUnitScale, MidpointRounding.AwayFromZero)));
            })
            .Where(source => source is not null)
            .Select(source => source!)
            .ToArray();

        return ConsumeSources("Hunt", neededFood, foodSources, regionState.FaunaPopulations);
    }

    private static AcquisitionResult ConsumeSources(
        string action,
        int neededFood,
        IReadOnlyList<FoodSource> sources,
        IDictionary<string, int> mutablePopulations)
    {
        if (neededFood <= 0 || sources.Count == 0)
        {
            return AcquisitionResult.Empty(action);
        }

        var remainingNeed = neededFood;
        var consumed = new Dictionary<string, int>(StringComparer.Ordinal);
        var foodGained = 0;

        foreach (var source in sources
                     .OrderByDescending(source => source.FoodPerUnit)
                     .ThenBy(source => source.Id, StringComparer.Ordinal))
        {
            if (remainingNeed <= 0)
            {
                break;
            }

            var unitsNeeded = (int)MathF.Ceiling(remainingNeed / (float)source.FoodPerUnit);
            var unitsTaken = Math.Min(source.AvailablePopulation, unitsNeeded);

            mutablePopulations[source.Id] -= unitsTaken;
            if (mutablePopulations[source.Id] <= 0)
            {
                mutablePopulations.Remove(source.Id);
            }

            consumed[source.Id] = unitsTaken;
            foodGained += unitsTaken * source.FoodPerUnit;
            remainingNeed -= unitsTaken * source.FoodPerUnit;
        }

        return new AcquisitionResult(action, foodGained, BuildSummary(action, consumed, foodGained));
    }

    private static int CalculateStarvationLoss(int startingPopulation, int monthlyFoodNeed, int shortage)
    {
        if (startingPopulation <= 0 || monthlyFoodNeed <= 0 || shortage <= 0)
        {
            return 0;
        }

        var shortageRatio = shortage / (float)monthlyFoodNeed;
        return Math.Min(
            startingPopulation,
            (int)MathF.Ceiling(startingPopulation * shortageRatio * GroupSurvivalConstants.StarvationLossSeverity));
    }

    private static string ResolveOutcome(int startingPopulation, int finalPopulation)
    {
        if (finalPopulation <= 0 && startingPopulation > 0)
        {
            return "WentExtinct";
        }

        if (finalPopulation < startingPopulation)
        {
            return "Declined";
        }

        return "Survived";
    }

    private static string ResolveSurvivalReason(int monthlyFoodNeed, int acquiredFood, int storedFoodBefore, int shortage, int starvationLoss)
    {
        if (starvationLoss > 0)
        {
            return $"Group starved due to severe shortfall of {shortage}.";
        }

        if (storedFoodBefore > 0 && acquiredFood < monthlyFoodNeed)
        {
            return "Group used StoredFood to help cover monthly need.";
        }

        return $"Group acquired {acquiredFood} food and covered monthly need.";
    }

    private static string BuildSummary(string action, IReadOnlyDictionary<string, int> consumed, int foodGained)
    {
        var consumedSummary = consumed.Count == 0
            ? "none"
            : string.Join(", ", consumed.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => $"{entry.Key}:{entry.Value}"));

        return $"{action} gained {foodGained} food from [{consumedSummary}]";
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
            Pressures = new PressureState
            {
                FoodPressure = group.Pressures.FoodPressure,
                WaterPressure = group.Pressures.WaterPressure,
                ThreatPressure = group.Pressures.ThreatPressure,
                OvercrowdingPressure = group.Pressures.OvercrowdingPressure,
                MigrationPressure = group.Pressures.MigrationPressure
            },
            KnownRegionIds = new HashSet<string>(group.KnownRegionIds, StringComparer.Ordinal),
            KnownDiscoveryIds = new HashSet<string>(group.KnownDiscoveryIds, StringComparer.Ordinal)
        };
    }

    private sealed record FoodSource(string Id, int AvailablePopulation, int FoodPerUnit);

    private sealed record AcquisitionResult(string Action, int FoodGained, string Summary)
    {
        public static AcquisitionResult Empty(string action)
        {
            return new AcquisitionResult(action, 0, $"{action} gained 0 food from [none]");
        }
    }

    private sealed class MutableRegionState
    {
        public MutableRegionState(Region region, Dictionary<string, int> floraPopulations, Dictionary<string, int> faunaPopulations)
        {
            Region = region;
            FloraPopulations = floraPopulations;
            FaunaPopulations = faunaPopulations;
        }

        public Region Region { get; }

        public Dictionary<string, int> FloraPopulations { get; }

        public Dictionary<string, int> FaunaPopulations { get; }

        public Region ToRegion()
        {
            return new Region(
                Region.Id,
                Region.Name,
                Region.Fertility,
                Region.Biome,
                Region.WaterAvailability,
                Region.NeighborIds,
                new RegionEcosystem(FloraPopulations, FaunaPopulations));
        }
    }
}
