using Species.Domain.Catalogs;
using Species.Domain.Generation;
using Species.Domain.Models;
using Species.Domain.Simulation;
using Species.Domain.Validation;
using System.Diagnostics;

const int TickDelayMilliseconds = 1000;
const int InputPollDelayMilliseconds = 25;

var floraCatalog = FloraSpeciesCatalog.CreateStarterSet();
var faunaCatalog = FaunaSpeciesCatalog.CreateStarterSet();
var world = WorldGenerator.Create(floraCatalog, faunaCatalog);
var discoveryCatalog = DiscoveryCatalog.CreateForWorld(world);
var advancementCatalog = AdvancementCatalog.CreateStarterSet();
var validationErrors = CollectStartupValidationErrors(world);

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
viewState.EnsureFocalPolity(simulationEngine.CurrentWorld);
simulationEngine.PlayerPolityId = viewState.FocalPolityId;
var chronicleFrameRenderer = new ConsoleFrameRenderer();
var viewErrors = PlayerViewValidator.Validate(viewState, simulationEngine.CurrentWorld, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog).ToArray();
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

        if (PlayerScreenNavigation.TryGetScreenForHotkey(key.Key, out var hotkeyScreen))
        {
            viewState.SetScreen(hotkeyScreen);
            shouldRender = true;
        }
        else if (key.Key == ConsoleKey.Spacebar)
        {
            viewState.ToggleSimulation();
            elapsedSinceLastTick.Restart();
            shouldRender = true;
        }
        else if (key.Key == ConsoleKey.Enter && !viewState.IsSimulationRunning)
        {
            if (!AdvanceOneMonth())
            {
                break;
            }

            elapsedSinceLastTick.Restart();
            shouldRender = true;
        }
        else if (key.Key == ConsoleKey.Tab)
        {
            viewState.CycleScreen();
            shouldRender = true;
        }
        else if (viewState.CurrentScreen == PlayerScreen.Regions)
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
        else if (viewState.CurrentScreen == PlayerScreen.KnownPolities)
        {
            var polityCount = KnownPolitiesScreenDataBuilder.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState.CurrentKnownPolityIndex, discoveryCatalog, advancementCatalog).Polities.Count;
            switch (key.Key)
            {
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                case ConsoleKey.N:
                    if (polityCount > 0)
                    {
                        viewState.MoveToNextKnownPolity(polityCount);
                        shouldRender = true;
                    }
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                case ConsoleKey.P:
                    if (polityCount > 0)
                    {
                        viewState.MoveToPreviousKnownPolity(polityCount);
                        shouldRender = true;
                    }
                    break;
            }
        }
        else if (viewState.CurrentScreen == PlayerScreen.Advancements)
        {
            var advancementCount = AdvancementsScreenDataBuilder.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, discoveryCatalog, advancementCatalog, viewState.CurrentAdvancementIndex).Items.Count;
            switch (key.Key)
            {
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                case ConsoleKey.N:
                    if (advancementCount > 0)
                    {
                        viewState.MoveToNextAdvancement(advancementCount);
                        shouldRender = true;
                    }
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                case ConsoleKey.P:
                    if (advancementCount > 0)
                    {
                        viewState.MoveToPreviousAdvancement(advancementCount);
                        shouldRender = true;
                    }
                    break;
            }
        }
        else if (viewState.CurrentScreen == PlayerScreen.Laws)
        {
            var lawCount = LawsScreenDataBuilder.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState.CurrentLawIndex).Laws.Count;
            switch (key.Key)
            {
                case ConsoleKey.P:
                    if (!viewState.IsSimulationRunning && simulationEngine.PassActiveLawProposal())
                    {
                        shouldRender = true;
                    }
                    break;
                case ConsoleKey.V:
                    if (!viewState.IsSimulationRunning && simulationEngine.VetoActiveLawProposal())
                    {
                        shouldRender = true;
                    }
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                case ConsoleKey.N:
                    if (lawCount > 0)
                    {
                        viewState.MoveToNextLaw(lawCount);
                        shouldRender = true;
                    }
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    if (lawCount > 0)
                    {
                        viewState.MoveToPreviousLaw(lawCount);
                        shouldRender = true;
                    }
                    break;
            }
        }
        else if (viewState.CurrentScreen == PlayerScreen.KnownSpecies)
        {
            var speciesCount = KnownSpeciesScreenDataBuilder.Build(simulationEngine.CurrentWorld, faunaCatalog, viewState.FocalPolityId, viewState.CurrentKnownSpeciesIndex).Species.Count;
            switch (key.Key)
            {
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                case ConsoleKey.N:
                    if (speciesCount > 0)
                    {
                        viewState.MoveToNextKnownSpecies(speciesCount);
                        shouldRender = true;
                    }
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                case ConsoleKey.P:
                    if (speciesCount > 0)
                    {
                        viewState.MoveToPreviousKnownSpecies(speciesCount);
                        shouldRender = true;
                    }
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

    viewState.EnsureFocalPolity(simulationEngine.CurrentWorld);
    simulationEngine.PlayerPolityId = viewState.FocalPolityId;
    viewState.ClampRegionIndex(RegionsScreenDataBuilder.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState.CurrentRegionIndex, floraCatalog, faunaCatalog, discoveryCatalog).Regions.Count);
    viewState.ClampKnownPolityIndex(KnownPolitiesScreenDataBuilder.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState.CurrentKnownPolityIndex, discoveryCatalog, advancementCatalog).Polities.Count);
    viewState.ClampAdvancementIndex(AdvancementsScreenDataBuilder.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, discoveryCatalog, advancementCatalog, viewState.CurrentAdvancementIndex).Items.Count);
    viewState.ClampLawIndex(LawsScreenDataBuilder.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState.CurrentLawIndex).Laws.Count);
    viewState.ClampKnownSpeciesIndex(KnownSpeciesScreenDataBuilder.Build(simulationEngine.CurrentWorld, faunaCatalog, viewState.FocalPolityId, viewState.CurrentKnownSpeciesIndex).Species.Count);

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
    var postTickValidationErrors = CollectPostTickValidationErrors(previousWorld, tickResult);

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

    chronicleFrameRenderer.Render(frame, viewport);
}

