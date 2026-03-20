using Species.Domain.Catalogs;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class SimulationEngine
{
    private readonly FloraSpeciesCatalog floraCatalog;
    private readonly FaunaSpeciesCatalog faunaCatalog;
    private readonly DiscoveryCatalog discoveryCatalog;
    private readonly AdvancementCatalog advancementCatalog;
    private readonly FloraSimulationSystem floraSimulationSystem;
    private readonly FaunaSimulationSystem faunaSimulationSystem;
    private readonly PressureCalculationSystem pressureCalculationSystem;
    private readonly GroupSurvivalSystem groupSurvivalSystem;
    private readonly MigrationSystem migrationSystem;
    private readonly DiscoverySystem discoverySystem;
    private readonly AdvancementSystem advancementSystem;
    private readonly SettlementSystem settlementSystem;
    private readonly PoliticalBlocSystem politicalBlocSystem;
    private readonly LawProposalSystem lawProposalSystem;
    private readonly EnactedLawSystem enactedLawSystem;
    private readonly ChronicleSystem chronicleSystem;

    public SimulationEngine(
        World initialWorld,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        CurrentWorld = initialWorld;
        this.floraCatalog = floraCatalog;
        this.faunaCatalog = faunaCatalog;
        this.discoveryCatalog = discoveryCatalog;
        this.advancementCatalog = advancementCatalog;
        floraSimulationSystem = new FloraSimulationSystem();
        faunaSimulationSystem = new FaunaSimulationSystem();
        pressureCalculationSystem = new PressureCalculationSystem();
        groupSurvivalSystem = new GroupSurvivalSystem();
        migrationSystem = new MigrationSystem();
        discoverySystem = new DiscoverySystem();
        advancementSystem = new AdvancementSystem();
        settlementSystem = new SettlementSystem();
        politicalBlocSystem = new PoliticalBlocSystem();
        lawProposalSystem = new LawProposalSystem();
        enactedLawSystem = new EnactedLawSystem();
        chronicleSystem = new ChronicleSystem();
    }

    public World CurrentWorld { get; private set; }

    public string PlayerPolityId
    {
        get => CurrentWorld.FocalPolityId;
        set => CurrentWorld = new World(
            CurrentWorld.Seed,
            CurrentWorld.CurrentYear,
            CurrentWorld.CurrentMonth,
            CurrentWorld.Regions,
            CurrentWorld.PopulationGroups,
            CurrentWorld.Chronicle,
            CurrentWorld.Polities,
            value);
    }

    public SimulationTickResult Tick()
    {
        var advancedWorld = AdvanceMonth(CurrentWorld);
        var floraResult = floraSimulationSystem.Run(advancedWorld, floraCatalog);
        var faunaResult = faunaSimulationSystem.Run(floraResult.World, floraCatalog, faunaCatalog);
        var pressureResult = pressureCalculationSystem.Run(faunaResult.World, discoveryCatalog, floraCatalog, faunaCatalog);
        var enactedLawWorld = enactedLawSystem.Run(pressureResult.World);
        var survivalResult = groupSurvivalSystem.Run(enactedLawWorld, floraCatalog, faunaCatalog, advancementCatalog);
        var migrationResult = migrationSystem.Run(survivalResult.World, discoveryCatalog, floraCatalog, faunaCatalog, survivalResult.Changes);
        var settlementResult = settlementSystem.Run(migrationResult.World);
        var discoveryResult = discoverySystem.Run(settlementResult.World, discoveryCatalog, survivalResult.Changes, migrationResult.Changes);
        var advancementResult = advancementSystem.Run(discoveryResult.World, discoveryCatalog, advancementCatalog, survivalResult.Changes, migrationResult.Changes);
        var politicalBlocWorld = politicalBlocSystem.Run(advancementResult.World);
        var lawProposalResult = lawProposalSystem.Run(politicalBlocWorld, PlayerPolityId);
        var chronicleResult = chronicleSystem.Run(lawProposalResult.World, survivalResult.Changes, migrationResult.Changes, discoveryResult.Changes, advancementResult.Changes, lawProposalResult.Changes, settlementResult.Changes);
        var finalizedWorld = FinalizeTick(chronicleResult.World);

        CurrentWorld = finalizedWorld;
        return new SimulationTickResult(finalizedWorld, floraResult.Changes, faunaResult.Changes, pressureResult.Changes, survivalResult.Changes, migrationResult.Changes, settlementResult.Changes, discoveryResult.Changes, advancementResult.Changes, lawProposalResult.Changes, chronicleResult.RecordedEntries, chronicleResult.RevealedEntries);
    }

    public bool PassActiveLawProposal()
    {
        return ResolveActiveLawProposal(LawProposalStatus.Passed);
    }

    public bool VetoActiveLawProposal()
    {
        return ResolveActiveLawProposal(LawProposalStatus.Vetoed);
    }

    private static World AdvanceMonth(World world)
    {
        var nextMonth = world.CurrentMonth == 12 ? 1 : world.CurrentMonth + 1;
        var nextYear = world.CurrentMonth == 12 ? world.CurrentYear + 1 : world.CurrentYear;
        return new World(world.Seed, nextYear, nextMonth, world.Regions, world.PopulationGroups, world.Chronicle, world.Polities, world.FocalPolityId);
    }

    private static World FinalizeTick(World world)
    {
        var memberGroupIdsByPolityId = world.PopulationGroups
            .GroupBy(group => group.PolityId, StringComparer.Ordinal)
            .ToDictionary(
                grouping => grouping.Key,
                grouping => grouping
                    .OrderBy(group => group.Id, StringComparer.Ordinal)
                    .Select(group => group.Id)
                    .ToArray(),
                StringComparer.Ordinal);
        var populationByPolityId = world.PopulationGroups
            .GroupBy(group => group.PolityId, StringComparer.Ordinal)
            .ToDictionary(
                grouping => grouping.Key,
                grouping => grouping.Sum(group => group.Population),
                StringComparer.Ordinal);
        var updatedPolities = world.Polities
            .Where(polity => memberGroupIdsByPolityId.ContainsKey(polity.Id))
            .Select(polity =>
            {
                var updatedPolity = polity.Clone();
                updatedPolity.MemberGroupIds.Clear();
                updatedPolity.MemberGroupIds.AddRange(memberGroupIdsByPolityId[polity.Id]);
                return updatedPolity;
            })
            .ToArray();
        var focalPolityId = updatedPolities.Any(polity => string.Equals(polity.Id, world.FocalPolityId, StringComparison.Ordinal))
            ? world.FocalPolityId
            : updatedPolities
                .OrderByDescending(polity => populationByPolityId.GetValueOrDefault(polity.Id))
                .ThenBy(polity => polity.Name, StringComparer.Ordinal)
                .Select(polity => polity.Id)
                .FirstOrDefault() ?? string.Empty;

        return new World(
            world.Seed,
            world.CurrentYear,
            world.CurrentMonth,
            world.Regions,
            world.PopulationGroups,
            world.Chronicle,
            updatedPolities,
            focalPolityId);
    }

    private bool ResolveActiveLawProposal(LawProposalStatus status)
    {
        var result = lawProposalSystem.ResolvePlayerDecision(CurrentWorld, PlayerPolityId, status);
        if (result.Changes.Count == 0)
        {
            return false;
        }

        var updatedWorld = result.World;
        foreach (var change in result.Changes)
        {
            updatedWorld = chronicleSystem.RecordLawDecision(updatedWorld, change);
        }

        CurrentWorld = updatedWorld;
        return true;
    }
}
