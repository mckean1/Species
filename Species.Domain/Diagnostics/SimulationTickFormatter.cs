using Species.Domain.Simulation;

namespace Species.Domain.Diagnostics;

public static class SimulationTickFormatter
{
    public static string Format(SimulationTickResult tickResult)
    {
        var lines = new List<string>
        {
            $"Tick Result: Year {tickResult.World.CurrentYear}, Month {tickResult.World.CurrentMonth}",
            "Flora Changes:"
        };

        foreach (var change in tickResult.FloraChanges)
        {
            lines.Add(
                $"{change.RegionId} | {change.RegionName} | {change.FloraSpeciesId} ({change.FloraSpeciesName}) | {change.PreviousPopulation} -> {change.NewPopulation} | Target={change.TargetPopulation} | Outcome={change.Outcome} | WaterSupported={change.WaterSupported} | CoreBiomeFit={change.CoreBiomeFit} | FertilityFit={change.FertilityFit:0.00} | Cause={change.PrimaryCause}");
        }

        lines.Add(string.Empty);
        lines.Add("Fauna Changes:");

        foreach (var change in tickResult.FaunaChanges)
        {
            lines.Add(
                $"{change.RegionId} | {change.RegionName} | {change.FaunaSpeciesId} ({change.FaunaSpeciesName}) | {change.PreviousPopulation} -> {change.NewPopulation} | Needed={change.FoodNeeded:0.00} | Consumed={change.FoodConsumed:0.00} | Fulfillment={change.FulfillmentRatio:0.00} | Habitat={change.HabitatSupport:0.00} | Outcome={change.Outcome} | FloraConsumed=[{change.ConsumedFloraSummary}] | FaunaConsumed=[{change.ConsumedFaunaSummary}] | Cause={change.PrimaryCause}");
        }

        lines.Add(string.Empty);
        lines.Add("Group Pressures:");

        foreach (var change in tickResult.GroupPressureChanges)
        {
            lines.Add(
                $"{change.GroupId} | {change.GroupName} | Region={change.CurrentRegionId} ({change.CurrentRegionName}) | Population={change.Population} | StoredFood={change.StoredFood} | Food={change.Pressures.FoodPressure} | Water={change.Pressures.WaterPressure} | Threat={change.Pressures.ThreatPressure} | Overcrowding={change.Pressures.OvercrowdingPressure} | Migration={change.Pressures.MigrationPressure} | FoodReason={change.FoodPressureReason} | ThreatReason={change.ThreatPressureReason} | MigrationReason={change.MigrationPressureReason}");
        }

        lines.Add(string.Empty);
        lines.Add("Group Survival:");

        foreach (var change in tickResult.GroupSurvivalChanges)
        {
            lines.Add(
                $"{change.GroupId} | {change.GroupName} | Region={change.CurrentRegionId} ({change.CurrentRegionName}) | Mode={change.SubsistenceMode} | Population={change.StartingPopulation}->{change.FinalPopulation} | Need={change.MonthlyFoodNeed} | Primary={change.PrimaryAction}:{change.PrimaryFoodGained} | Fallback={change.FallbackAction}:{change.FallbackFoodGained} | Acquired={change.TotalFoodAcquired} | StoredFood={change.StoredFoodBefore}->{change.StoredFoodAfter} | Shortage={change.Shortage} | StarvationLoss={change.StarvationLoss} | Outcome={change.Outcome} | PrimarySummary={change.PrimarySummary} | FallbackSummary={change.FallbackSummary} | Reason={change.SurvivalReason}");
        }

        lines.Add(string.Empty);
        lines.Add("Migration:");

        foreach (var change in tickResult.MigrationChanges)
        {
            lines.Add(
                $"{change.GroupId} | {change.GroupName} | Region={change.CurrentRegionId} ({change.CurrentRegionName}) | MigrationPressure={change.MigrationPressure} | StoredFood={change.StoredFood} | Considered={change.ConsideredMigration} | CurrentScore={change.CurrentRegionScore:0.0} | Neighbors=[{change.NeighborScoresSummary}] | Winner={change.WinningRegionId} ({change.WinningRegionName})={change.WinningRegionScore:0.0} | Moved={change.Moved} | NewRegion={change.NewRegionId} ({change.NewRegionName}) | LastRegionId={change.LastRegionId} | MonthsSinceLastMove={change.MonthsSinceLastMove} | Reason={change.DecisionReason}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
