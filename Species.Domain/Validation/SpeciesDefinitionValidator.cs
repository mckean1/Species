using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Models;

namespace Species.Domain.Validation;

public static class SpeciesDefinitionValidator
{
    public static IReadOnlyList<string> Validate(FloraSpeciesCatalog floraCatalog)
    {
        var errors = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var definition in floraCatalog.Definitions)
        {
            ValidateSharedDefinitionFields(definition.Id, definition.Name, definition.CoreBiomes, definition.SupportedWaterAvailabilities, seenIds, "flora", errors);

            if (definition.PreferredFertilityMin < SpeciesDefinitionConstants.NormalizedMinimum ||
                definition.PreferredFertilityMin > SpeciesDefinitionConstants.NormalizedMaximum)
            {
                errors.Add($"Flora species {definition.Id} has PreferredFertilityMin outside the normalized range.");
            }

            if (definition.PreferredFertilityMax < SpeciesDefinitionConstants.NormalizedMinimum ||
                definition.PreferredFertilityMax > SpeciesDefinitionConstants.NormalizedMaximum)
            {
                errors.Add($"Flora species {definition.Id} has PreferredFertilityMax outside the normalized range.");
            }

            if (definition.PreferredFertilityMin > definition.PreferredFertilityMax)
            {
                errors.Add($"Flora species {definition.Id} has an invalid fertility band.");
            }

            ValidateNonNegative(definition.GrowthRate, $"Flora species {definition.Id} has negative GrowthRate.", errors);
            ValidateNonNegative(definition.FoodValue, $"Flora species {definition.Id} has negative FoodValue.", errors);
        }

        return errors;
    }

    public static IReadOnlyList<string> Validate(FaunaSpeciesCatalog faunaCatalog)
    {
        var errors = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var definition in faunaCatalog.Definitions)
        {
            ValidateSharedDefinitionFields(definition.Id, definition.Name, definition.CoreBiomes, definition.SupportedWaterAvailabilities, seenIds, "fauna", errors);

            if (!Enum.IsDefined(definition.DietCategory))
            {
                errors.Add($"Fauna species {definition.Id} has an invalid DietCategory.");
            }

            ValidateNonNegative(definition.FoodRequirement, $"Fauna species {definition.Id} has negative FoodRequirement.", errors);
            ValidateNonNegative(definition.ReproductionRate, $"Fauna species {definition.Id} has negative ReproductionRate.", errors);
            ValidateNonNegative(definition.MigrationTendency, $"Fauna species {definition.Id} has negative MigrationTendency.", errors);
            ValidateNonNegative(definition.FoodYield, $"Fauna species {definition.Id} has negative FoodYield.", errors);
        }

        return errors;
    }

    private static void ValidateSharedDefinitionFields(
        string id,
        string name,
        IReadOnlyList<Enums.Biome> coreBiomes,
        IReadOnlyList<Enums.WaterAvailability> supportedWaterAvailabilities,
        ISet<string> seenIds,
        string catalogName,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            errors.Add($"A {catalogName} species definition is missing an ID.");
        }
        else if (!seenIds.Add(id))
        {
            errors.Add($"Duplicate {catalogName} species ID detected: {id}");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add($"{catalogName} species {id} is missing a name.");
        }

        if (coreBiomes.Count == 0)
        {
            errors.Add($"{catalogName} species {id} must declare at least one core biome.");
        }

        if (supportedWaterAvailabilities.Count == 0)
        {
            errors.Add($"{catalogName} species {id} must declare at least one supported water availability.");
        }
    }

    private static void ValidateNonNegative(float value, string message, ICollection<string> errors)
    {
        if (value < SpeciesDefinitionConstants.NormalizedMinimum)
        {
            errors.Add(message);
        }
    }
}
