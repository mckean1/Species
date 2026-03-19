using Species.Domain.Catalogs;
using Species.Domain.Models;

public static class PlayerScreenRenderer
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
        return viewState.CurrentScreen switch
        {
            PlayerScreen.Chronicle => ChronicleScreenRenderer.Render(world, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Polity => PolityScreenRenderer.Render(world, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Advancements => AdvancementsScreenRenderer.Render(world, discoveryCatalog, advancementCatalog, viewState.CurrentAdvancementIndex, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Laws => LawsScreenRenderer.Render(world, viewState.CurrentLawIndex, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Regions => RegionViewerRenderer.Render(world, viewState.CurrentRegionIndex, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog, viewport),
            PlayerScreen.KnownPolities => KnownPolitiesScreenRenderer.Render(world, viewState.CurrentKnownPolityIndex, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning, viewport),
            PlayerScreen.KnownSpecies => KnownSpeciesScreenRenderer.Render(world, viewState.CurrentKnownSpeciesIndex, viewState.IsSimulationRunning, viewport),
            _ => throw new NotSupportedException($"Unsupported screen {viewState.CurrentScreen}.")
        };
    }
}
