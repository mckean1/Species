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
            PlayerScreen.Chronicle => ChronicleScreenRenderer.Render(world, viewState, viewport),
            PlayerScreen.Polity => PolityScreenRenderer.Render(world, viewState.FocalPolityId, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Advancements => AdvancementsScreenRenderer.Render(world, viewState.FocalPolityId, discoveryCatalog, advancementCatalog, viewState.CurrentAdvancementIndex, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Laws => LawsScreenRenderer.Render(world, viewState.FocalPolityId, viewState.CurrentLawIndex, viewState.IsSimulationRunning, viewState.IsLawActionMenuOpen, viewState.CurrentLawActionIndex, viewport),
            PlayerScreen.Regions => RegionViewerRenderer.Render(world, viewState.FocalPolityId, viewState.CurrentRegionIndex, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning, viewport),
            PlayerScreen.KnownPolities => KnownPolitiesScreenRenderer.Render(world, viewState.FocalPolityId, viewState.CurrentKnownPolityIndex, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning, viewport),
            PlayerScreen.KnownSpecies => KnownSpeciesScreenRenderer.Render(world, viewState.FocalPolityId, faunaCatalog, viewState.CurrentKnownSpeciesIndex, viewState.IsSimulationRunning, viewport),
            _ => throw new NotSupportedException($"Unsupported screen {viewState.CurrentScreen}.")
        };
    }
}
