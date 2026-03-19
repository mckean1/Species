using Species.Domain.Catalogs;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class SimulationEngine
{
    private readonly FloraSpeciesCatalog floraCatalog;
    private readonly FaunaSpeciesCatalog faunaCatalog;
    private readonly FloraSimulationSystem floraSimulationSystem;
    private readonly FaunaSimulationSystem faunaSimulationSystem;

    public SimulationEngine(
        World initialWorld,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        CurrentWorld = initialWorld;
        this.floraCatalog = floraCatalog;
        this.faunaCatalog = faunaCatalog;
        floraSimulationSystem = new FloraSimulationSystem();
        faunaSimulationSystem = new FaunaSimulationSystem();
    }

    public World CurrentWorld { get; private set; }

    public SimulationTickResult Tick()
    {
        var advancedWorld = AdvanceMonth(CurrentWorld);
        var floraResult = floraSimulationSystem.Run(advancedWorld, floraCatalog);
        var faunaResult = faunaSimulationSystem.Run(floraResult.World, floraCatalog, faunaCatalog);
        var finalizedWorld = FinalizeTick(faunaResult.World);

        CurrentWorld = finalizedWorld;
        return new SimulationTickResult(finalizedWorld, floraResult.Changes, faunaResult.Changes);
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
