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

        return string.Join(Environment.NewLine, lines);
    }
}
