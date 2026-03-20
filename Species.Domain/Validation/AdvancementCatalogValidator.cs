using Species.Domain.Catalogs;

namespace Species.Domain.Validation;

public static class AdvancementCatalogValidator
{
    public static IReadOnlyList<string> Validate(AdvancementCatalog advancementCatalog)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var definition in advancementCatalog.Definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                errors.Add("An advancement definition is missing an ID.");
            }
            else if (!ids.Add(definition.Id))
            {
                errors.Add($"Duplicate advancement definition ID detected: {definition.Id}");
            }

            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                errors.Add($"Advancement definition {definition.Id} is missing a name.");
            }

            if (string.IsNullOrWhiteSpace(definition.PrerequisiteSummary))
            {
                errors.Add($"Advancement definition {definition.Id} is missing a prerequisite summary.");
            }
        }

        return errors;
    }
}
