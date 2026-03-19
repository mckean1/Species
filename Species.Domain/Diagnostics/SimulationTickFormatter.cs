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

        return string.Join(Environment.NewLine, lines);
    }
}
