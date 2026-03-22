using Species.Domain.Models;
using Species.Domain.Simulation;
using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;

namespace Species.Client.ViewModelFactories;

public static class PolityViewModelFactory
{
    private static readonly PolityConditionEvaluator PolityConditionEvaluator = new();

    private enum PolityPressureStateKind
    {
        Stable,
        Rising,
        Severe,
        Critical
    }

    public static PolityViewModel Build(World world, string focalPolityId, bool isSimulationRunning = false)
    {
        var focusPolity = PlayerFocus.Resolve(world, focalPolityId);
        var context = PlayerFocus.ResolveContext(world, focalPolityId);
        if (focusPolity is null || context?.LeadGroup is null)
        {
            return new PolityViewModel(
                "Unknown polity",
                FormatMonthYear(world.CurrentMonth, world.CurrentYear),
                isSimulationRunning,
                "Unknown",
                "0",
                "0",
                "0",
                "Stable",
                Array.Empty<PolityPressureItem>(),
                "No clear strength is visible yet.",
                "No major pressure is active right now.");
        }

        var snapshot = PolityConditionEvaluator.Evaluate(world, focusPolity);
        var activeSettlementCount = focusPolity.Settlements.Count(settlement => settlement.IsActive);
        var pressureItems = BuildPressureItems(context.Pressures);
        var pressureState = ResolvePressureState(pressureItems);

        return new PolityViewModel(
            focusPolity.Name,
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            isSimulationRunning,
            PolityPresentation.DescribeGovernmentForm(snapshot.GovernmentForm),
            context.TotalPopulation.ToString("N0"),
            activeSettlementCount.ToString("N0"),
            context.FoodAccounting.EndingTotalStores.ToString("N0"),
            pressureState.ToString(),
            BuildTopPressures(pressureItems, pressureState),
            BuildStrength(context, snapshot, activeSettlementCount, pressureState),
            BuildConcern(context, snapshot, activeSettlementCount, pressureItems, pressureState));
    }

    private static IReadOnlyList<PolityPressureItem> BuildPressureItems(PressureState pressures)
    {
        return new[]
        {
            new PolityPressureItem("Scarcity", pressures.Food.DisplayValue, pressures.Food.SeverityLabel),
            new PolityPressureItem("Water", pressures.Water.DisplayValue, pressures.Water.SeverityLabel),
            new PolityPressureItem("Threat", pressures.Threat.DisplayValue, pressures.Threat.SeverityLabel),
            new PolityPressureItem("Crowding", pressures.Overcrowding.DisplayValue, pressures.Overcrowding.SeverityLabel),
            new PolityPressureItem("Migration", pressures.Migration.DisplayValue, pressures.Migration.SeverityLabel)
        }
        .OrderByDescending(item => item.Value)
        .ThenBy(item => item.Label, StringComparer.Ordinal)
        .ToArray();
    }

    private static PolityPressureStateKind ResolvePressureState(IReadOnlyList<PolityPressureItem> pressures)
    {
        var severeCount = pressures.Count(pressure => pressure.Value >= 50);
        var moderateCount = pressures.Count(pressure => pressure.Value >= 25);

        if (pressures.Any(pressure => pressure.Value >= 75) || severeCount >= 2)
        {
            return PolityPressureStateKind.Critical;
        }

        if ((pressures.Any(pressure => pressure.Value >= 50) && pressures.All(pressure => pressure.Value < 75)) ||
            moderateCount >= 2)
        {
            return PolityPressureStateKind.Severe;
        }

        if (pressures.Any(pressure => pressure.Value >= 10))
        {
            return PolityPressureStateKind.Rising;
        }

        return PolityPressureStateKind.Stable;
    }

    private static IReadOnlyList<PolityPressureItem> BuildTopPressures(
        IReadOnlyList<PolityPressureItem> pressures,
        PolityPressureStateKind pressureState)
    {
        if (pressureState == PolityPressureStateKind.Stable)
        {
            return Array.Empty<PolityPressureItem>();
        }

        return pressures
            .Where(pressure => pressure.Value >= 10)
            .OrderByDescending(pressure => pressure.Value)
            .ThenBy(pressure => pressure.Label, StringComparer.Ordinal)
            .Take(3)
            .ToArray();
    }

