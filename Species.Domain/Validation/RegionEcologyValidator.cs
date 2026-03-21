using Species.Domain.Catalogs;
using Species.Domain.Models;

namespace Species.Domain.Validation;

public static class RegionEcologyValidator
{
    public static IReadOnlyList<string> Validate(
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var errors = new List<string>();
        var knownFloraIds = floraCatalog.Definitions.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);
        var knownFaunaIds = faunaCatalog.Definitions.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var region in world.Regions)
        {
            if (region.Ecosystem is null)
            {
                errors.Add($"Region {region.Id} is missing a RegionEcosystem.");
                continue;
            }

            ValidateSubstrate(region.Id, region.Ecosystem.ProtoLifeSubstrate, errors);

            foreach (var floraPopulation in region.Ecosystem.FloraPopulations)
            {
                if (!knownFloraIds.Contains(floraPopulation.Key))
                {
                    errors.Add($"Region {region.Id} references unknown flora species {floraPopulation.Key}.");
                }

                if (floraPopulation.Value < 0)
                {
                    errors.Add($"Region {region.Id} has negative flora population for {floraPopulation.Key}.");
                }
            }

            foreach (var floraProfile in region.Ecosystem.FloraProfiles)
            {
                if (!knownFloraIds.Contains(floraProfile.Key))
                {
                    errors.Add($"Region {region.Id} references unknown flora biological profile {floraProfile.Key}.");
                }

                ValidateProfile(region.Id, floraProfile.Value, errors);
            }

            foreach (var faunaPopulation in region.Ecosystem.FaunaPopulations)
            {
                if (!knownFaunaIds.Contains(faunaPopulation.Key))
                {
                    errors.Add($"Region {region.Id} references unknown fauna species {faunaPopulation.Key}.");
                }

                if (faunaPopulation.Value < 0)
                {
                    errors.Add($"Region {region.Id} has negative fauna population for {faunaPopulation.Key}.");
                }
            }

            foreach (var faunaProfile in region.Ecosystem.FaunaProfiles)
            {
                if (!knownFaunaIds.Contains(faunaProfile.Key))
                {
                    errors.Add($"Region {region.Id} references unknown fauna biological profile {faunaProfile.Key}.");
                }

                ValidateProfile(region.Id, faunaProfile.Value, errors);
            }

            foreach (var fossil in region.Ecosystem.FossilRecords)
            {
                if (!string.Equals(fossil.RegionId, region.Id, StringComparison.Ordinal))
                {
                    errors.Add($"Fossil record {fossil.Id} is attached to the wrong region.");
                }

                if (string.IsNullOrWhiteSpace(fossil.SpeciesId) || string.IsNullOrWhiteSpace(fossil.FormName))
                {
                    errors.Add($"Region {region.Id} has an incomplete fossil record.");
                }
            }

            foreach (var record in region.Ecosystem.BiologicalHistoryRecords)
            {
                if (!string.Equals(record.RegionId, region.Id, StringComparison.Ordinal))
                {
                    errors.Add($"Biological history record {record.Id} is attached to the wrong region.");
                }
            }
        }

        return errors;
    }

    private static void ValidateProfile(string regionId, RegionalBiologicalProfile profile, ICollection<string> errors)
    {
        if (!string.Equals(profile.RegionId, regionId, StringComparison.Ordinal))
        {
            errors.Add($"Regional biological profile {profile.SpeciesId} is attached to the wrong region.");
        }

        if (profile.DivergenceScore is < 0 or > 100 ||
            profile.SpeciationProgress is < 0 or > 100 ||
            profile.ViabilityScore is < 0 or > 100)
        {
            errors.Add($"Regional biological profile {profile.SpeciesId} has invalid divergence or viability values.");
        }

        if (profile.ContinuityMonths < 0 || profile.SuccessfulMonths < 0 || profile.LastPopulation < 0)
        {
            errors.Add($"Regional biological profile {profile.SpeciesId} has negative counters.");
        }

        if (profile.StableSupportMonths < 0 ||
            profile.SocialCoordinationPressureMonths < 0 ||
            profile.LearningPressureMonths < 0 ||
            profile.SapienceProgress < 0)
        {
            errors.Add($"Regional biological profile {profile.SpeciesId} has negative sapience-emergence counters.");
        }

        if (profile.HungerPressure is < 0 or > 1 || profile.ShortageMonths < 0 || profile.FeedingMomentum is < 0 or > 1)
        {
            errors.Add($"Regional biological profile {profile.SpeciesId} has invalid hunger or feeding state.");
        }

        if (!Enum.IsDefined(profile.FoodStressState))
        {
            errors.Add($"Regional biological profile {profile.SpeciesId} has an invalid food stress state.");
        }

        if (profile.IsExtinct && profile.LastPopulation > 0)
        {
            errors.Add($"Regional biological profile {profile.SpeciesId} is extinct but still has population.");
        }

        ValidateTraitRange(profile.SpeciesId, profile.Traits, errors);
    }

    private static void ValidateTraitRange(string speciesId, BiologicalTraitProfile traits, ICollection<string> errors)
    {
        if (traits.ColdTolerance is < 0 or > 100 ||
            traits.HeatTolerance is < 0 or > 100 ||
            traits.DroughtTolerance is < 0 or > 100 ||
            traits.Flexibility is < 0 or > 100 ||
            traits.BodySize is < 0 or > 100 ||
            traits.Reproduction is < 0 or > 100 ||
            traits.Mobility is < 0 or > 100 ||
            traits.Defense is < 0 or > 100 ||
            traits.Resilience is < 0 or > 100)
        {
            errors.Add($"Regional biological profile {speciesId} has traits outside the valid range.");
        }
    }

    private static void ValidateSubstrate(string regionId, ProtoLifeSubstrate substrate, ICollection<string> errors)
    {
        if (substrate.ProtoFloraCapacity is < 0 or > 1 ||
            substrate.ProtoFaunaCapacity is < 0 or > 1 ||
            substrate.ProtoFloraPressure is < 0 or > 1 ||
            substrate.ProtoFaunaPressure is < 0 or > 1 ||
            substrate.FloraOccupancyDeficit is < 0 or > 1 ||
            substrate.FaunaOccupancyDeficit is < 0 or > 1 ||
            substrate.FloraSupportDeficit is < 0 or > 1 ||
            substrate.FaunaSupportDeficit is < 0 or > 1 ||
            substrate.EcologicalVacancy is < 0 or > 1 ||
            substrate.RecentCollapseOpening is < 0 or > 1)
        {
            errors.Add($"Region {regionId} has proto-life substrate values outside the normalized range.");
        }

        if (substrate.ProtoFloraReadinessMonths < 0 ||
            substrate.ProtoFaunaReadinessMonths < 0 ||
            substrate.ProtoFloraCandidateMonths < 0 ||
            substrate.ProtoFaunaCandidateMonths < 0 ||
            substrate.ProtoFloraGenesisCooldownMonths < 0 ||
            substrate.ProtoFaunaGenesisCooldownMonths < 0)
        {
            errors.Add($"Region {regionId} has negative proto-genesis readiness or cooldown counters.");
        }
    }
}