string[] CollectStartupValidationErrors(World currentWorld)
{
    return WorldValidator.Validate(currentWorld)
        .Concat(SpeciesDefinitionValidator.Validate(floraCatalog))
        .Concat(SpeciesDefinitionValidator.Validate(faunaCatalog))
        .Concat(DiscoveryCatalogValidator.Validate(discoveryCatalog))
        .Concat(AdvancementCatalogValidator.Validate(advancementCatalog))
        .Concat(ChronicleValidator.Validate(currentWorld))
        .Concat(RegionEcologyValidator.Validate(currentWorld, floraCatalog, faunaCatalog))
        .Concat(PolityValidator.Validate(currentWorld))
        .Concat(PopulationGroupValidator.Validate(currentWorld))
        .ToArray();
}

string[] CollectPostTickValidationErrors(World previousWorld, SimulationTickResult tickResult)
{
    return WorldValidator.Validate(simulationEngine.CurrentWorld)
        .Concat(RegionEcologyValidator.Validate(simulationEngine.CurrentWorld, floraCatalog, faunaCatalog))
        .Concat(PolityValidator.Validate(simulationEngine.CurrentWorld))
        .Concat(PopulationGroupValidator.Validate(simulationEngine.CurrentWorld))
        .Concat(SimulationTickValidator.Validate(previousWorld, tickResult))
        .Concat(GroupSurvivalValidator.Validate(previousWorld, tickResult))
        .Concat(MigrationValidator.Validate(previousWorld, tickResult))
        .Concat(DiscoveryStateValidator.Validate(simulationEngine.CurrentWorld, discoveryCatalog, tickResult))
        .Concat(AdvancementStateValidator.Validate(simulationEngine.CurrentWorld, advancementCatalog, tickResult))
        .Concat(ChronicleValidator.Validate(simulationEngine.CurrentWorld, tickResult))
        .Concat(PlayerViewValidator.Validate(viewState, simulationEngine.CurrentWorld, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog))
        .ToArray();
}
