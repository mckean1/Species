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
                AdvancementViewModelFactory.Build(world, viewState.FocalPolityId, discoveryCatalog, advancementCatalog, viewState.CurrentAdvancementIndex, viewState.IsSimulationRunning),
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
                KnownSpeciesViewModelFactory.Build(world, faunaCatalog, viewState.FocalPolityId, viewState.CurrentKnownSpeciesIndex, viewState.IsSimulationRunning),
                viewport),
            _ => throw new NotSupportedException($"Unsupported screen {viewState.CurrentScreen}.")
        };
    }
}
