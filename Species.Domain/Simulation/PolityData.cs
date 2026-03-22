using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Discovery;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed record PolityContext(
    Polity Polity,
    PopulationGroup? LeadGroup,
    IReadOnlyList<PopulationGroup> MemberGroups,
    int TotalPopulation,
    int TotalStoredFood,
    FoodAccountingSnapshot FoodAccounting,
    MaterialStockpile TotalMaterialStores,
    PressureState Pressures,
    IReadOnlySet<string> KnownRegionIds,
    IReadOnlySet<string> KnownDiscoveryIds,
    IReadOnlySet<string> LearnedAdvancementIds,
    Settlement? PrimarySettlement,
    string HomeRegionId,
    string CoreRegionId,
    string CurrentRegionId,
    string OriginRegionId,
    string SpeciesId,
    PolityAnchoringKind AnchoringKind,
    string ParentPolityId,
    GovernanceState Governance,
    ExternalPressureState ExternalPressure,
    PoliticalScaleState ScaleState,
    SocialMemoryState SocialMemory,
    SocialIdentityState SocialIdentity,
    MaterialProductionState MaterialProduction,
    int MaterialShortageMonths,
    int MaterialSurplusMonths);

public static class PolityData
{
    public static Polity? Resolve(World world, string polityId)
    {
        if (!string.IsNullOrWhiteSpace(polityId))
        {
            var polity = world.Polities.FirstOrDefault(item => string.Equals(item.Id, polityId, StringComparison.Ordinal));
            if (polity is not null)
            {
                return polity;
            }
        }

        if (!string.IsNullOrWhiteSpace(world.FocalPolityId))
        {
            var focal = world.Polities.FirstOrDefault(item => string.Equals(item.Id, world.FocalPolityId, StringComparison.Ordinal));
            if (focal is not null)
            {
                return focal;
            }
        }

        return world.Polities
            .Select(polity => BuildContext(world, polity))
            .Where(context => context is not null)
            .OrderByDescending(context => context!.TotalPopulation)
            .ThenBy(context => context!.Polity.Name, StringComparer.Ordinal)
            .Select(context => context!.Polity)
            .FirstOrDefault();
    }

    public static PolityContext? BuildContext(World world, Polity polity)
    {
        // The polity view must always reflect live group truth, even before any cached member-id list is rebuilt.
        var memberGroups = world.PopulationGroups
            .Where(group => string.Equals(group.PolityId, polity.Id, StringComparison.Ordinal))
            .OrderByDescending(group => group.Population)
            .ThenBy(group => group.Name, StringComparer.Ordinal)
            .ThenBy(group => group.Id, StringComparer.Ordinal)
            .ToArray();

        if (memberGroups.Length == 0 && polity.MemberGroupIds.Count > 0)
        {
            var groupsById = world.PopulationGroups.ToDictionary(group => group.Id, StringComparer.Ordinal);
            memberGroups = polity.MemberGroupIds
                .Select(groupsById.GetValueOrDefault)
                .Where(group => group is not null)
                .Cast<PopulationGroup>()
                .OrderByDescending(group => group.Population)
                .ThenBy(group => group.Name, StringComparer.Ordinal)
                .ThenBy(group => group.Id, StringComparer.Ordinal)
                .ToArray();
        }

        if (memberGroups.Length == 0)
        {
            return null;
        }

        var leadGroup = memberGroups[0];
        var totalPopulation = memberGroups.Sum(group => group.Population);
        var totalStoredFood = memberGroups.Sum(group => group.StoredFood);
        var foodAccounting = AggregateFoodAccounting(memberGroups, polity);
        var totalMaterialStores = polity.MaterialStores.Clone();
        foreach (var settlement in polity.Settlements.Where(settlement => settlement.IsActive))
        {
            totalMaterialStores.Timber += settlement.MaterialStores.Timber;
            totalMaterialStores.Stone += settlement.MaterialStores.Stone;
            totalMaterialStores.Fiber += settlement.MaterialStores.Fiber;
            totalMaterialStores.Clay += settlement.MaterialStores.Clay;
            totalMaterialStores.Hides += settlement.MaterialStores.Hides;
        }
        var pressureWeight = Math.Max(1, totalPopulation);

        var knownRegionIds = memberGroups
            .SelectMany(group => group.KnownRegionIds)
            .ToHashSet(StringComparer.Ordinal);
        var knownDiscoveryIds = memberGroups
            .SelectMany(group => group.KnownDiscoveryIds)
            .ToHashSet(StringComparer.Ordinal);
        var learnedAdvancementIds = memberGroups
            .SelectMany(group => group.LearnedAdvancementIds)
            .ToHashSet(StringComparer.Ordinal);

        return new PolityContext(
            polity,
            leadGroup,
            memberGroups,
            totalPopulation,
            totalStoredFood,
            foodAccounting,
            totalMaterialStores,
            new PressureState
            {
                Food = AggregateFoodPressure(memberGroups),
                Water = WeightedPressure(memberGroups, group => group.Pressures.Water, PressureDefinitions.Water),
                Threat = WeightedPressure(memberGroups, group => group.Pressures.Threat, PressureDefinitions.Threat),
                Overcrowding = WeightedPressure(memberGroups, group => group.Pressures.Overcrowding, PressureDefinitions.Overcrowding),
                Migration = WeightedPressure(memberGroups, group => group.Pressures.Migration, PressureDefinitions.Migration),
                Curiosity = PressureMath.CreateValue(PressureDefinitions.Curiosity, polity.Curiosity.StoredRawPressure)
            },
            knownRegionIds,
            knownDiscoveryIds,
            learnedAdvancementIds,
            polity.Settlements.FirstOrDefault(settlement => settlement.IsActive && settlement.IsPrimary),
            string.IsNullOrWhiteSpace(polity.HomeRegionId) ? leadGroup.OriginRegionId : polity.HomeRegionId,
            string.IsNullOrWhiteSpace(polity.CoreRegionId) ? leadGroup.OriginRegionId : polity.CoreRegionId,
            leadGroup.CurrentRegionId,
            leadGroup.OriginRegionId,
            leadGroup.SpeciesId,
            polity.AnchoringKind,
            polity.ParentPolityId,
            polity.Governance.Clone(),
            polity.ExternalPressure.Clone(),
            polity.ScaleState.Clone(),
            polity.SocialMemory.Clone(),
            polity.SocialIdentity.Clone(),
            polity.MaterialProduction.Clone(),
            polity.MaterialShortageMonths,
            polity.MaterialSurplusMonths);
    }

