using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Client.Enums;
using Species.Client.Renderers;
using Species.Client.ViewModelFactories;

namespace Species.Client.Presentation;

public static class PlayerScreenRouter
{
    public static string Render(
        World world,
        PlayerViewState viewState,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog,
        TerminalViewport viewport)
    {
        // In primitive-world mode, polity-dependent screens show a placeholder
        if (viewState.IsPrimitiveWorldMode && IsPolityDependentScreen(viewState.CurrentScreen))
        {
            return RenderPrimitiveWorldPlaceholder(viewState.CurrentScreen, world, viewState.IsSimulationRunning, viewport);
        }
        
        return viewState.CurrentScreen switch
        {
            PlayerScreen.Chronicle => ChronicleRenderer.Render(
                ChronicleViewModelFactory.Build(world, viewState.FocalPolityId, viewState.CreateChronicleViewRequest(), viewState.IsSimulationRunning),
                viewport),
            PlayerScreen.Polity => PolityRenderer.Render(
                PolityViewModelFactory.Build(world, viewState.FocalPolityId, viewState.IsSimulationRunning),
                viewport),
            PlayerScreen.Government => GovernmentRenderer.Render(
                GovernmentViewModelFactory.Build(world, viewState.FocalPolityId, viewState.IsSimulationRunning),
                viewport),
            PlayerScreen.Advancements => AdvancementsRenderer.Render(
                AdvancementViewModelFactory.Build(world, viewState.FocalPolityId, discoveryCatalog, advancementCatalog, floraCatalog, faunaCatalog, viewState.CurrentAdvancementIndex, viewState.IsSimulationRunning),
                viewport),
            PlayerScreen.Laws => LawsRenderer.Render(
                LawsViewModelFactory.Build(world, viewState.FocalPolityId, viewState.CurrentLawIndex, viewState.IsSimulationRunning, viewState.IsLawActionMenuOpen, viewState.CurrentLawActionIndex),
                viewport),
            PlayerScreen.Regions => RegionRenderer.Render(
                RegionsViewModelFactory.Build(world, viewState.FocalPolityId, viewState.CurrentRegionIndex, floraCatalog, faunaCatalog, discoveryCatalog, viewState.IsSimulationRunning),
                viewport),
            PlayerScreen.KnownPolities => KnownPolitiesRenderer.Render(
                KnownPolitiesViewModelFactory.Build(world, viewState.FocalPolityId, viewState.CurrentKnownPolityIndex, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning),
                viewport),
            PlayerScreen.KnownSpecies => KnownSpeciesRenderer.Render(
                KnownSpeciesViewModelFactory.Build(world, floraCatalog, faunaCatalog, viewState.FocalPolityId, viewState.CurrentKnownSpeciesIndex, viewState.IsSimulationRunning),
                viewport),
            _ => throw new NotSupportedException($"Unsupported screen {viewState.CurrentScreen}.")
        };
    }

    private static bool IsPolityDependentScreen(PlayerScreen screen)
    {
        return screen is PlayerScreen.Chronicle 
            or PlayerScreen.Laws 
            or PlayerScreen.Government 
            or PlayerScreen.Polity 
            or PlayerScreen.KnownPolities 
            or PlayerScreen.Advancements;
    }

    private static string RenderPrimitiveWorldPlaceholder(PlayerScreen screen, World world, bool isSimulationRunning, TerminalViewport viewport)
    {
        var innerWidth = Math.Max(84, viewport.Width - 4);
        var bodyHeight = Math.Max(10, viewport.Height - 10);
        var currentDate = $"Year {world.CurrentYear}, Month {world.CurrentMonth}";
        
        var lines = new List<string>();
        lines.AddRange(PlayerScreenShell.BuildHeader(screen.ToString(), "No polities", currentDate, isSimulationRunning, isPrimitiveWorldMode: true, innerWidth));
        
        var message = new[]
        {
            "",
            $"The {screen} screen requires sapient polities to function.",
            "",
            "The world is currently in a primitive, pre-sapient state.",
            "Only primitive flora and fauna have been seeded.",
            "",
            "Available screens:",
            "  • Regions - inspect regional biology and populations",
            "  • KnownSpecies - view all seeded species and traits",
            "",
            "Simulation can still run. Ecological systems will process,",
            "but no polity or group systems will operate.",
            "",
            "Use Tab to navigate to available screens."
        };
        
        var startPadding = Math.Max(0, (bodyHeight - message.Length) / 2);
        for (var i = 0; i < startPadding; i++)
        {
            lines.Add(PlayerScreenShell.BorderLine("", innerWidth));
        }
        
        foreach (var line in message)
        {
            lines.Add(PlayerScreenShell.BorderLine(PlayerScreenShell.PadVisible(line, innerWidth), innerWidth));
        }
        
        while (lines.Count < viewport.Height - 3)
        {
            lines.Add(PlayerScreenShell.BorderLine("", innerWidth));
        }
        
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        lines.Add(PlayerScreenShell.BuildFooter(innerWidth, ["Tab: Screens", "Space: Pause/Run", "N: Next Tick"]));
        lines.Add(PlayerScreenShell.HorizontalBorder(innerWidth));
        
        return string.Join(Environment.NewLine, lines);
    }
}
