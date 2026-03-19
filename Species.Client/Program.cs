using Species.Domain.Catalogs;
using Species.Domain.Diagnostics;
using Species.Domain.Generation;
using Species.Domain.Simulation;
using Species.Domain.Validation;

var floraCatalog = FloraSpeciesCatalog.CreateStarterSet();
var faunaCatalog = FaunaSpeciesCatalog.CreateStarterSet();
var world = WorldGenerator.Create(floraCatalog, faunaCatalog);
var discoveryCatalog = DiscoveryCatalog.CreateForWorld(world);
var advancementCatalog = AdvancementCatalog.CreateStarterSet();
var validationErrors = WorldValidator.Validate(world)
    .Concat(SpeciesDefinitionValidator.Validate(floraCatalog))
    .Concat(SpeciesDefinitionValidator.Validate(faunaCatalog))
    .Concat(DiscoveryCatalogValidator.Validate(discoveryCatalog))
    .Concat(AdvancementCatalogValidator.Validate(advancementCatalog))
    .Concat(ChronicleValidator.Validate(world))
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

var simulationEngine = new SimulationEngine(world, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog);
var tickResult = simulationEngine.Tick();
var postTickValidationErrors = WorldValidator.Validate(simulationEngine.CurrentWorld)
    .Concat(RegionEcologyValidator.Validate(simulationEngine.CurrentWorld, floraCatalog, faunaCatalog))
    .Concat(PopulationGroupValidator.Validate(simulationEngine.CurrentWorld))
    .Concat(SimulationTickValidator.Validate(world, tickResult))
    .Concat(GroupSurvivalValidator.Validate(world, tickResult))
    .Concat(MigrationValidator.Validate(world, tickResult))
    .Concat(DiscoveryStateValidator.Validate(simulationEngine.CurrentWorld, discoveryCatalog, tickResult))
    .Concat(AdvancementStateValidator.Validate(simulationEngine.CurrentWorld, advancementCatalog, tickResult))
    .Concat(ChronicleValidator.Validate(simulationEngine.CurrentWorld, tickResult))
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

Console.WriteLine(ChronicleFeedFormatter.Format(simulationEngine.CurrentWorld.Chronicle));
Console.WriteLine();
Console.WriteLine(ChronicleDebugFormatter.Format(simulationEngine.CurrentWorld.Chronicle));
Console.WriteLine();
Console.WriteLine(PopulationGroupSummaryFormatter.Format(simulationEngine.CurrentWorld));
