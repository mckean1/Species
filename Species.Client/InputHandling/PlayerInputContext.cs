using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Domain.Simulation;
using Species.Client.Presentation;

namespace Species.Client.InputHandling;

public sealed class PlayerInputContext
{
    public PlayerInputContext(
        SimulationEngine simulationEngine,
        PlayerViewState viewState,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog,
        Func<bool> advanceOneMonth)
    {
        SimulationEngine = simulationEngine;
        ViewState = viewState;
        FloraCatalog = floraCatalog;
        FaunaCatalog = faunaCatalog;
        DiscoveryCatalog = discoveryCatalog;
        AdvancementCatalog = advancementCatalog;
        AdvanceOneMonth = advanceOneMonth;
    }

    public SimulationEngine SimulationEngine { get; }

    public World CurrentWorld => SimulationEngine.CurrentWorld;

    public PlayerViewState ViewState { get; }

    public FloraSpeciesCatalog FloraCatalog { get; }

    public FaunaSpeciesCatalog FaunaCatalog { get; }

    public DiscoveryCatalog DiscoveryCatalog { get; }

    public AdvancementCatalog AdvancementCatalog { get; }

    public Func<bool> AdvanceOneMonth { get; }
}
