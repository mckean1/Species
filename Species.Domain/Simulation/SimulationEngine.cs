using Species.Domain.Catalogs;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class SimulationEngine
{
    private readonly FloraSpeciesCatalog floraCatalog;
    private readonly FaunaSpeciesCatalog faunaCatalog;
    private readonly DiscoveryCatalog discoveryCatalog;
    private readonly FloraSimulationSystem floraSimulationSystem;
    private readonly FaunaSimulationSystem faunaSimulationSystem;
    private readonly PressureCalculationSystem pressureCalculationSystem;
    private readonly GroupSurvivalSystem groupSurvivalSystem;
    private readonly MigrationSystem migrationSystem;
    private readonly DiscoverySystem discoverySystem;

    public SimulationEngine(
        World initialWorld,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog)
    {
        CurrentWorld = initialWorld;
        this.floraCatalog = floraCatalog;
        this.faunaCatalog = faunaCatalog;
        this.discoveryCatalog = discoveryCatalog;
        floraSimulationSystem = new FloraSimulationSystem();
        faunaSimulationSystem = new FaunaSimulationSystem();
        pressureCalculationSystem = new PressureCalculationSystem();
        groupSurvivalSystem = new GroupSurvivalSystem();
        migrationSystem = new MigrationSystem();
        discoverySystem = new DiscoverySystem();
    }

    public World CurrentWorld { get; private set; }

    public SimulationTickResult Tick()
    {
        var advancedWorld = AdvanceMonth(CurrentWorld);
        var floraResult = floraSimulationSystem.Run(advancedWorld, floraCatalog);
        var faunaResult = faunaSimulationSystem.Run(floraResult.World, floraCatalog, faunaCatalog);
        var pressureResult = pressureCalculationSystem.Run(faunaResult.World, faunaCatalog);
        var survivalResult = groupSurvivalSystem.Run(pressureResult.World, floraCatalog, faunaCatalog);
        var migrationResult = migrationSystem.Run(survivalResult.World, discoveryCatalog, faunaCatalog, survivalResult.Changes);
        var discoveryResult = discoverySystem.Run(migrationResult.World, discoveryCatalog, survivalResult.Changes, migrationResult.Changes);
        var finalizedWorld = FinalizeTick(discoveryResult.World);

        CurrentWorld = finalizedWorld;
        return new SimulationTickResult(finalizedWorld, floraResult.Changes, faunaResult.Changes, pressureResult.Changes, survivalResult.Changes, migrationResult.Changes, discoveryResult.Changes);
    }

    private static World AdvanceMonth(World world)
    {
        var nextMonth = world.CurrentMonth == 12 ? 1 : world.CurrentMonth + 1;
        var nextYear = world.CurrentMonth == 12 ? world.CurrentYear + 1 : world.CurrentYear;
        return new World(world.Seed, nextYear, nextMonth, world.Regions, world.PopulationGroups);
    }

    private static World FinalizeTick(World world)
    {
        return world;
    }
}
