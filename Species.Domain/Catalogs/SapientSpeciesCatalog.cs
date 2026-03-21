using Species.Domain.Models;

namespace Species.Domain.Catalogs;

public sealed class SapientSpeciesCatalog
{
    private readonly Dictionary<string, SapientSpeciesDefinition> definitionsById;
    private readonly List<SapientSpeciesDefinition> definitions;

    public SapientSpeciesCatalog(IReadOnlyList<SapientSpeciesDefinition> definitions)
    {
        this.definitions = definitions.ToList();
        Definitions = this.definitions;
        definitionsById = this.definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<SapientSpeciesDefinition> Definitions { get; }

    public SapientSpeciesDefinition? GetById(string id)
    {
        return definitionsById.GetValueOrDefault(id);
    }

    public void AddOrReplace(SapientSpeciesDefinition definition)
    {
        definitionsById[definition.Id] = definition;
        var index = definitions.FindIndex(item => string.Equals(item.Id, definition.Id, StringComparison.Ordinal));
        if (index >= 0)
        {
            definitions[index] = definition;
            return;
        }

        definitions.Add(definition);
    }

    public static SapientSpeciesCatalog CreateStarterSet()
    {
        return new SapientSpeciesCatalog(Array.Empty<SapientSpeciesDefinition>());
    }
}
