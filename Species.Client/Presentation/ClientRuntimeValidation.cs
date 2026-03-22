using Species.Domain.Catalogs;
using Species.Domain.Generation;
using Species.Domain.Models;
using Species.Domain.Simulation;
using Species.Domain.Validation;

namespace Species.Client.Presentation;

public static class ClientRuntimeValidation
{
    public static string[] ValidateStartup(
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        return WorldValidator.Validate(world)
            .Concat(SpeciesDefinitionValidator.Validate(floraCatalog))
            .Concat(SpeciesDefinitionValidator.Validate(faunaCatalog, floraCatalog))
            .Concat(DiscoveryCatalogValidator.Validate(discoveryCatalog))
            .Concat(AdvancementCatalogValidator.Validate(advancementCatalog))
            .Concat(ChronicleValidator.Validate(world))
            .Concat(RegionEcologyValidator.Validate(world, floraCatalog, faunaCatalog))
            .Concat(PolityValidator.Validate(world))
            .Concat(PopulationGroupValidator.Validate(world))
            .ToArray();
    }

    public static string[] ValidatePostTick(
        World previousWorld,
        SimulationEngine simulationEngine,
        SimulationTickResult tickResult,
        PlayerViewState viewState,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        return WorldValidator.Validate(simulationEngine.CurrentWorld)
            .Concat(RegionEcologyValidator.Validate(simulationEngine.CurrentWorld, floraCatalog, faunaCatalog))
            .Concat(PolityValidator.Validate(simulationEngine.CurrentWorld))
            .Concat(PopulationGroupValidator.Validate(simulationEngine.CurrentWorld))
            .Concat(SimulationTickValidator.Validate(previousWorld, tickResult))
            .Concat(GroupSurvivalValidator.Validate(previousWorld, tickResult))
            .Concat(MigrationValidator.Validate(previousWorld, tickResult))
            .Concat(DiscoveryStateValidator.Validate(simulationEngine.CurrentWorld, discoveryCatalog, tickResult))
            .Concat(AdvancementStateValidator.Validate(simulationEngine.CurrentWorld, advancementCatalog, tickResult))
            .Concat(ChronicleValidator.Validate(simulationEngine.CurrentWorld, tickResult))
            .Concat(PlayerViewValidator.Validate(viewState, simulationEngine.CurrentWorld, floraCatalog, faunaCatalog, discoveryCatalog, advancementCatalog))
            .ToArray();
    }
}
