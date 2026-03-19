using Species.Domain.Catalogs;
using Species.Domain.Diagnostics;
using Species.Domain.Generation;
using Species.Domain.Simulation;
using Species.Domain.Validation;

var floraCatalog = FloraSpeciesCatalog.CreateStarterSet();
var faunaCatalog = FaunaSpeciesCatalog.CreateStarterSet();
var world = WorldGenerator.Create(floraCatalog, faunaCatalog);
var validationErrors = WorldValidator.Validate(world)
    .Concat(SpeciesDefinitionValidator.Validate(floraCatalog))
    .Concat(SpeciesDefinitionValidator.Validate(faunaCatalog))
    .Concat(RegionEcologyValidator.Validate(world, floraCatalog, faunaCatalog))
    .Concat(PopulationGroupValidator.Validate(world))
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

var simulationEngine = new SimulationEngine(world, floraCatalog, faunaCatalog);
var tickResult = simulationEngine.Tick();
var postTickValidationErrors = WorldValidator.Validate(simulationEngine.CurrentWorld)
    .Concat(RegionEcologyValidator.Validate(simulationEngine.CurrentWorld, floraCatalog, faunaCatalog))
    .Concat(PopulationGroupValidator.Validate(simulationEngine.CurrentWorld))
    .Concat(SimulationTickValidator.Validate(world, tickResult))
    .Concat(GroupSurvivalValidator.Validate(world, tickResult))
    .ToArray();

if (postTickValidationErrors.Length > 0)
{
    Console.WriteLine("Post-tick validation failed:");

    foreach (var error in postTickValidationErrors)
    {
        Console.WriteLine($"- {error}");
    }

    return;
}

Console.WriteLine(WorldSummaryFormatter.Format(world));
Console.WriteLine();
Console.WriteLine(SimulationTickFormatter.Format(tickResult));
Console.WriteLine();
Console.WriteLine(WorldSummaryFormatter.Format(simulationEngine.CurrentWorld));
Console.WriteLine();
Console.WriteLine(PopulationGroupSummaryFormatter.Format(simulationEngine.CurrentWorld));
Console.WriteLine();
Console.WriteLine(SpeciesCatalogSummaryFormatter.Format(floraCatalog, faunaCatalog));
