using Species.Domain.Catalogs;
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
var viewState = new PlayerViewState();
var viewErrors = PlayerViewValidator.Validate(viewState, simulationEngine.CurrentWorld).ToArray();
if (viewErrors.Length > 0)
{
    Console.WriteLine("View validation failed:");

    foreach (var error in viewErrors)
    {
        Console.WriteLine($"- {error}");
    }

    return;
}

RenderCurrentScreen();

if (Console.IsInputRedirected)
{
    return;
}

while (true)
{
    var key = Console.ReadKey(intercept: true);
    if (key.Key == ConsoleKey.Escape)
    {
        break;
    }

    if (key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
    {
        if (!AdvanceOneMonth())
        {
            break;
        }
    }
    else if (key.Key == ConsoleKey.Tab)
    {
        viewState.CycleScreen();
    }
    else if (viewState.CurrentScreen == PlayerScreen.RegionViewer)
    {
        switch (key.Key)
        {
            case ConsoleKey.RightArrow:
            case ConsoleKey.D:
            case ConsoleKey.N:
                viewState.MoveToNextRegion(simulationEngine.CurrentWorld.Regions.Count);
                break;
            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
            case ConsoleKey.P:
                viewState.MoveToPreviousRegion(simulationEngine.CurrentWorld.Regions.Count);
                break;
        }
    }

    viewState.ClampRegionIndex(simulationEngine.CurrentWorld.Regions.Count);
    RenderCurrentScreen();
}

bool AdvanceOneMonth()
{
    var previousWorld = simulationEngine.CurrentWorld;
    var tickResult = simulationEngine.Tick();
    var postTickValidationErrors = WorldValidator.Validate(simulationEngine.CurrentWorld)
        .Concat(RegionEcologyValidator.Validate(simulationEngine.CurrentWorld, floraCatalog, faunaCatalog))
        .Concat(PopulationGroupValidator.Validate(simulationEngine.CurrentWorld))
        .Concat(SimulationTickValidator.Validate(previousWorld, tickResult))
        .Concat(GroupSurvivalValidator.Validate(previousWorld, tickResult))
        .Concat(MigrationValidator.Validate(previousWorld, tickResult))
        .Concat(DiscoveryStateValidator.Validate(simulationEngine.CurrentWorld, discoveryCatalog, tickResult))
        .Concat(AdvancementStateValidator.Validate(simulationEngine.CurrentWorld, advancementCatalog, tickResult))
        .Concat(ChronicleValidator.Validate(simulationEngine.CurrentWorld, tickResult))
        .Concat(PlayerViewValidator.Validate(viewState, simulationEngine.CurrentWorld))
        .ToArray();

    if (postTickValidationErrors.Length == 0)
    {
        return true;
    }

    if (!Console.IsOutputRedirected)
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
        }
    }

    Console.WriteLine("Post-tick validation failed:");
    foreach (var error in postTickValidationErrors)
    {
        Console.WriteLine($"- {error}");
    }

    return false;
}

void RenderCurrentScreen()
{
    if (!Console.IsOutputRedirected)
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
        }
    }

    Console.WriteLine(PlayerScreenRenderer.Render(simulationEngine.CurrentWorld, viewState, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog));
}
