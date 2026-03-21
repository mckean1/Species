using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Client.DataBuilders;
using Species.Client.Enums;
using Species.Client.Presentation;

namespace Species.Client.Renderers;

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
            Species.Client.Enums.PlayerScreen.Chronicle => ChronicleScreenRenderer.Render(world, viewState, viewport),
            Species.Client.Enums.PlayerScreen.Polity => PolityScreenRenderer.Render(world, viewState.FocalPolityId, viewState.IsSimulationRunning, viewport),
            Species.Client.Enums.PlayerScreen.Government => GovernmentScreenRenderer.Render(world, viewState.FocalPolityId, viewState.IsSimulationRunning, viewport),
            Species.Client.Enums.PlayerScreen.Advancements => AdvancementsScreenRenderer.Render(world, viewState.FocalPolityId, discoveryCatalog, advancementCatalog, viewState.CurrentAdvancementIndex, viewState.IsSimulationRunning, viewport),
            Species.Client.Enums.PlayerScreen.Laws => LawsScreenRenderer.Render(world, viewState.FocalPolityId, viewState.CurrentLawIndex, viewState.IsSimulationRunning, viewState.IsLawActionMenuOpen, viewState.CurrentLawActionIndex, viewport),
            Species.Client.Enums.PlayerScreen.Regions => RegionViewerRenderer.Render(world, viewState.FocalPolityId, viewState.CurrentRegionIndex, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning, viewport),
            Species.Client.Enums.PlayerScreen.KnownPolities => KnownPolitiesScreenRenderer.Render(world, viewState.FocalPolityId, viewState.CurrentKnownPolityIndex, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning, viewport),
            Species.Client.Enums.PlayerScreen.KnownSpecies => KnownSpeciesScreenRenderer.Render(world, viewState.FocalPolityId, faunaCatalog, viewState.CurrentKnownSpeciesIndex, viewState.IsSimulationRunning, viewport),
            _ => throw new NotSupportedException($"Unsupported screen {viewState.CurrentScreen}.")
        };
    }
}
