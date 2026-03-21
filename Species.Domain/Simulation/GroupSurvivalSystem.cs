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
        var discoveryCatalog = DiscoveryCatalog.CreateForWorld(world);
        var updatedPolities = world.Polities
            .Select(polity => polity.Clone())
            .ToArray();
        var settlementLookup = updatedPolities
            .SelectMany(polity => polity.Settlements
                .Where(settlement => settlement.IsActive)
                .Select(settlement => new
                {
                    Key = BuildSettlementLookupKey(polity.Id, settlement.RegionId),
                    Settlement = settlement
                }))
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(
                grouping => grouping.Key,
                grouping => grouping
                    .OrderByDescending(item => item.Settlement.IsPrimary)
                    .ThenByDescending(item => item.Settlement.StoredFood)
                    .Select(item => item.Settlement)
                    .First(),
                StringComparer.Ordinal);
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
                GroupKnowledgeContext.Create(world, group, discoveryCatalog, floraCatalog, faunaCatalog),
                monthlyFoodNeed,
                ResolvePrimaryAction(group.SubsistenceMode),
                ResolveFallbackAction(group.SubsistenceMode),
                "PreferenceDriven");
        }

        foreach (var regionGroups in groupsByRegionId)
        {
            if (!mutableRegions.TryGetValue(regionGroups.Key, out var regionState))
            {
                continue;
            }

            foreach (var state in regionGroups.Value.Select(group => survivalStates[group.Id]))
            {
                state.ResolvePlan(regionState.Region, regionState.FloraPopulations, regionState.FaunaPopulations, floraCatalog, faunaCatalog);
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
            var storedFoodUsabilityMultiplier = ResolveStoredFoodUsabilityMultiplier(group);
            var usableStoredFoodAvailable = (int)MathF.Round(state.StoredFoodBefore * storedFoodUsabilityMultiplier, MidpointRounding.AwayFromZero);
            var consumedFromActions = Math.Min(state.MonthlyFoodNeed, totalFoodAcquired);
            var remainingNeedAfterActions = Math.Max(0, state.MonthlyFoodNeed - consumedFromActions);
            var consumedFromStoredFood = Math.Min(remainingNeedAfterActions, usableStoredFoodAvailable);
            var remainingNeedAfterStoredFood = Math.Max(0, remainingNeedAfterActions - consumedFromStoredFood);
            var settlementFoodUsed = ConsumeSettlementReserve(settlementLookup, group, remainingNeedAfterStoredFood);
            // "Usable food" is the amount this actor can actually convert into survival this month,
            // after subsistence access, Knowledge gating, storage access, and local reserve access are resolved.
            var usableFoodConsumed = consumedFromActions + consumedFromStoredFood + settlementFoodUsed;
            var usableFoodRatio = state.MonthlyFoodNeed <= 0
                ? 1.0f
                : Math.Clamp(usableFoodConsumed / (float)state.MonthlyFoodNeed, 0.0f, 1.0f);
            var shortage = Math.Max(0, state.MonthlyFoodNeed - usableFoodConsumed);
            var rawStoredFoodConsumed = ResolveRawFoodConsumed(consumedFromStoredFood, storedFoodUsabilityMultiplier);
            var storedFoodAfter = Math.Max(0, state.StoredFoodBefore - rawStoredFoodConsumed);
            var hardshipPressure = Math.Max(group.Pressures.Food.EffectiveValue, group.Pressures.Water.EffectiveValue);
            var hardshipSeverity = ResolveHardshipSeverity(group);
            var hungerPressure = FoodStressModel.ResolveHungerPressure(group.HungerPressure, usableFoodRatio, GroupSurvivalConstants.HungerRiseRate, GroupSurvivalConstants.HungerDecayRate);
            var shortageMonths = FoodStressModel.ResolveShortageMonths(group.ShortageMonths, usableFoodRatio);
            var foodStressState = FoodStressModel.ResolveState(usableFoodRatio, hungerPressure, shortageMonths);
            var starvationLoss = CalculateStarvationLoss(group.Population, usableFoodRatio, shortageMonths, hungerPressure, hardshipPressure, foodStressState);
            var finalPopulation = Math.Max(0, group.Population - starvationLoss);

            changes.Add(new GroupSurvivalChange
            {
                GroupId = group.Id,
                GroupName = group.Name,
                CurrentRegionId = group.CurrentRegionId,
                CurrentRegionName = regionState.Region.Name,
                SubsistenceMode = state.Group.SubsistenceMode.ToString(),
                SubsistencePreference = group.SubsistencePreference.ToString(),
                ExtractionPlan = state.PlanKind,
                StartingPopulation = group.Population,
                MonthlyFoodNeed = state.MonthlyFoodNeed,
                KnownGatheringSupport = (int)MathF.Round((float)state.KnownGatheringSupport, MidpointRounding.AwayFromZero),
                KnownHuntingSupport = (int)MathF.Round((float)state.KnownHuntingSupport, MidpointRounding.AwayFromZero),
                PrimaryAction = state.PrimaryAction,
                PrimaryFoodGained = state.PrimaryAcquisition.FoodGained,
                PrimarySummary = state.PrimaryAcquisition.BuildSummary(),
                PrimaryConsumedSourceUnits = state.PrimaryAcquisition.ConsumedSourceUnits,
                FallbackAction = state.FallbackAction,
                FallbackFoodGained = state.FallbackAcquisition.FoodGained,
                FallbackSummary = state.FallbackAcquisition.BuildSummary(),
                FallbackConsumedSourceUnits = state.FallbackAcquisition.ConsumedSourceUnits,
                TotalFoodAcquired = totalFoodAcquired,
                StoredFoodBefore = state.StoredFoodBefore,
                StoredFoodAfter = storedFoodAfter,
                SettlementFoodUsed = settlementFoodUsed,
                UsableFoodConsumed = usableFoodConsumed,
                FoodPressureEffective = group.Pressures.Food.EffectiveValue,
                WaterPressureEffective = group.Pressures.Water.EffectiveValue,
                HardshipSeverityLabel = hardshipSeverity,
                HungerPressure = MathF.Round(hungerPressure, 2),
                ShortageMonths = shortageMonths,
                FoodStressState = foodStressState.ToString(),
                Shortage = shortage,
                StarvationLoss = starvationLoss,
                FinalPopulation = finalPopulation,
                Outcome = ResolveOutcome(group.Population, finalPopulation),
                SurvivalReason = ResolveSurvivalReason(state.MonthlyFoodNeed, totalFoodAcquired, usableFoodConsumed, state.StoredFoodBefore, settlementFoodUsed, shortage, starvationLoss, hardshipPressure, hardshipSeverity, foodStressState)
            });

            if (finalPopulation > 0)
            {
                var updatedGroup = CloneGroup(group);
                updatedGroup.StoredFood = storedFoodAfter;
                updatedGroup.Population = finalPopulation;
                updatedGroup.HungerPressure = hungerPressure;
                updatedGroup.ShortageMonths = shortageMonths;
                updatedGroup.FoodStressState = foodStressState;
                updatedGroups.Add(updatedGroup);
            }
        }

        var updatedRegions = world.Regions
            .Select(region => mutableRegions[region.Id].ToRegion())
            .ToArray();

        return new GroupSurvivalResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, updatedRegions, updatedGroups, world.Chronicle, updatedPolities, world.FocalPolityId),
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
                    species => SubsistenceSupportModel.ResolveFloraFoodPerPopulation(regionState.Region, species),
                    SubsistenceSupportModel.ResolveGatheringMultiplier,
                    (state, sourceId) => state.KnowledgeContext.ResolveFloraUseMultiplier(regionState.Region, sourceId));
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
                    SubsistenceSupportModel.ResolveHuntingMultiplier,
                    (state, sourceId) => state.KnowledgeContext.ResolveFaunaUseMultiplier(regionState.Region, sourceId));
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
        Func<PopulationGroup, Region, float> resolveMultiplier,
        Func<GroupSurvivalState, string, float> resolveKnowledgeMultiplier)
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
                        (float)participants.Average(state =>
                        {
                            var knowledgeMultiplier = resolveKnowledgeMultiplier(state, entry.Key);
                            return knowledgeMultiplier <= 0.0f
                                ? 0.0f
                                : SubsistenceSupportModel.ResolveFoodPerUnit(resolveBaseFoodValue(definition), resolveMultiplier(state.Group, region) * knowledgeMultiplier);
                        }));
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
                    var knowledgeMultiplier = resolveKnowledgeMultiplier(state, source.Id);
                    if (knowledgeMultiplier <= 0.0f)
                    {
                        return null;
                    }

                    var foodPerUnit = SubsistenceSupportModel.ResolveFoodPerUnit(source.BaseFoodValue, resolveMultiplier(state.Group, region) * knowledgeMultiplier);
                    var neededUnits = (int)MathF.Ceiling(state.RemainingNeed / (float)foodPerUnit);
                    return new GroupDemand(state, foodPerUnit, Math.Max(0, neededUnits));
                })
                .Where(demand => demand is not null)
                .Select(demand => demand!)
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

    private static int CalculateStarvationLoss(int startingPopulation, float usableFoodRatio, int shortageMonths, float hungerPressure, int hardshipPressure, FoodStressState foodStressState)
    {
        if (startingPopulation <= 0 || foodStressState == FoodStressState.FedStable)
        {
            return 0;
        }

        var shortageRatio = 1.0f - usableFoodRatio;
        var starvationSeverity = ResolveStarvationSeverity(hardshipPressure);
        if (foodStressState == FoodStressState.SevereShortage)
        {
            starvationSeverity = Math.Max(starvationSeverity, GroupSurvivalConstants.SevereShortageLossSeverity);
        }

        if (foodStressState == FoodStressState.Starvation)
        {
            starvationSeverity += GroupSurvivalConstants.StarvationLossSeverity;
            starvationSeverity += Math.Max(0, shortageMonths - 3) * GroupSurvivalConstants.ShortageMonthStarvationRamp;
            starvationSeverity += hungerPressure * GroupSurvivalConstants.HungerPressureStarvationRamp;
            if (usableFoodRatio <= 0.05f)
            {
                starvationSeverity += GroupSurvivalConstants.NoUsableFoodStarvationBonus;
            }
        }

        return Math.Min(
            startingPopulation,
            (int)MathF.Ceiling(startingPopulation * shortageRatio * starvationSeverity));
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

    private static int ConsumeSettlementReserve(
        IReadOnlyDictionary<string, Settlement> settlementLookup,
        PopulationGroup group,
        int remainingNeed)
    {
        if (remainingNeed <= 0)
        {
            return 0;
        }

        var key = BuildSettlementLookupKey(group.PolityId, group.CurrentRegionId);
        if (!settlementLookup.TryGetValue(key, out var settlement) || !settlement.IsActive || settlement.StoredFood <= 0)
        {
            return 0;
        }

        var usabilityMultiplier = ResolveSettlementReserveUsabilityMultiplier(group);
        var usableReserveAvailable = (int)MathF.Round(settlement.StoredFood * usabilityMultiplier, MidpointRounding.AwayFromZero);
        var usableFoodUsed = Math.Min(usableReserveAvailable, remainingNeed);
        var rawFoodConsumed = ResolveRawFoodConsumed(usableFoodUsed, usabilityMultiplier);
        settlement.StoredFood = Math.Max(0, settlement.StoredFood - rawFoodConsumed);
        return usableFoodUsed;
    }

    private static string ResolveSurvivalReason(int monthlyFoodNeed, int acquiredFood, int usableFoodConsumed, int storedFoodBefore, int settlementFoodUsed, int shortage, int starvationLoss, int hardshipPressure, string hardshipSeverity, FoodStressState foodStressState)
    {
        if (starvationLoss > 0)
        {
            return $"Group entered {DescribeFoodStressState(foodStressState)} after only {usableFoodConsumed} usable food against need {monthlyFoodNeed}; losses followed under {hardshipSeverity.ToLowerInvariant()} hardship pressure (effective {hardshipPressure}).";
        }

        if (settlementFoodUsed > 0)
        {
            return $"Group used {settlementFoodUsed} usable food from a local settlement reserve to help cover monthly need.";
        }

        if (storedFoodBefore > 0 && acquiredFood < monthlyFoodNeed)
        {
            return $"Group used StoredFood as usable food to help cover monthly need while hardship pressure stayed {hardshipSeverity.ToLowerInvariant()} (effective {hardshipPressure}).";
        }

        if (shortage > 0)
        {
            return $"Group absorbed a usable-food shortfall of {shortage} and ended the month in {DescribeFoodStressState(foodStressState)}, but losses stayed limited because effective hardship pressure was {hardshipPressure} ({hardshipSeverity.ToLowerInvariant()}).";
        }

        return $"Group secured {usableFoodConsumed} usable food and stayed fed for the month.";
    }

    private static float ResolveStarvationSeverity(int hardshipPressure)
    {
        var severity = GroupSurvivalConstants.StarvationLossSeverity;
        if (hardshipPressure >= GroupSurvivalConstants.CriticalHardshipPressureThreshold)
        {
            severity += GroupSurvivalConstants.CriticalHardshipStarvationBonus;
        }
        else if (hardshipPressure >= GroupSurvivalConstants.SevereHardshipPressureThreshold)
        {
            severity += GroupSurvivalConstants.SevereHardshipStarvationBonus;
        }
        else if (hardshipPressure >= GroupSurvivalConstants.ModerateHardshipPressureThreshold)
        {
            severity += GroupSurvivalConstants.ModerateHardshipStarvationBonus;
        }

        return Math.Min(GroupSurvivalConstants.MaxStarvationLossSeverity, severity);
    }

    private static string ResolveHardshipSeverity(PopulationGroup group)
    {
        var display = Math.Max(group.Pressures.Food.DisplayValue, group.Pressures.Water.DisplayValue);
        return PressureMath.ComputeSeverityLabel(display);
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
            HungerPressure = group.HungerPressure,
            ShortageMonths = group.ShortageMonths,
            FoodStressState = group.FoodStressState,
            SubsistencePreference = group.SubsistencePreference,
            SubsistenceMode = group.SubsistenceMode,
            LastRegionId = group.LastRegionId,
            MonthsSinceLastMove = group.MonthsSinceLastMove,
            Pressures = group.Pressures.Clone(),
            KnownRegionIds = new HashSet<string>(group.KnownRegionIds, StringComparer.Ordinal),
            KnownDiscoveryIds = new HashSet<string>(group.KnownDiscoveryIds, StringComparer.Ordinal),
            DiscoveryEvidence = group.DiscoveryEvidence.Clone(),
            LearnedAdvancementIds = new HashSet<string>(group.LearnedAdvancementIds, StringComparer.Ordinal),
            AdvancementEvidence = group.AdvancementEvidence.Clone()
        };
    }

    private static float ResolveStoredFoodUsabilityMultiplier(PopulationGroup group)
    {
        if (!group.LearnedAdvancementIds.Contains(AdvancementCatalog.FoodStorageId))
        {
            return 1.0f;
        }

        return AdvancementConstants.FoodStorageStoredFoodMultiplier;
    }

    private static float ResolveSettlementReserveUsabilityMultiplier(PopulationGroup group)
    {
        if (!group.LearnedAdvancementIds.Contains(AdvancementCatalog.FoodStorageId))
        {
            return 1.0f;
        }

        return AdvancementConstants.FoodStorageSettlementReserveMultiplier;
    }

    private static int ResolveRawFoodConsumed(int usableFoodConsumed, float usabilityMultiplier)
    {
        if (usableFoodConsumed <= 0)
        {
            return 0;
        }

        return Math.Max(1, (int)MathF.Ceiling(usableFoodConsumed / Math.Max(0.01f, usabilityMultiplier)));
    }

    private static string BuildSettlementLookupKey(string polityId, string regionId)
    {
        return $"{polityId}|{regionId}";
    }

    private static string DescribeFoodStressState(FoodStressState foodStressState)
    {
        return foodStressState switch
        {
            FoodStressState.FedStable => "fed stability",
            FoodStressState.HungerPressure => "hunger pressure",
            FoodStressState.SevereShortage => "severe shortage",
            FoodStressState.Starvation => "starvation",
            _ => "food stress"
        };
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
                new RegionEcosystem(
                    Region.Ecosystem.ProtoLifeSubstrate,
                    FloraPopulations,
                    FaunaPopulations,
                    Region.Ecosystem.FloraProfiles.ToDictionary(entry => entry.Key, entry => entry.Value.Clone(), StringComparer.Ordinal),
                    Region.Ecosystem.FaunaProfiles.ToDictionary(entry => entry.Key, entry => entry.Value.Clone(), StringComparer.Ordinal),
                    Region.Ecosystem.FossilRecords.ToArray(),
                    Region.Ecosystem.BiologicalHistoryRecords.ToArray()),
                Region.MaterialProfile.Clone());
        }
    }

    private sealed class GroupSurvivalState
    {
        public GroupSurvivalState(PopulationGroup group, GroupKnowledgeContext knowledgeContext, int monthlyFoodNeed, string primaryAction, string fallbackAction, string planKind)
        {
            Group = group;
            KnowledgeContext = knowledgeContext;
            MonthlyFoodNeed = monthlyFoodNeed;
            RemainingNeed = monthlyFoodNeed;
            PrimaryAction = primaryAction;
            FallbackAction = fallbackAction;
            PlanKind = planKind;
            StoredFoodBefore = group.StoredFood;
            PrimaryAcquisition = new AcquisitionState(primaryAction);
            FallbackAcquisition = new AcquisitionState(fallbackAction);
        }

        public PopulationGroup Group { get; }

        public GroupKnowledgeContext KnowledgeContext { get; }

        public int MonthlyFoodNeed { get; }

        public int RemainingNeed { get; private set; }

        public string PrimaryAction { get; private set; }

        public string FallbackAction { get; private set; }

        public string PlanKind { get; private set; }

        public int StoredFoodBefore { get; }

        public double KnownGatheringSupport { get; private set; }

        public double KnownHuntingSupport { get; private set; }

        public AcquisitionState PrimaryAcquisition { get; }

        public AcquisitionState FallbackAcquisition { get; }

        public void ResolvePlan(
            Region region,
            IReadOnlyDictionary<string, int> floraPopulations,
            IReadOnlyDictionary<string, int> faunaPopulations,
            FloraSpeciesCatalog floraCatalog,
            FaunaSpeciesCatalog faunaCatalog)
        {
            // "Known support" is intentionally strict: only Knowledge-backed species use counts here.
            // Discovery can improve awareness and future progress, but it must not grant deliberate extraction.
            KnownGatheringSupport = ResolveKnownGatheringSupport(region, floraPopulations, floraCatalog);
            KnownHuntingSupport = ResolveKnownHuntingSupport(region, faunaPopulations, faunaCatalog);

            var plan = NormalizeExtractionPlan(ResolveExtractionPlan(Group, KnownGatheringSupport, KnownHuntingSupport));
            PrimaryAction = plan.PrimaryAction;
            FallbackAction = plan.FallbackAction;
            PlanKind = plan.PlanKind;
            Group.SubsistenceMode = plan.RealizedMode;
        }

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

        private double ResolveKnownGatheringSupport(
            Region region,
            IReadOnlyDictionary<string, int> floraPopulations,
            FloraSpeciesCatalog floraCatalog)
        {
            return floraPopulations
                .Where(entry => entry.Value > 0)
                .Sum(entry =>
                {
                    var species = floraCatalog.GetById(entry.Key);
                    var knowledgeMultiplier = KnowledgeContext.ResolveFloraUseMultiplier(region, entry.Key);
                    return species is null || knowledgeMultiplier <= 0.0f
                        ? 0.0
                        : entry.Value * SubsistenceSupportModel.ResolveFoodPerUnit(
                            SubsistenceSupportModel.ResolveFloraFoodPerPopulation(region, species),
                            SubsistenceSupportModel.ResolveGatheringMultiplier(Group, region) * knowledgeMultiplier);
                });
        }

        private double ResolveKnownHuntingSupport(
            Region region,
            IReadOnlyDictionary<string, int> faunaPopulations,
            FaunaSpeciesCatalog faunaCatalog)
        {
            return faunaPopulations
                .Where(entry => entry.Value > 0)
                .Sum(entry =>
                {
                    var species = faunaCatalog.GetById(entry.Key);
                    var knowledgeMultiplier = KnowledgeContext.ResolveFaunaUseMultiplier(region, entry.Key);
                    return species is null || knowledgeMultiplier <= 0.0f
                        ? 0.0
                        : entry.Value * SubsistenceSupportModel.ResolveFoodPerUnit(
                            species.FoodYield,
                            SubsistenceSupportModel.ResolveHuntingMultiplier(Group, region) * knowledgeMultiplier);
                });
        }

        private static ExtractionPlan ResolveExtractionPlan(PopulationGroup group, double knownGatheringSupport, double knownHuntingSupport)
        {
            var hasGather = knownGatheringSupport > 0.0;
            var hasHunt = knownHuntingSupport > 0.0;
            var isDesperate = group.FoodStressState is FoodStressState.SevereShortage or FoodStressState.Starvation;

            if (isDesperate)
            {
                if (knownHuntingSupport > knownGatheringSupport && hasHunt)
                {
                    return new ExtractionPlan(SubsistenceMode.Hunter, "Hunt", hasGather ? "Gather" : "Hunt", "DesperationFallback");
                }

                if (hasGather)
                {
                    return new ExtractionPlan(hasHunt ? SubsistenceMode.Mixed : SubsistenceMode.Gatherer, "Gather", hasHunt ? "Hunt" : "Gather", "DesperationFallback");
                }
            }

            return group.SubsistencePreference switch
            {
                SubsistencePreference.HunterLeaning when hasHunt && (knownHuntingSupport >= knownGatheringSupport * 0.80 || !hasGather)
                    => new ExtractionPlan(SubsistenceMode.Hunter, "Hunt", hasGather ? "Gather" : "Hunt", "PreferenceDriven"),
                SubsistencePreference.ForagerLeaning when hasGather && (knownGatheringSupport >= knownHuntingSupport * 0.80 || !hasHunt)
                    => new ExtractionPlan(SubsistenceMode.Gatherer, "Gather", hasHunt ? "Hunt" : "Gather", "PreferenceDriven"),
                _ when hasGather && hasHunt && Math.Abs(knownGatheringSupport - knownHuntingSupport) / Math.Max(1.0, Math.Max(knownGatheringSupport, knownHuntingSupport)) <= 0.20
                    => new ExtractionPlan(SubsistenceMode.Mixed, "Gather", "Hunt", "BalancedKnownUse"),
                _ when hasHunt && (!hasGather || knownHuntingSupport > knownGatheringSupport * 1.20)
                    => new ExtractionPlan(SubsistenceMode.Hunter, "Hunt", hasGather ? "Gather" : "Hunt", "ResourceShift"),
                _ when hasGather
                    => new ExtractionPlan(hasHunt ? SubsistenceMode.Mixed : SubsistenceMode.Gatherer, "Gather", hasHunt ? "Hunt" : "Gather", hasHunt ? "ResourceShift" : "PreferenceDriven"),
                _ => new ExtractionPlan(
                    group.SubsistencePreference == SubsistencePreference.HunterLeaning ? SubsistenceMode.Hunter :
                    group.SubsistencePreference == SubsistencePreference.ForagerLeaning ? SubsistenceMode.Gatherer :
                    SubsistenceMode.Mixed,
                    group.SubsistencePreference == SubsistencePreference.HunterLeaning ? "Hunt" : "Gather",
                    group.SubsistencePreference == SubsistencePreference.HunterLeaning ? "Gather" : "Hunt",
                    "KnownSourceLimited")
            };
        }

        private static ExtractionPlan NormalizeExtractionPlan(ExtractionPlan plan)
        {
            return plan.PrimaryAction == plan.FallbackAction && plan.PlanKind != "KnownSourceLimited"
                ? plan with { PlanKind = "KnownSourceLimited" }
                : plan;
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

        public IReadOnlyDictionary<string, int> ConsumedSourceUnits => consumed;

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

    private sealed record ExtractionPlan(SubsistenceMode RealizedMode, string PrimaryAction, string FallbackAction, string PlanKind);
}
