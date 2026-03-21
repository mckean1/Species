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

            if (definition.HabitatFertilityMin < SpeciesDefinitionConstants.NormalizedMinimum ||
                definition.HabitatFertilityMin > SpeciesDefinitionConstants.NormalizedMaximum)
            {
                errors.Add($"Flora species {definition.Id} has HabitatFertilityMin outside the normalized range.");
            }

            if (definition.HabitatFertilityMax < SpeciesDefinitionConstants.NormalizedMinimum ||
                definition.HabitatFertilityMax > SpeciesDefinitionConstants.NormalizedMaximum)
            {
                errors.Add($"Flora species {definition.Id} has HabitatFertilityMax outside the normalized range.");
            }

            if (definition.HabitatFertilityMin > definition.HabitatFertilityMax)
            {
                errors.Add($"Flora species {definition.Id} has an invalid fertility band.");
            }

            ValidateNonNegative(definition.GrowthRate, $"Flora species {definition.Id} has negative GrowthRate.", errors);
            ValidateNonNegative(definition.RecoveryRate, $"Flora species {definition.Id} has negative RecoveryRate.", errors);
            ValidateNonNegative(definition.UsableBiomass, $"Flora species {definition.Id} has negative UsableBiomass.", errors);
            ValidateNonNegative(definition.ConsumptionResilience, $"Flora species {definition.Id} has negative ConsumptionResilience.", errors);
            ValidateNonNegative(definition.SpreadTendency, $"Flora species {definition.Id} has negative SpreadTendency.", errors);
            ValidateNonNegative(definition.RegionalAbundance, $"Flora species {definition.Id} has negative RegionalAbundance.", errors);
            ValidateNonNegative(definition.Conspicuousness, $"Flora species {definition.Id} has negative Conspicuousness.", errors);
            ValidateTraitRange(definition.Id, definition.BaselineTraits, errors);

            if (!string.IsNullOrWhiteSpace(definition.ParentSpeciesId) &&
                !floraCatalog.Definitions.Any(item => string.Equals(item.Id, definition.ParentSpeciesId, StringComparison.Ordinal)))
            {
                errors.Add($"Flora species {definition.Id} references missing parent lineage {definition.ParentSpeciesId}.");
            }
        }

        return errors;
    }

    public static IReadOnlyList<string> Validate(FaunaSpeciesCatalog faunaCatalog, FloraSpeciesCatalog floraCatalog)
    {
        var errors = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var knownFloraIds = floraCatalog.Definitions.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);
        var knownFaunaIds = faunaCatalog.Definitions.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var definition in faunaCatalog.Definitions)
        {
            ValidateSharedDefinitionFields(definition.Id, definition.Name, definition.CoreBiomes, definition.SupportedWaterAvailabilities, seenIds, "fauna", errors);

            if (!Enum.IsDefined(definition.DietCategory))
            {
                errors.Add($"Fauna species {definition.Id} has an invalid DietCategory.");
            }

            if (definition.HabitatFertilityMin < SpeciesDefinitionConstants.NormalizedMinimum ||
                definition.HabitatFertilityMin > SpeciesDefinitionConstants.NormalizedMaximum)
            {
                errors.Add($"Fauna species {definition.Id} has HabitatFertilityMin outside the normalized range.");
            }

            if (definition.HabitatFertilityMax < SpeciesDefinitionConstants.NormalizedMinimum ||
                definition.HabitatFertilityMax > SpeciesDefinitionConstants.NormalizedMaximum)
            {
                errors.Add($"Fauna species {definition.Id} has HabitatFertilityMax outside the normalized range.");
            }

            if (definition.HabitatFertilityMin > definition.HabitatFertilityMax)
            {
                errors.Add($"Fauna species {definition.Id} has an invalid fertility band.");
            }

            ValidateDietLinks(definition, knownFloraIds, knownFaunaIds, errors);

            ValidateNonNegative(definition.RequiredIntake, $"Fauna species {definition.Id} has negative RequiredIntake.", errors);
            ValidateNonNegative(definition.ReproductionRate, $"Fauna species {definition.Id} has negative ReproductionRate.", errors);
            ValidateNonNegative(definition.MortalitySensitivity, $"Fauna species {definition.Id} has negative MortalitySensitivity.", errors);
            ValidateNonNegative(definition.Mobility, $"Fauna species {definition.Id} has negative Mobility.", errors);
            ValidateNonNegative(definition.FeedingEfficiency, $"Fauna species {definition.Id} has negative FeedingEfficiency.", errors);
            ValidateNonNegative(definition.PredatorVulnerability, $"Fauna species {definition.Id} has negative PredatorVulnerability.", errors);
            ValidateNonNegative(definition.RegionalAbundance, $"Fauna species {definition.Id} has negative RegionalAbundance.", errors);
            ValidateNonNegative(definition.Conspicuousness, $"Fauna species {definition.Id} has negative Conspicuousness.", errors);
            ValidateNonNegative(definition.FoodYield, $"Fauna species {definition.Id} has negative FoodYield.", errors);
            ValidateTraitRange(definition.Id, definition.BaselineTraits, errors);

            if (!string.IsNullOrWhiteSpace(definition.ParentSpeciesId) &&
                !faunaCatalog.Definitions.Any(item => string.Equals(item.Id, definition.ParentSpeciesId, StringComparison.Ordinal)))
            {
                errors.Add($"Fauna species {definition.Id} references missing parent lineage {definition.ParentSpeciesId}.");
            }
        }

        return errors;
    }

    public static IReadOnlyList<string> Validate(FaunaSpeciesCatalog faunaCatalog)
    {
        return Validate(faunaCatalog, new FloraSpeciesCatalog(Array.Empty<FloraSpeciesDefinition>()));
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
            errors.Add($"Species {speciesId} has baseline traits outside the valid range.");
        }
    }

    private static void ValidateDietLinks(
        FaunaSpeciesDefinition definition,
        IReadOnlySet<string> knownFloraIds,
        IReadOnlySet<string> knownFaunaIds,
        ICollection<string> errors)
    {
        if (definition.DietLinks.Count == 0 || definition.DietLinks.Sum(link => link.Weight) <= 0.0f)
        {
            errors.Add($"Fauna species {definition.Id} must declare a positive explicit diet link set.");
            return;
        }

        if (!definition.DietLinks.Any(link => !link.IsFallback))
        {
            errors.Add($"Fauna species {definition.Id} must declare at least one preferred diet link.");
        }

        foreach (var link in definition.DietLinks)
        {
            if (link.Weight <= 0.0f)
            {
                errors.Add($"Fauna species {definition.Id} has a nonpositive diet link weight.");
            }

            if (!Enum.IsDefined(link.TargetKind))
            {
                errors.Add($"Fauna species {definition.Id} has an invalid diet target kind.");
                continue;
            }

            if (link.TargetKind == Enums.FaunaDietTargetKind.FloraSpecies &&
                !knownFloraIds.Contains(link.TargetSpeciesId))
            {
                errors.Add($"Fauna species {definition.Id} references unknown flora diet target {link.TargetSpeciesId}.");
            }

            if (link.TargetKind == Enums.FaunaDietTargetKind.FaunaSpecies)
            {
                if (!knownFaunaIds.Contains(link.TargetSpeciesId))
                {
                    errors.Add($"Fauna species {definition.Id} references unknown fauna diet target {link.TargetSpeciesId}.");
                }

                if (string.Equals(link.TargetSpeciesId, definition.Id, StringComparison.Ordinal))
                {
                    errors.Add($"Fauna species {definition.Id} cannot target itself in the food web.");
                }
            }

            if (link.TargetKind == Enums.FaunaDietTargetKind.ScavengePool &&
                !string.IsNullOrWhiteSpace(link.TargetSpeciesId))
            {
                errors.Add($"Fauna species {definition.Id} scavenge links may not name a target species.");
            }

            if (link.TargetKind == Enums.FaunaDietTargetKind.ScavengePool && !link.IsFallback)
            {
                errors.Add($"Fauna species {definition.Id} scavenge links must be fallback-only.");
            }
        }
    }
}
