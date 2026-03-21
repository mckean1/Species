using Species.Domain.Models;
using Species.Domain.Simulation;
using Species.Domain.Enums;
using Species.Domain.Constants;

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
            if (change.Pressures.Food.DisplayValue is < 0 or > 100 ||
                change.Pressures.Water.DisplayValue is < 0 or > 100 ||
                change.Pressures.Threat.DisplayValue is < 0 or > 100 ||
                change.Pressures.Overcrowding.DisplayValue is < 0 or > 100 ||
                change.Pressures.Migration.DisplayValue is < 0 or > 100)
            {
                errors.Add($"Pressure recalculation produced an out-of-range pressure for {change.GroupId}.");
            }

            var synthesizedMigration = (int)Math.Round(
                Math.Clamp(
                    ((change.Food.MonthlyContribution * PressureCalculationConstants.MigrationFoodWeight) +
                     (change.Water.MonthlyContribution * PressureCalculationConstants.MigrationWaterWeight) +
                     (change.Threat.MonthlyContribution * PressureCalculationConstants.MigrationThreatWeight) +
                     (change.Overcrowding.MonthlyContribution * PressureCalculationConstants.MigrationOvercrowdingWeight)) -
                    PressureCalculationConstants.MigrationMonthlyRelief,
                    0.0,
                    PressureCalculationConstants.PressureScaleMaximum),
                MidpointRounding.AwayFromZero);

            if (Math.Abs(change.Migration.MonthlyContribution - synthesizedMigration) > 1)
            {
                errors.Add($"Migration pressure contribution for {change.GroupId} is inconsistent with component contributions.");
            }
        }

        foreach (var change in tickResult.SettlementChanges)
        {
            if (string.IsNullOrWhiteSpace(change.PolityId) || string.IsNullOrWhiteSpace(change.Message))
            {
                errors.Add("Settlement update reported an incomplete polity anchoring change.");
            }
        }

        foreach (var change in tickResult.MaterialEconomyChanges)
        {
            if (string.IsNullOrWhiteSpace(change.PolityId) || string.IsNullOrWhiteSpace(change.Message))
            {
                errors.Add("Material economy update reported an incomplete change.");
            }

            if (change.Extracted.Timber < 0 ||
                change.Extracted.Stone < 0 ||
                change.Extracted.Fiber < 0 ||
                change.Extracted.Clay < 0 ||
                change.Extracted.Hides < 0)
            {
                errors.Add($"Material economy change for {change.PolityId} reported negative extraction.");
            }
        }

        foreach (var change in tickResult.BiologicalHistoryChanges)
        {
            if (string.IsNullOrWhiteSpace(change.RegionId) ||
                string.IsNullOrWhiteSpace(change.SpeciesId) ||
                string.IsNullOrWhiteSpace(change.Message))
            {
                errors.Add("Biological evolution update reported an incomplete change.");
            }
        }

        foreach (var change in tickResult.SocialIdentityChanges)
        {
            if (string.IsNullOrWhiteSpace(change.PolityId) ||
                string.IsNullOrWhiteSpace(change.TraditionId) ||
                string.IsNullOrWhiteSpace(change.Message))
            {
                errors.Add("Social identity update reported an incomplete change.");
            }
        }

        foreach (var change in tickResult.InterPolityChanges)
        {
            if (string.IsNullOrWhiteSpace(change.PrimaryPolityId) ||
                string.IsNullOrWhiteSpace(change.OtherPolityId) ||
                string.IsNullOrWhiteSpace(change.Kind) ||
                string.IsNullOrWhiteSpace(change.Message))
            {
                errors.Add("Inter-polity interaction update reported an incomplete change.");
            }

            if (string.Equals(change.PrimaryPolityId, change.OtherPolityId, StringComparison.Ordinal))
            {
                errors.Add("Inter-polity interaction update reported a self-targeting change.");
            }
        }

        foreach (var change in tickResult.PoliticalScaleChanges)
        {
            if (string.IsNullOrWhiteSpace(change.PolityId) ||
                string.IsNullOrWhiteSpace(change.Kind) ||
                string.IsNullOrWhiteSpace(change.Message))
            {
                errors.Add("Political scaling update reported an incomplete change.");
            }
        }

        if (tickResult.LawProposalChanges.Any(change => change.Status is not (LawProposalStatus.Passed or LawProposalStatus.Vetoed)))
        {
            errors.Add("Simulation tick reported a law proposal change that was not passed or vetoed.");
        }

        var recordedLawEntries = tickResult.RecordedChronicleEntries.Count(entry => entry.Category == ChronicleEventCategory.Law);
        if (recordedLawEntries != tickResult.LawProposalChanges.Count)
        {
            errors.Add("Recorded Chronicle law entries do not match resolved law proposal changes for the tick.");
        }

        var recordedExternalEntries = tickResult.RecordedChronicleEntries.Count(entry => entry.Category == ChronicleEventCategory.External);
        if (recordedExternalEntries != tickResult.InterPolityChanges.Count)
        {
            errors.Add("Recorded Chronicle external entries do not match inter-polity changes for the tick.");
        }

        var recordedPoliticalEntries = tickResult.RecordedChronicleEntries.Count(entry => entry.Category == ChronicleEventCategory.Political);
        if (recordedPoliticalEntries != tickResult.PoliticalScaleChanges.Count)
        {
            errors.Add("Recorded Chronicle political entries do not match political scaling changes for the tick.");
        }

        return errors;
    }
}
