using Species.Domain.Catalogs;
using Species.Domain.Simulation;
using Species.Client.Enums;
using Species.Client.ViewModelFactories;

namespace Species.Client.Presentation;

public static class PlayerViewStateCoordinator
{
    public static void Synchronize(
        PlayerViewState viewState,
        SimulationEngine simulationEngine,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var world = simulationEngine.CurrentWorld;
        viewState.EnsureFocalPolity(world);
        simulationEngine.PlayerPolityId = viewState.FocalPolityId;

        viewState.ClampRegionIndex(RegionsViewModelFactory.Build(
            world,
            viewState.FocalPolityId,
            viewState.CurrentRegionIndex,
            floraCatalog,
            faunaCatalog,
            discoveryCatalog).Regions.Count);

        viewState.ClampKnownPolityIndex(KnownPolitiesViewModelFactory.Build(
            world,
            viewState.FocalPolityId,
            viewState.CurrentKnownPolityIndex,
            discoveryCatalog,
            advancementCatalog).Polities.Count);

        viewState.ClampAdvancementIndex(AdvancementViewModelFactory.Build(
            world,
            viewState.FocalPolityId,
            discoveryCatalog,
            advancementCatalog,
            viewState.CurrentAdvancementIndex).Items.Count);

        var lawsViewModel = LawsViewModelFactory.Build(world, viewState.FocalPolityId, viewState.CurrentLawIndex);
        viewState.ClampLawIndex(lawsViewModel.Laws.Count);
        if (viewState.CurrentScreen != PlayerScreen.Laws || !lawsViewModel.HasSelectedPendingDecision)
        {
            viewState.CloseLawActionMenu();
        }

        viewState.ClampKnownSpeciesIndex(KnownSpeciesViewModelFactory.Build(
            world,
            faunaCatalog,
            viewState.FocalPolityId,
            viewState.CurrentKnownSpeciesIndex).Species.Count);

        var chronicleViewModel = ChronicleViewModelFactory.Build(world, viewState.FocalPolityId, viewState);
        viewState.ClampChronicleSelection(chronicleViewModel.UrgentItems.Count, chronicleViewModel.Entries.Count);
    }
}
