using Species.Domain.Catalogs;
using Species.Domain.Generation;
using Species.Domain.Models;
using Species.Domain.Simulation;
using Species.Domain.Validation;
using Species.Client.Enums;
using Species.Client.Presentation;
using System.Diagnostics;
using Species.Client.ViewModelFactories;

const int TickDelayMilliseconds = 1000;
const int InputPollDelayMilliseconds = 25;

var floraCatalog = FloraSpeciesCatalog.CreateStarterSet();
var faunaCatalog = FaunaSpeciesCatalog.CreateStarterSet();
var sapientCatalog = SapientSpeciesCatalog.CreateStarterSet();
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

var simulationEngine = new SimulationEngine(world, floraCatalog, faunaCatalog, sapientCatalog, discoveryCatalog, advancementCatalog);
var viewState = new PlayerViewState();
viewState.EnsureFocalPolity(simulationEngine.CurrentWorld);
simulationEngine.PlayerPolityId = viewState.FocalPolityId;
var chronicleFrameRenderer = new ConsoleFrameWriter();
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
        else if (key.Key == ConsoleKey.Backspace && viewState.CurrentScreen == PlayerScreen.Chronicle)
        {
            var chronicleData = ChronicleViewModelFactory.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState);
            viewState.ReturnChronicleToLive(chronicleData.UrgentItems.Count);
            shouldRender = true;
        }
        else if (key.Key == ConsoleKey.Spacebar)
        {
            viewState.ToggleSimulation();
            viewState.CloseLawActionMenu();
            elapsedSinceLastTick.Restart();
            shouldRender = true;
        }
        else if (key.Key == ConsoleKey.N && !viewState.IsSimulationRunning)
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
        else if (viewState.CurrentScreen == PlayerScreen.Chronicle)
        {
            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    viewState.MoveToPreviousChronicleMode();
                    shouldRender = true;
                    break;
                case ConsoleKey.RightArrow:
                    viewState.MoveToNextChronicleMode();
                    shouldRender = true;
                    break;
                case ConsoleKey.UpArrow:
                    {
                        var chronicleData = ChronicleViewModelFactory.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState);
                        viewState.MoveChronicleSelection(chronicleData.UrgentItems.Count, chronicleData.Entries.Count, -1);
                        shouldRender = true;
                        break;
                    }
                case ConsoleKey.DownArrow:
                    {
                        var chronicleData = ChronicleViewModelFactory.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState);
                        viewState.MoveChronicleSelection(chronicleData.UrgentItems.Count, chronicleData.Entries.Count, 1);
                        shouldRender = true;
                        break;
                    }
                case ConsoleKey.Enter:
                    {
                        var chronicleData = ChronicleViewModelFactory.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState);
                        if (chronicleData.SelectedUrgent is not null)
                        {
                            viewState.SetScreen(chronicleData.SelectedUrgent.TargetScreen);
                            if (chronicleData.SelectedUrgent.TargetScreen == PlayerScreen.Laws)
                            {
                                viewState.SetLawSelection(0);
                            }

                            shouldRender = true;
                        }

                        break;
                    }
            }
        }
        else if (viewState.CurrentScreen == PlayerScreen.Regions)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    viewState.MoveRegionSelection(simulationEngine.CurrentWorld.Regions.Count, -1);
                    shouldRender = true;
                    break;
                case ConsoleKey.DownArrow:
                    viewState.MoveRegionSelection(simulationEngine.CurrentWorld.Regions.Count, 1);
                    shouldRender = true;
                    break;
            }
        }
        else if (viewState.CurrentScreen == PlayerScreen.KnownPolities)
        {
            var polityCount = KnownPolitiesViewModelFactory.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState.CurrentKnownPolityIndex, discoveryCatalog, advancementCatalog).Polities.Count;
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (polityCount > 0)
                    {
                        viewState.MoveKnownPolitySelection(polityCount, -1);
                        shouldRender = true;
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (polityCount > 0)
                    {
                        viewState.MoveKnownPolitySelection(polityCount, 1);
                        shouldRender = true;
                    }
                    break;
            }
        }
        else if (viewState.CurrentScreen == PlayerScreen.Advancements)
        {
            var advancementCount = AdvancementViewModelFactory.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, discoveryCatalog, advancementCatalog, viewState.CurrentAdvancementIndex).Items.Count;
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (advancementCount > 0)
                    {
                        viewState.MoveAdvancementSelection(advancementCount, -1);
                        shouldRender = true;
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (advancementCount > 0)
                    {
                        viewState.MoveAdvancementSelection(advancementCount, 1);
                        shouldRender = true;
                    }
                    break;
            }
        }
        else if (viewState.CurrentScreen == PlayerScreen.Laws)
        {
            var lawsData = LawsViewModelFactory.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState.CurrentLawIndex);
            var lawCount = lawsData.Laws.Count;
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (viewState.IsLawActionMenuOpen)
                    {
                        viewState.MoveLawActionSelection(-1);
                        shouldRender = true;
                    }
                    else if (lawCount > 0)
                    {
                        viewState.MoveLawSelection(lawCount, -1);
                        shouldRender = true;
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (viewState.IsLawActionMenuOpen)
                    {
                        viewState.MoveLawActionSelection(1);
                        shouldRender = true;
                    }
                    else if (lawCount > 0)
                    {
                        viewState.MoveLawSelection(lawCount, 1);
                        shouldRender = true;
                    }
                    break;
                case ConsoleKey.Enter:
                    if (!viewState.IsSimulationRunning && lawsData.HasSelectedPendingDecision)
                    {
                        if (!viewState.IsLawActionMenuOpen)
                        {
                            viewState.OpenLawActionMenu();
                            shouldRender = true;
                        }
                        else
                        {
                            var actionApplied = viewState.GetSelectedLawAction() == LawDecisionAction.Pass
                                ? simulationEngine.PassActiveLawProposal()
                                : simulationEngine.VetoActiveLawProposal();
                            if (actionApplied)
                            {
                                viewState.CloseLawActionMenu();
                                shouldRender = true;
                            }
                        }
                    }
                    break;
                case ConsoleKey.Backspace:
                    if (viewState.IsLawActionMenuOpen)
                    {
                        viewState.CloseLawActionMenu();
                        shouldRender = true;
                    }
                    break;
            }
        }
        else if (viewState.CurrentScreen == PlayerScreen.KnownSpecies)
        {
            var speciesCount = KnownSpeciesViewModelFactory.Build(simulationEngine.CurrentWorld, faunaCatalog, viewState.FocalPolityId, viewState.CurrentKnownSpeciesIndex).Species.Count;
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (speciesCount > 0)
                    {
                        viewState.MoveKnownSpeciesSelection(speciesCount, -1);
                        shouldRender = true;
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (speciesCount > 0)
                    {
                        viewState.MoveKnownSpeciesSelection(speciesCount, 1);
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
    viewState.ClampRegionIndex(RegionsViewModelFactory.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState.CurrentRegionIndex, floraCatalog, faunaCatalog, discoveryCatalog).Regions.Count);
    viewState.ClampKnownPolityIndex(KnownPolitiesViewModelFactory.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState.CurrentKnownPolityIndex, discoveryCatalog, advancementCatalog).Polities.Count);
    viewState.ClampAdvancementIndex(AdvancementViewModelFactory.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, discoveryCatalog, advancementCatalog, viewState.CurrentAdvancementIndex).Items.Count);
    viewState.ClampLawIndex(LawsViewModelFactory.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState.CurrentLawIndex).Laws.Count);
    viewState.ClampKnownSpeciesIndex(KnownSpeciesViewModelFactory.Build(simulationEngine.CurrentWorld, faunaCatalog, viewState.FocalPolityId, viewState.CurrentKnownSpeciesIndex).Species.Count);
    var currentChronicleData = ChronicleViewModelFactory.Build(simulationEngine.CurrentWorld, viewState.FocalPolityId, viewState);
    viewState.ClampChronicleSelection(currentChronicleData.UrgentItems.Count, currentChronicleData.Entries.Count);

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
    var frame = PlayerScreenRouter.Render(
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
        .Concat(SpeciesDefinitionValidator.Validate(faunaCatalog, floraCatalog))
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
