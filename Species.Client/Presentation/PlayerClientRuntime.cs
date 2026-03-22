using Species.Domain.Catalogs;
using Species.Domain.Simulation;
using Species.Client.InputHandling;
using System.Diagnostics;

namespace Species.Client.Presentation;

public sealed class PlayerClientRuntime
{
    private const int TickDelayMilliseconds = 1000;
    private const int InputPollDelayMilliseconds = 25;

    private readonly SimulationEngine simulationEngine;
    private readonly PlayerViewState viewState;
    private readonly FloraSpeciesCatalog floraCatalog;
    private readonly FaunaSpeciesCatalog faunaCatalog;
    private readonly DiscoveryCatalog discoveryCatalog;
    private readonly AdvancementCatalog advancementCatalog;
    private readonly ConsoleFrameWriter frameWriter;
    private readonly PlayerInputContext inputContext;

    public PlayerClientRuntime(
        SimulationEngine simulationEngine,
        PlayerViewState viewState,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog,
        ConsoleFrameWriter frameWriter)
    {
        this.simulationEngine = simulationEngine;
        this.viewState = viewState;
        this.floraCatalog = floraCatalog;
        this.faunaCatalog = faunaCatalog;
        this.discoveryCatalog = discoveryCatalog;
        this.advancementCatalog = advancementCatalog;
        this.frameWriter = frameWriter;
        inputContext = new PlayerInputContext(
            simulationEngine,
            viewState,
            floraCatalog,
            faunaCatalog,
            discoveryCatalog,
            advancementCatalog,
            AdvanceOneMonth);
    }

    public void Run()
    {
        PlayerViewStateCoordinator.Synchronize(viewState, simulationEngine, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog);
        var viewErrors = PlayerViewValidator.Validate(viewState, simulationEngine.CurrentWorld, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog).ToArray();
        if (viewErrors.Length > 0)
        {
            WriteValidationErrors("View validation failed:", viewErrors);
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
                var result = PlayerInputRouter.ProcessKey(Console.ReadKey(intercept: true), inputContext);
                if ((result & PlayerInputResult.Exit) != 0)
                {
                    return;
                }

                if ((result & PlayerInputResult.ResetTickTimer) != 0)
                {
                    elapsedSinceLastTick.Restart();
                }

                shouldRender |= (result & PlayerInputResult.Render) != 0;
            }

            if (viewState.IsSimulationRunning && elapsedSinceLastTick.ElapsedMilliseconds >= TickDelayMilliseconds)
            {
                elapsedSinceLastTick.Restart();
                if (!AdvanceOneMonth())
                {
                    return;
                }

                shouldRender = true;
            }

            PlayerViewStateCoordinator.Synchronize(viewState, simulationEngine, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog);
            if (shouldRender)
            {
                RenderCurrentScreen();
            }

            Thread.Sleep(InputPollDelayMilliseconds);
        }
    }

    private bool AdvanceOneMonth()
    {
        var previousWorld = simulationEngine.CurrentWorld;
        var tickResult = simulationEngine.Tick();
        PlayerViewStateCoordinator.Synchronize(viewState, simulationEngine, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog);
        var validationErrors = ClientRuntimeValidation.ValidatePostTick(
            previousWorld,
            simulationEngine,
            tickResult,
            viewState,
            floraCatalog,
            faunaCatalog,
            discoveryCatalog,
            advancementCatalog);
        if (validationErrors.Length == 0)
        {
            return true;
        }

        if (!Console.IsOutputRedirected)
        {
            try
            {
                frameWriter.Reset();
                Console.Clear();
            }
            catch (IOException)
            {
            }
        }

        WriteValidationErrors("Post-tick validation failed:", validationErrors);
        return false;
    }

    private void RenderCurrentScreen()
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

        frameWriter.Render(frame, viewport);
    }

    private static void WriteValidationErrors(string title, IReadOnlyList<string> errors)
    {
        Console.WriteLine(title);
        foreach (var error in errors)
        {
            Console.WriteLine($"- {error}");
        }
    }
}
