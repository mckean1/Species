using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Client.Enums;
using Species.Client.Renderers;

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
        return viewState.CurrentScreen switch
        {
            PlayerScreen.Chronicle => ChronicleRenderer.Render(world, viewState, viewport),
            PlayerScreen.Polity => PolityRenderer.Render(world, viewState.FocalPolityId, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Government => GovernmentRenderer.Render(world, viewState.FocalPolityId, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Advancements => AdvancementsRenderer.Render(world, viewState.FocalPolityId, discoveryCatalog, advancementCatalog, viewState.CurrentAdvancementIndex, viewState.IsSimulationRunning, viewport),
            PlayerScreen.Laws => LawsRenderer.Render(world, viewState.FocalPolityId, viewState.CurrentLawIndex, viewState.IsSimulationRunning, viewState.IsLawActionMenuOpen, viewState.CurrentLawActionIndex, viewport),
            PlayerScreen.Regions => RegionRenderer.Render(world, viewState.FocalPolityId, viewState.CurrentRegionIndex, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning, viewport),
            PlayerScreen.KnownPolities => KnownPolitiesRenderer.Render(world, viewState.FocalPolityId, viewState.CurrentKnownPolityIndex, discoveryCatalog, advancementCatalog, viewState.IsSimulationRunning, viewport),
            PlayerScreen.KnownSpecies => KnownSpeciesRenderer.Render(world, viewState.FocalPolityId, faunaCatalog, viewState.CurrentKnownSpeciesIndex, viewState.IsSimulationRunning, viewport),
            _ => throw new NotSupportedException($"Unsupported screen {viewState.CurrentScreen}.")
        };
    }
}
