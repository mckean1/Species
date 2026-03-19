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
            PlayerScreen.RegionViewer => RegionViewerRenderer.Render(world, viewState.CurrentRegionIndex, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog),
            _ => throw new NotSupportedException($"Unsupported screen {viewState.CurrentScreen}.")
        };
    }
}
