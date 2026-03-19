using Species.Domain.Models;
using Species.Domain.Simulation;

namespace Species.Domain.Validation;

public static class SimulationTickValidator
{
    public static IReadOnlyList<string> Validate(World previousWorld, SimulationTickResult tickResult)
    {
        var errors = new List<string>();
        var expectedMonth = previousWorld.CurrentMonth == 12 ? 1 : previousWorld.CurrentMonth + 1;
        var expectedYear = previousWorld.CurrentMonth == 12 ? previousWorld.CurrentYear + 1 : previousWorld.CurrentYear;

        if (tickResult.World.CurrentMonth != expectedMonth || tickResult.World.CurrentYear != expectedYear)
        {
            errors.Add("Simulation tick did not advance the month correctly.");
        }

        foreach (var change in tickResult.FloraChanges)
        {
            if (change.NewPopulation < 0)
            {
                errors.Add($"Flora simulation produced a negative population for {change.FloraSpeciesId} in {change.RegionId}.");
            }

            if (!change.WaterSupported && change.NewPopulation > change.PreviousPopulation)
            {
                errors.Add($"Unsupported-water flora {change.FloraSpeciesId} grew in {change.RegionId}.");
            }
        }

        foreach (var change in tickResult.FaunaChanges)
        {
            if (change.NewPopulation < 0)
            {
                errors.Add($"Fauna simulation produced a negative population for {change.FaunaSpeciesId} in {change.RegionId}.");
            }

            if (change.ConsumedFaunaSummary.Contains($"{change.FaunaSpeciesId}:", StringComparison.Ordinal))
            {
                errors.Add($"Fauna simulation allowed cannibalism for {change.FaunaSpeciesId} in {change.RegionId}.");
            }
        }

        foreach (var change in tickResult.GroupPressureChanges)
        {
            if (change.Pressures.FoodPressure is < 0 or > 100 ||
                change.Pressures.WaterPressure is < 0 or > 100 ||
                change.Pressures.ThreatPressure is < 0 or > 100 ||
                change.Pressures.OvercrowdingPressure is < 0 or > 100 ||
                change.Pressures.MigrationPressure is < 0 or > 100)
            {
                errors.Add($"Pressure recalculation produced an out-of-range pressure for {change.GroupId}.");
            }

            var synthesizedMigration = (int)Math.Round(
                (change.Pressures.FoodPressure * 0.35) +
                (change.Pressures.WaterPressure * 0.15) +
                (change.Pressures.ThreatPressure * 0.20) +
                (change.Pressures.OvercrowdingPressure * 0.30),
                MidpointRounding.AwayFromZero);

            if (Math.Abs(change.Pressures.MigrationPressure - synthesizedMigration) > 1)
            {
                errors.Add($"MigrationPressure for {change.GroupId} is inconsistent with component pressures.");
            }
        }

        return errors;
    }
}