    private static int WeightedAverage(
        IReadOnlyList<PopulationGroup> groups,
        int totalPopulation,
        Func<PopulationGroup, int> selector)
    {
        if (groups.Count == 0)
        {
            return 0;
        }

        if (totalPopulation <= 0)
        {
            return (int)Math.Round(groups.Average(selector), MidpointRounding.AwayFromZero);
        }

        var weightedTotal = groups.Sum(group => selector(group) * Math.Max(1, group.Population));
        return (int)Math.Round(weightedTotal / (double)groups.Sum(group => Math.Max(1, group.Population)), MidpointRounding.AwayFromZero);
    }

    private static PressureValue WeightedPressure(
        IReadOnlyList<PopulationGroup> groups,
        Func<PopulationGroup, PressureValue> selector,
        PressureDefinition definition)
    {
        var rawAverage = WeightedAverage(groups, Math.Max(1, groups.Sum(group => group.Population)), group => selector(group).RawValue);
        return PressureMath.CreateValue(definition, rawAverage);
    }

    private static PressureValue AggregateFoodPressure(IReadOnlyList<PopulationGroup> groups)
    {
        if (groups.Count == 0)
        {
            return new PressureValue();
        }

        var totalPopulation = Math.Max(1, groups.Sum(group => group.Population));
        var rawAverage = WeightedAverage(groups, totalPopulation, group => group.Pressures.Food.RawValue);
        var totalStoredFood = groups.Sum(group => group.FoodAccounting.EndingTotalStores > 0 ? group.FoodAccounting.EndingTotalStores : group.StoredFood);
        var totalMonthlyFoodNeed = groups.Sum(group => group.FoodAccounting.MonthlyDemand > 0 ? group.FoodAccounting.MonthlyDemand : SubsistenceSupportModel.CalculateMonthlyFoodNeed(group.Population));
        var reserveCoverage = totalMonthlyFoodNeed <= 0
            ? PressureCalculationConstants.StoredFoodMonthsSafe
            : totalStoredFood / (float)totalMonthlyFoodNeed;
        var reserveSignal = (int)Math.Round(
            Math.Max(0.0f, 1.0f - MathF.Min(1.0f, reserveCoverage / PressureCalculationConstants.StoredFoodMonthsSafe)) * 140.0f,
            MidpointRounding.AwayFromZero);
        var hungerSignal = WeightedAverage(
            groups,
            totalPopulation,
            group => (int)Math.Round(group.HungerPressure * 160.0f, MidpointRounding.AwayFromZero));
        var shortageSignal = WeightedAverage(
            groups,
            totalPopulation,
            group => Math.Min(220, group.ShortageMonths * 36));
        var collapseSignal = WeightedAverage(groups, totalPopulation, group => ResolveFoodStressSignal(group.FoodStressState));
        var derivedRaw = (int)Math.Round(
            (reserveSignal * 0.20f) +
            (hungerSignal * 0.25f) +
            (shortageSignal * 0.20f) +
            (collapseSignal * 0.35f),
            MidpointRounding.AwayFromZero);

        // Food pressure is an active month-end strain signal. It should track finalized food stress,
        // shortage carryover, and reserve weakness without folding in non-food material fragility.
        return PressureMath.CreateValue(PressureDefinitions.Food, Math.Max(rawAverage, derivedRaw));
    }

