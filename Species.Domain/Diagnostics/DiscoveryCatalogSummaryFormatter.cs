using Species.Domain.Catalogs;

namespace Species.Domain.Diagnostics;

public static class DiscoveryCatalogSummaryFormatter
{
    public static string Format(DiscoveryCatalog discoveryCatalog)
    {
        var lines = new List<string>
        {
            $"Discoveries: {discoveryCatalog.Definitions.Count}",
            string.Empty,
            "Discovery Definitions:"
        };

        foreach (var definition in discoveryCatalog.Definitions.OrderBy(definition => definition.Id, StringComparer.Ordinal))
        {
            lines.Add(
                $"{definition.Id} | {definition.Name} | Category={definition.Category} | Effect={definition.DecisionEffectSummary}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
