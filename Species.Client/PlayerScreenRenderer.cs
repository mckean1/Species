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
            PlayerScreen.Chronicle => ChronicleScreenRenderer.Render(world, viewState.FocalGroupId, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Polity => PolityScreenRenderer.Render(world, viewState.FocalGroupId, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Advancements => AdvancementsScreenRenderer.Render(world, viewState.FocalGroupId, discoveryCatalog, advancementCatalog, viewState.CurrentAdvancementIndex, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Laws => LawsScreenRenderer.Render(world, viewState.FocalGroupId, viewState.CurrentLawIndex, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Regions => RegionViewerRenderer.Render(world, viewState.FocalGroupId, viewState.CurrentRegionIndex, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning, viewport),
            PlayerScreen.KnownPolities => KnownPolitiesScreenRenderer.Render(world, viewState.FocalGroupId, viewState.CurrentKnownPolityIndex, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning, viewport),
            PlayerScreen.KnownSpecies => KnownSpeciesScreenRenderer.Render(world, viewState.FocalGroupId, faunaCatalog, viewState.CurrentKnownSpeciesIndex, viewState.IsSimulationRunning, viewport),
            _ => throw new NotSupportedException($"Unsupported screen {viewState.CurrentScreen}.")
        };
    }
}
