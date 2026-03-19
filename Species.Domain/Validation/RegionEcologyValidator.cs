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
        }

        return errors;
    }
}
