using Species.Domain.Models;
using Species.Domain.Simulation;

namespace Species.Domain.Validation;

public static class GroupSurvivalValidator
{
    public static IReadOnlyList<string> Validate(World previousWorld, SimulationTickResult tickResult)
    {
        var errors = new List<string>();

        foreach (var change in tickResult.GroupSurvivalChanges)
        {
            if (change.StartingPopulation < 0 || change.FinalPopulation < 0)
            {
                errors.Add($"Group survival produced a negative population for {change.GroupId}.");
            }

            if (change.StoredFoodBefore < 0 || change.StoredFoodAfter < 0)
            {
                errors.Add($"Group survival produced a negative StoredFood for {change.GroupId}.");
            }

            if (change.Shortage < 0 || change.StarvationLoss < 0)
            {
                errors.Add($"Group survival produced an invalid shortage or starvation value for {change.GroupId}.");
            }

            if (change.PrimaryAction is not ("Gather" or "Hunt") || change.FallbackAction is not ("Gather" or "Hunt"))
            {
                errors.Add($"Group {change.GroupId} has an invalid extraction action.");
            }

            if (change.PrimaryAction == change.FallbackAction && change.ExtractionPlan != "KnownSourceLimited")
            {
                errors.Add($"Group {change.GroupId} should not repeat the same primary and fallback action outside source-limited behavior.");
            }

            if (change.UsableFoodConsumed < 0)
            {
                errors.Add($"Group survival produced negative usable food for {change.GroupId}.");
            }

            if (change.SubsistenceMode is "Gatherer" or "Mixed" && change.PrimaryAction == "Gather" && change.FallbackAction != "Hunt" && change.ExtractionPlan != "KnownSourceLimited")
            {
                errors.Add($"Group {change.GroupId} does not follow gather-then-hunt fallback order.");
            }

            if (change.SubsistenceMode == "Hunter" && change.PrimaryAction == "Hunt" && change.FallbackAction != "Gather" && change.ExtractionPlan != "KnownSourceLimited")
            {
                errors.Add($"Group {change.GroupId} does not follow hunt-then-gather fallback order.");
            }
        }

        foreach (var region in tickResult.World.Regions)
        {
            foreach (var flora in region.Ecosystem.FloraPopulations)
            {
                if (flora.Value < 0)
                {
                    errors.Add($"Region {region.Id} has negative flora after group survival.");
                }
            }

            foreach (var fauna in region.Ecosystem.FaunaPopulations)
            {
                if (fauna.Value < 0)
                {
                    errors.Add($"Region {region.Id} has negative fauna after group survival.");
                }
            }
        }

        _ = previousWorld;
        return errors;
    }
}