    private static FoodAccountingSnapshot AggregateFoodAccounting(IReadOnlyList<PopulationGroup> groups, Polity polity)
    {
        var resolvedGroups = groups
            .Where(group =>
                group.FoodAccounting.StartingTotalStores > 0 ||
                group.FoodAccounting.EndingTotalStores > 0 ||
                group.FoodAccounting.MonthlyDemand > 0 ||
                group.FoodAccounting.FoodInflow > 0 ||
                group.FoodAccounting.FoodConsumption > 0 ||
                group.FoodAccounting.UnresolvedDeficit > 0 ||
                group.StoredFood > 0)
            .ToArray();

        // The polity screen should read current member-group month-end food truth first.
        // Cached polity food snapshots remain only as a fallback when no resolved group snapshot exists.
        if (resolvedGroups.Length > 0)
        {
            var totalPopulation = Math.Max(1, resolvedGroups.Sum(group => Math.Max(1, group.Population)));
            var weightedHunger = resolvedGroups.Sum(group => group.FoodAccounting.HungerPressure * Math.Max(1, group.Population));
            var weightedShortageMonths = resolvedGroups.Sum(group => group.FoodAccounting.ShortageMonths * Math.Max(1, group.Population));
            var worstFoodState = resolvedGroups.Max(group => group.FoodAccounting.FoodStressState);

            return new FoodAccountingSnapshot
            {
                StartingCarriedStores = resolvedGroups.Sum(group => group.FoodAccounting.StartingCarriedStores),
                StartingReserveStores = resolvedGroups.Sum(group => group.FoodAccounting.StartingReserveStores),
                FoodInflow = resolvedGroups.Sum(group => group.FoodAccounting.FoodInflow),
                FoodConsumption = resolvedGroups.Sum(group => group.FoodAccounting.FoodConsumption),
                FoodLosses = resolvedGroups.Sum(group => group.FoodAccounting.FoodLosses),
                NetFoodChange = resolvedGroups.Sum(group => group.FoodAccounting.NetFoodChange),
                EndingCarriedStores = resolvedGroups.Sum(group => group.FoodAccounting.EndingCarriedStores),
                EndingReserveStores = resolvedGroups.Sum(group => group.FoodAccounting.EndingReserveStores),
                MonthlyDemand = resolvedGroups.Sum(group => group.FoodAccounting.MonthlyDemand),
                UsableFoodConsumed = resolvedGroups.Sum(group => group.FoodAccounting.UsableFoodConsumed),
                UnresolvedDeficit = resolvedGroups.Sum(group => group.FoodAccounting.UnresolvedDeficit),
                HungerPressure = MathF.Round((float)(weightedHunger / totalPopulation), 2),
                ShortageMonths = (int)Math.Round((double)weightedShortageMonths / totalPopulation, MidpointRounding.AwayFromZero),
                FoodStressState = worstFoodState
            };
        }

        if (polity.FoodAccounting.EndingTotalStores > 0 ||
            polity.FoodAccounting.StartingTotalStores > 0 ||
            polity.FoodAccounting.MonthlyDemand > 0)
        {
            return polity.FoodAccounting.Clone();
        }

        var carriedStores = groups.Sum(group => group.StoredFood);
        var reserveStores = polity.Settlements.Where(settlement => settlement.IsActive).Sum(settlement => settlement.StoredFood);
        return FoodAccountingSnapshot.CreateInitial(carriedStores, reserveStores);
    }

    private static int ResolveFoodStressSignal(FoodStressState state)
    {
        return state switch
        {
            FoodStressState.HungerPressure => 70,
            FoodStressState.SevereShortage => 150,
            FoodStressState.Starvation => 240,
            _ => 0
        };
    }
}
