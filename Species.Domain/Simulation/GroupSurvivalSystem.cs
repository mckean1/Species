using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Knowledge;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class GroupSurvivalSystem
{
    public GroupSurvivalResult Run(
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var mutableRegions = world.Regions.ToDictionary(
            region => region.Id,
            region => new MutableRegionState(
                region,
                new Dictionary<string, int>(region.Ecosystem.FloraPopulations, StringComparer.Ordinal),
                new Dictionary<string, int>(region.Ecosystem.FaunaPopulations, StringComparer.Ordinal)),
            StringComparer.Ordinal);

        var groupsByRegionId = world.PopulationGroups
            .GroupBy(group => group.CurrentRegionId, StringComparer.Ordinal)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray(), StringComparer.Ordinal);
        var survivalStates = new Dictionary<string, GroupSurvivalState>(StringComparer.Ordinal);

        foreach (var group in world.PopulationGroups)
        {
            var monthlyFoodNeed = SubsistenceSupportModel.CalculateMonthlyFoodNeed(group.Population);
            survivalStates[group.Id] = new GroupSurvivalState(
                group,
                monthlyFoodNeed,
                ResolvePrimaryAction(group.SubsistenceMode),
                ResolveFallbackAction(group.SubsistenceMode));
        }

        foreach (var regionGroups in groupsByRegionId)
        {
            if (!mutableRegions.TryGetValue(regionGroups.Key, out var regionState))
            {
                continue;
            }

            RunActionPhase(world, regionState, regionGroups.Value.Select(group => survivalStates[group.Id]).Where(state => state.PrimaryAction == "Gather"), "Gather", floraCatalog, faunaCatalog);
            RunActionPhase(world, regionState, regionGroups.Value.Select(group => survivalStates[group.Id]).Where(state => state.PrimaryAction == "Hunt"), "Hunt", floraCatalog, faunaCatalog);
            RunActionPhase(world, regionState, regionGroups.Value.Select(group => survivalStates[group.Id]).Where(state => state.FallbackAction == "Gather"), "Gather", floraCatalog, faunaCatalog, useFallback: true);
            RunActionPhase(world, regionState, regionGroups.Value.Select(group => survivalStates[group.Id]).Where(state => state.FallbackAction == "Hunt"), "Hunt", floraCatalog, faunaCatalog, useFallback: true);
        }

        var updatedGroups = new List<PopulationGroup>(world.PopulationGroups.Count);
        var changes = new List<GroupSurvivalChange>(world.PopulationGroups.Count);

        foreach (var group in world.PopulationGroups.OrderBy(group => group.Id, StringComparer.Ordinal))
        {
            if (!survivalStates.TryGetValue(group.Id, out var state) ||
                !mutableRegions.TryGetValue(group.CurrentRegionId, out var regionState))
            {
                updatedGroups.Add(CloneGroup(group));
                continue;
            }

            var totalFoodAcquired = state.PrimaryAcquisition.FoodGained + state.FallbackAcquisition.FoodGained;
            var effectiveStoredFood = ApplyStoredFoodEffect(group, state.StoredFoodBefore);
            var availableFood = totalFoodAcquired + effectiveStoredFood;
            var consumedForNeed = Math.Min(state.MonthlyFoodNeed, availableFood);
            var shortage = Math.Max(0, state.MonthlyFoodNeed - consumedForNeed);
            var storedFoodAfter = Math.Max(0, availableFood - consumedForNeed);
            var starvationLoss = CalculateStarvationLoss(group.Population, state.MonthlyFoodNeed, shortage);
            var finalPopulation = Math.Max(0, group.Population - starvationLoss);

            changes.Add(new GroupSurvivalChange
            {
                GroupId = group.Id,
                GroupName = group.Name,
                CurrentRegionId = group.CurrentRegionId,
                CurrentRegionName = regionState.Region.Name,
                SubsistenceMode = group.SubsistenceMode.ToString(),
                StartingPopulation = group.Population,
                MonthlyFoodNeed = state.MonthlyFoodNeed,
                PrimaryAction = state.PrimaryAction,
                PrimaryFoodGained = state.PrimaryAcquisition.FoodGained,
                PrimarySummary = state.PrimaryAcquisition.BuildSummary(),
                FallbackAction = state.FallbackAction,
                FallbackFoodGained = state.FallbackAcquisition.FoodGained,
                FallbackSummary = state.FallbackAcquisition.BuildSummary(),
                TotalFoodAcquired = totalFoodAcquired,
                StoredFoodBefore = state.StoredFoodBefore,
                StoredFoodAfter = storedFoodAfter,
                Shortage = shortage,
                StarvationLoss = starvationLoss,
                FinalPopulation = finalPopulation,
                Outcome = ResolveOutcome(group.Population, finalPopulation),
                SurvivalReason = ResolveSurvivalReason(state.MonthlyFoodNeed, totalFoodAcquired, state.StoredFoodBefore, shortage, starvationLoss)
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
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, updatedRegions, updatedGroups, world.Chronicle),
            changes);
    }

    private static void RunActionPhase(
        World world,
        MutableRegionState regionState,
        IEnumerable<GroupSurvivalState> participants,
        string action,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        bool useFallback = false)
    {
        var activeParticipants = participants
            .Where(state => state.RemainingNeed > 0)
            .ToArray();

        if (activeParticipants.Length == 0)
        {
            return;
        }

        switch (action)
        {
            case "Gather":
                AllocateFairly(
                    world,
                    regionState.Region,
                    regionState.FloraPopulations,
                    activeParticipants,
                    useFallback,
                    floraCatalog.GetById,
                    species => species.FoodValue,
                    SubsistenceSupportModel.ResolveGatheringMultiplier);
                break;
            case "Hunt":
                AllocateFairly(
                    world,
                    regionState.Region,
                    regionState.FaunaPopulations,
                    activeParticipants,
                    useFallback,
                    faunaCatalog.GetById,
                    species => species.FoodYield,
                    SubsistenceSupportModel.ResolveHuntingMultiplier);
                break;
        }
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

    private static void AllocateFairly<TDefinition>(
        World world,
        Region region,
        IDictionary<string, int> mutablePopulations,
        IReadOnlyList<GroupSurvivalState> participants,
        bool useFallback,
        Func<string, TDefinition?> resolveDefinition,
        Func<TDefinition, float> resolveBaseFoodValue,
        Func<PopulationGroup, Region, float> resolveMultiplier)
        where TDefinition : class
    {
        if (mutablePopulations.Count == 0 || participants.Count == 0)
        {
            return;
        }

        var sourceStates = mutablePopulations
            .Where(entry => entry.Value > 0)
            .Select(entry =>
            {
                var definition = resolveDefinition(entry.Key);
                return definition is null
                    ? null
                    : new SourceState(
                        entry.Key,
                        entry.Value,
                        resolveBaseFoodValue(definition),
                        (float)participants.Average(state => SubsistenceSupportModel.ResolveFoodPerUnit(resolveBaseFoodValue(definition), resolveMultiplier(state.Group, region))));
            })
            .Where(source => source is not null)
            .Select(source => source!)
            .OrderByDescending(source => source.SortFoodPerUnit)
            .ThenBy(source => source.Id, StringComparer.Ordinal)
            .ToArray();

        foreach (var source in sourceStates)
        {
            if (!mutablePopulations.TryGetValue(source.Id, out var availableUnits) || availableUnits <= 0)
            {
                continue;
            }

            var demands = participants
                .Where(state => state.RemainingNeed > 0)
                .Select(state =>
                {
                    var foodPerUnit = SubsistenceSupportModel.ResolveFoodPerUnit(source.BaseFoodValue, resolveMultiplier(state.Group, region));
                    var neededUnits = (int)MathF.Ceiling(state.RemainingNeed / (float)foodPerUnit);
                    return new GroupDemand(state, foodPerUnit, Math.Max(0, neededUnits));
                })
                .Where(demand => demand.UnitsDemanded > 0)
                .ToArray();

            if (demands.Length == 0)
            {
                break;
            }

            var totalDemand = demands.Sum(demand => demand.UnitsDemanded);
            var allocations = totalDemand <= availableUnits
                ? demands.ToDictionary(demand => demand.State.Group.Id, demand => demand.UnitsDemanded, StringComparer.Ordinal)
                : AllocateUnitsByShare(world, region.Id, source.Id, availableUnits, demands);

            var totalAllocated = 0;
            foreach (var demand in demands)
            {
                if (!allocations.TryGetValue(demand.State.Group.Id, out var unitsTaken) || unitsTaken <= 0)
                {
                    continue;
                }

                totalAllocated += unitsTaken;
                demand.State.ApplyFood(source.Id, demand.FoodPerUnit, unitsTaken, useFallback);
            }

            mutablePopulations[source.Id] = Math.Max(0, availableUnits - totalAllocated);
            if (mutablePopulations[source.Id] == 0)
            {
                mutablePopulations.Remove(source.Id);
            }
        }
    }

    private static Dictionary<string, int> AllocateUnitsByShare(
        World world,
        string regionId,
        string sourceId,
        int availableUnits,
        IReadOnlyList<GroupDemand> demands)
    {
        var allocations = new Dictionary<string, int>(StringComparer.Ordinal);
        var exactShares = new List<ExactShare>(demands.Count);
        var totalDemand = demands.Sum(demand => demand.UnitsDemanded);
        var allocated = 0;

        foreach (var demand in demands)
        {
            var exact = availableUnits * (demand.UnitsDemanded / (float)totalDemand);
            var baseUnits = Math.Min(demand.UnitsDemanded, (int)MathF.Floor(exact));
            allocations[demand.State.Group.Id] = baseUnits;
            allocated += baseUnits;
            exactShares.Add(new ExactShare(demand.State, demand.UnitsDemanded, exact - baseUnits));
        }

        var remainingUnits = availableUnits - allocated;
        foreach (var share in exactShares
                     .OrderByDescending(share => share.Remainder)
                     .ThenBy(share => ComputeRotationPriority(world, regionId, sourceId, share.State.Group.Id)))
        {
            if (remainingUnits <= 0)
            {
                break;
            }

            if (allocations[share.State.Group.Id] >= share.UnitsDemanded)
            {
                continue;
            }

            allocations[share.State.Group.Id]++;
            remainingUnits--;
        }

        return allocations;
    }

    private static int ComputeRotationPriority(World world, string regionId, string sourceId, string groupId)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var value in $"{world.Seed}|{world.CurrentYear}|{world.CurrentMonth}|{regionId}|{sourceId}|{groupId}")
            {
                hash ^= value;
                hash *= 16777619;
            }

            return (int)hash;
        }
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
            LastRegionId = group.LastRegionId,
            MonthsSinceLastMove = group.MonthsSinceLastMove,
            Pressures = new PressureState
            {
                FoodPressure = group.Pressures.FoodPressure,
                WaterPressure = group.Pressures.WaterPressure,
                ThreatPressure = group.Pressures.ThreatPressure,
                OvercrowdingPressure = group.Pressures.OvercrowdingPressure,
                MigrationPressure = group.Pressures.MigrationPressure
            },
            KnownRegionIds = new HashSet<string>(group.KnownRegionIds, StringComparer.Ordinal),
            KnownDiscoveryIds = new HashSet<string>(group.KnownDiscoveryIds, StringComparer.Ordinal),
            DiscoveryEvidence = group.DiscoveryEvidence.Clone(),
            LearnedAdvancementIds = new HashSet<string>(group.LearnedAdvancementIds, StringComparer.Ordinal),
            AdvancementEvidence = group.AdvancementEvidence.Clone()
        };
    }

    private static int ApplyStoredFoodEffect(PopulationGroup group, int storedFoodBefore)
    {
        if (!group.LearnedAdvancementIds.Contains(AdvancementCatalog.FoodStorageId))
        {
            return storedFoodBefore;
        }

        return (int)MathF.Round(storedFoodBefore * AdvancementConstants.FoodStorageStoredFoodMultiplier, MidpointRounding.AwayFromZero);
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

    private sealed class GroupSurvivalState
    {
        public GroupSurvivalState(PopulationGroup group, int monthlyFoodNeed, string primaryAction, string fallbackAction)
        {
            Group = group;
            MonthlyFoodNeed = monthlyFoodNeed;
            RemainingNeed = monthlyFoodNeed;
            PrimaryAction = primaryAction;
            FallbackAction = fallbackAction;
            StoredFoodBefore = group.StoredFood;
            PrimaryAcquisition = new AcquisitionState(primaryAction);
            FallbackAcquisition = new AcquisitionState(fallbackAction);
        }

        public PopulationGroup Group { get; }

        public int MonthlyFoodNeed { get; }

        public int RemainingNeed { get; private set; }

        public string PrimaryAction { get; }

        public string FallbackAction { get; }

        public int StoredFoodBefore { get; }

        public AcquisitionState PrimaryAcquisition { get; }

        public AcquisitionState FallbackAcquisition { get; }

        public void ApplyFood(string sourceId, int foodPerUnit, int unitsTaken, bool useFallback)
        {
            if (unitsTaken <= 0)
            {
                return;
            }

            var acquisition = useFallback ? FallbackAcquisition : PrimaryAcquisition;
            var foodGained = unitsTaken * foodPerUnit;
            acquisition.Add(sourceId, unitsTaken, foodGained);
            RemainingNeed = Math.Max(0, RemainingNeed - foodGained);
        }
    }

    private sealed class AcquisitionState
    {
        private readonly Dictionary<string, int> consumed = new(StringComparer.Ordinal);

        public AcquisitionState(string action)
        {
            Action = action;
        }

        public string Action { get; }

        public int FoodGained { get; private set; }

        public void Add(string sourceId, int unitsTaken, int foodGained)
        {
            consumed[sourceId] = consumed.TryGetValue(sourceId, out var currentUnits)
                ? currentUnits + unitsTaken
                : unitsTaken;
            FoodGained += foodGained;
        }

        public string BuildSummary()
        {
            var consumedSummary = consumed.Count == 0
                ? "none"
                : string.Join(", ", consumed.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => $"{entry.Key}:{entry.Value}"));

            return $"{Action} gained {FoodGained} food from [{consumedSummary}]";
        }
    }

    private sealed record SourceState(string Id, int AvailablePopulation, float BaseFoodValue, float SortFoodPerUnit);

    private sealed record GroupDemand(GroupSurvivalState State, int FoodPerUnit, int UnitsDemanded);

    private sealed record ExactShare(GroupSurvivalState State, int UnitsDemanded, float Remainder);
}
