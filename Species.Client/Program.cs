using Species.Domain.Catalogs;
using Species.Domain.Generation;
using Species.Domain.Simulation;
using Species.Domain.Validation;
using System.Diagnostics;

const int TickDelayMilliseconds = 500;
const int InputPollDelayMilliseconds = 25;

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
var chronicleFrameRenderer = new ConsoleFrameRenderer();
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

var elapsedSinceLastTick = Stopwatch.StartNew();

while (true)
{
    var shouldRender = false;

    while (Console.KeyAvailable)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Escape)
        {
            return;
        }

        if (key.Key == ConsoleKey.Spacebar)
        {
            viewState.ToggleSimulation();
            elapsedSinceLastTick.Restart();
            shouldRender = true;
        }
        else if (key.Key == ConsoleKey.Tab)
        {
            viewState.CycleScreen();
            shouldRender = true;
        }
        else if (viewState.CurrentScreen == PlayerScreen.RegionViewer)
        {
            switch (key.Key)
            {
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                case ConsoleKey.N:
                    viewState.MoveToNextRegion(simulationEngine.CurrentWorld.Regions.Count);
                    shouldRender = true;
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                case ConsoleKey.P:
                    viewState.MoveToPreviousRegion(simulationEngine.CurrentWorld.Regions.Count);
                    shouldRender = true;
                    break;
            }
        }
    }

    if (viewState.IsSimulationRunning && elapsedSinceLastTick.ElapsedMilliseconds >= TickDelayMilliseconds)
    {
        elapsedSinceLastTick.Restart();

        if (!AdvanceOneMonth())
        {
            break;
        }

        shouldRender = true;
    }

    viewState.ClampRegionIndex(simulationEngine.CurrentWorld.Regions.Count);

    if (shouldRender)
    {
        RenderCurrentScreen();
    }

    Thread.Sleep(InputPollDelayMilliseconds);
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
            chronicleFrameRenderer.Reset();
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
    var viewport = TerminalViewport.GetCurrent();
    var frame = PlayerScreenRenderer.Render(
        simulationEngine.CurrentWorld,
        viewState,
        floraCatalog,
        faunaCatalog,
        discoveryCatalog,
        advancementCatalog,
        viewport);

    if (viewState.CurrentScreen == PlayerScreen.Chronicle)
    {
        chronicleFrameRenderer.Render(frame, viewport);
        return;
    }

    chronicleFrameRenderer.Reset();

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

    Console.WriteLine(frame);
}
