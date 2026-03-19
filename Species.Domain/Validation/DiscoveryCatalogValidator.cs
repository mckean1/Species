using Species.Domain.Catalogs;

namespace Species.Domain.Validation;

public static class DiscoveryCatalogValidator
{
    public static IReadOnlyList<string> Validate(DiscoveryCatalog discoveryCatalog)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var definition in discoveryCatalog.Definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                errors.Add("A discovery definition is missing an ID.");
            }
            else if (!ids.Add(definition.Id))
            {
                errors.Add($"Duplicate discovery definition ID detected: {definition.Id}");
            }

            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                errors.Add($"Discovery definition {definition.Id} is missing a name.");
            }
        }

        return errors;
    }
}
