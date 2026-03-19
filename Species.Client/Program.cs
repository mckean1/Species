using Species.Domain.Catalogs;
using Species.Domain.Diagnostics;
using Species.Domain.Generation;
using Species.Domain.Validation;

var world = WorldGenerator.Create();
var floraCatalog = FloraSpeciesCatalog.CreateStarterSet();
var faunaCatalog = FaunaSpeciesCatalog.CreateStarterSet();
var validationErrors = WorldValidator.Validate(world)
    .Concat(SpeciesDefinitionValidator.Validate(floraCatalog))
    .Concat(SpeciesDefinitionValidator.Validate(faunaCatalog))
    .ToArray();

if (validationErrors.Length > 0)
{
    Console.WriteLine("Startup validation failed:");

    foreach (var error in validationErrors)
    {
        Console.WriteLine($"- {error}");
    }

    return;
}

Console.WriteLine(WorldSummaryFormatter.Format(world));
Console.WriteLine();
Console.WriteLine(SpeciesCatalogSummaryFormatter.Format(floraCatalog, faunaCatalog));
