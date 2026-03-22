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

        viewState.ClampRegionIndex(RegionsViewModelFactory.GetKnownRegionCount(world, viewState.FocalPolityId));

        viewState.ClampKnownPolityIndex(KnownPolitiesViewModelFactory.GetKnownPolityCount(world, viewState.FocalPolityId));

        viewState.ClampAdvancementIndex(AdvancementViewModelFactory.GetAdvancementCount(advancementCatalog));

        var lawSelection = LawsViewModelFactory.QuerySelectionInfo(world, viewState.FocalPolityId, viewState.CurrentLawIndex);
        viewState.ClampLawIndex(lawSelection.LawCount);
        if (viewState.CurrentScreen != PlayerScreen.Laws || !lawSelection.HasSelectedPendingDecision)
        {
            viewState.CloseLawActionMenu();
        }

        viewState.ClampKnownSpeciesIndex(KnownSpeciesViewModelFactory.GetKnownSpeciesCount(world, faunaCatalog, viewState.FocalPolityId));

        var chronicleSelection = ChronicleViewModelFactory.QuerySelectionInfo(world, viewState.FocalPolityId, viewState.CreateChronicleViewRequest());
        viewState.ClampChronicleSelection(chronicleSelection.UrgentCount, chronicleSelection.EntryCount);
    }
}