    private static string BuildStrength(
        PolityContext context,
        PolityConditionSnapshot snapshot,
        int activeSettlementCount,
        PolityPressureStateKind pressureState)
    {
        var monthlyDemand = Math.Max(1, context.FoodAccounting.MonthlyDemand);
        var reserveMonths = context.FoodAccounting.EndingTotalStores / (float)monthlyDemand;

        if (activeSettlementCount >= 2 &&
            reserveMonths >= 1.0f &&
            context.FoodAccounting.NetFoodChange >= 0 &&
            pressureState == PolityPressureStateKind.Stable)
        {
            return "Dense settlement core with stable food reserves.";
        }

        if (reserveMonths >= 1.5f &&
            context.FoodAccounting.NetFoodChange >= 0 &&
            context.Pressures.Food.DisplayValue < 10)
        {
            return "Stable food reserves support continued growth.";
        }

        if (snapshot.MaterialSurvival.FoodCondition == PolityConditionSeverity.Stable &&
            snapshot.MaterialSurvival.WaterCondition == PolityConditionSeverity.Stable)
        {
            return "Food and water conditions remain stable across the homeland.";
        }

        if (snapshot.SpatialStability.HasStableBase && activeSettlementCount > 0)
        {
            return "A settled core still anchors the polity.";
        }

        if (context.FoodAccounting.EndingTotalStores > 0)
        {
            return "Current stores still provide a workable buffer.";
        }

        return "The polity still has a workable homeland base.";
    }

    private static string BuildConcern(
        PolityContext context,
        PolityConditionSnapshot snapshot,
        int activeSettlementCount,
        IReadOnlyList<PolityPressureItem> pressures,
        PolityPressureStateKind pressureState)
    {
        if (activeSettlementCount == 0)
        {
            return "No active settlements remain under polity control.";
        }

        if (snapshot.MaterialSurvival.FoodCondition >= PolityConditionSeverity.Critical ||
            context.FoodAccounting.UnresolvedDeficit > 0 ||
            context.FoodAccounting.EndingTotalStores <= 0)
        {
            return "Food stores are under severe strain and scarcity pressure is rising.";
        }

        if (context.Pressures.Food.DisplayValue >= 10 && context.FoodAccounting.NetFoodChange < 0)
        {
            return "Food stores are falling as scarcity pressure rises.";
        }

        if (context.Pressures.Migration.DisplayValue >= 10)
        {
            return activeSettlementCount >= 2
                ? "Migration pressure is rising across frontier settlements."
                : "Migration pressure is rising around the polity's current base.";
        }

        if (context.Pressures.Water.DisplayValue >= 10)
        {
            return "Water pressure is rising across the polity.";
        }

        if (context.Pressures.Threat.DisplayValue >= 10)
        {
            return "Threat pressure is rising around exposed borders.";
        }

        if (context.Pressures.Overcrowding.DisplayValue >= 10)
        {
            return "Crowding pressure is rising in settled regions.";
        }

        if (!snapshot.SpatialStability.HasStableBase)
        {
            return "The polity's settled core is becoming harder to hold.";
        }

        return pressureState == PolityPressureStateKind.Stable && pressures.Count > 0
            ? "No major pressure is active right now."
            : "Operational strain is building, even if no single pressure yet dominates.";
    }

    private static string FormatMonthYear(int month, int year)
    {
        var monthText = month switch
        {
            1 => "Jan",
            2 => "Feb",
            3 => "Mar",
            4 => "Apr",
            5 => "May",
            6 => "Jun",
            7 => "Jul",
            8 => "Aug",
            9 => "Sep",
            10 => "Oct",
            11 => "Nov",
            12 => "Dec",
            _ => "Jan"
        };

        return $"{monthText} {year:D3}";
    }
}
