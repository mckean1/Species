using Species.Domain.Catalogs;

namespace Species.Domain.Diagnostics;

public static class AdvancementCatalogSummaryFormatter
{
    public static string Format(AdvancementCatalog advancementCatalog)
    {
        var lines = new List<string>
        {
            $"Advancements: {advancementCatalog.Definitions.Count}",
            string.Empty,
            "Advancement Definitions:"
        };

        foreach (var definition in advancementCatalog.Definitions.OrderBy(definition => definition.Id, StringComparer.Ordinal))
        {
            lines.Add(
                $"{definition.Id} | {definition.Name} | Category={definition.Category} | Effect={definition.PracticalEffectSummary}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
