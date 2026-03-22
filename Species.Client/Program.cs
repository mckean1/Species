using Species.Domain.Catalogs;
using Species.Domain.Generation;
using Species.Domain.Simulation;
using Species.Client.Presentation;

var floraCatalog = FloraSpeciesCatalog.CreateStarterSet();
var faunaCatalog = FaunaSpeciesCatalog.CreateStarterSet();
var sapientCatalog = SapientSpeciesCatalog.CreateStarterSet();
var world = WorldGenerator.Create(floraCatalog, faunaCatalog);
var discoveryCatalog = DiscoveryCatalog.CreateForWorld(world);
var advancementCatalog = AdvancementCatalog.CreateStarterSet();

var startupValidationErrors = ClientRuntimeValidation.ValidateStartup(world, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog);
if (startupValidationErrors.Length > 0)
{
    WriteValidationErrors("Startup validation failed:", startupValidationErrors);
    return;
}

var simulationEngine = new SimulationEngine(world, floraCatalog, faunaCatalog, sapientCatalog, discoveryCatalog, advancementCatalog);
var runtime = new PlayerClientRuntime(
    simulationEngine,
    new PlayerViewState(),
    floraCatalog,
    faunaCatalog,
    discoveryCatalog,
    advancementCatalog,
    new ConsoleFrameWriter());

runtime.Run();

static void WriteValidationErrors(string title, IReadOnlyList<string> errors)
{
    Console.WriteLine(title);
    foreach (var error in errors)
    {
        Console.WriteLine($"- {error}");
    }
}
