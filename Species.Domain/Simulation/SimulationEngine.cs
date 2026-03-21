using Species.Domain.Catalogs;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class SimulationEngine
{
    private readonly FloraSpeciesCatalog floraCatalog;
    private readonly FaunaSpeciesCatalog faunaCatalog;
    private readonly SapientSpeciesCatalog sapientCatalog;
    private readonly DiscoveryCatalog discoveryCatalog;
    private readonly AdvancementCatalog advancementCatalog;
    private readonly FloraSimulationSystem floraSimulationSystem;
    private readonly FaunaSimulationSystem faunaSimulationSystem;
    private readonly BiologicalEvolutionSystem biologicalEvolutionSystem;
    private readonly ProtoPressureSystem protoPressureSystem;
    private readonly PressureCalculationSystem pressureCalculationSystem;
    private readonly GroupSurvivalSystem groupSurvivalSystem;
    private readonly MigrationSystem migrationSystem;
    private readonly DiscoverySystem discoverySystem;
    private readonly AdvancementSystem advancementSystem;
    private readonly SocialIdentitySystem socialIdentitySystem;
    private readonly InterPolityInteractionSystem interPolityInteractionSystem;
    private readonly PoliticalScalingSystem politicalScalingSystem;
    private readonly SettlementSystem settlementSystem;
    private readonly MaterialEconomySystem materialEconomySystem;
    private readonly PoliticalBlocSystem politicalBlocSystem;
    private readonly LawProposalSystem lawProposalSystem;
    private readonly EnactedLawSystem enactedLawSystem;
    private readonly ChronicleSystem chronicleSystem;

    public SimulationEngine(
        World initialWorld,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        SapientSpeciesCatalog sapientCatalog,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        CurrentWorld = initialWorld;
        this.floraCatalog = floraCatalog;
        this.faunaCatalog = faunaCatalog;
        this.sapientCatalog = sapientCatalog;
        this.discoveryCatalog = discoveryCatalog;
        this.advancementCatalog = advancementCatalog;
        floraSimulationSystem = new FloraSimulationSystem();
        faunaSimulationSystem = new FaunaSimulationSystem();
        biologicalEvolutionSystem = new BiologicalEvolutionSystem();
        protoPressureSystem = new ProtoPressureSystem();
        pressureCalculationSystem = new PressureCalculationSystem();
        groupSurvivalSystem = new GroupSurvivalSystem();
        migrationSystem = new MigrationSystem();
        discoverySystem = new DiscoverySystem();
        advancementSystem = new AdvancementSystem();
        socialIdentitySystem = new SocialIdentitySystem();
        interPolityInteractionSystem = new InterPolityInteractionSystem();
        politicalScalingSystem = new PoliticalScalingSystem();
        settlementSystem = new SettlementSystem();
        materialEconomySystem = new MaterialEconomySystem();
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
        var biologicalEvolutionResult = biologicalEvolutionSystem.Run(faunaResult.World, floraCatalog, faunaCatalog, sapientCatalog, floraResult.Changes, faunaResult.Changes);
        var protoPressureResult = protoPressureSystem.Run(biologicalEvolutionResult.World);
        var pressureResult = pressureCalculationSystem.Run(protoPressureResult.World, discoveryCatalog, floraCatalog, faunaCatalog);
        var enactedLawWorld = enactedLawSystem.Run(pressureResult.World);
        var survivalResult = groupSurvivalSystem.Run(enactedLawWorld, floraCatalog, faunaCatalog, advancementCatalog);
        var migrationResult = migrationSystem.Run(survivalResult.World, discoveryCatalog, floraCatalog, faunaCatalog, survivalResult.Changes);
        var settlementResult = settlementSystem.Run(migrationResult.World);
        var materialEconomyResult = materialEconomySystem.Run(settlementResult.World);
        var discoveryResult = discoverySystem.Run(materialEconomyResult.World, discoveryCatalog, floraCatalog, faunaCatalog, survivalResult.Changes, migrationResult.Changes);
        var advancementResult = advancementSystem.Run(discoveryResult.World, discoveryCatalog, advancementCatalog, survivalResult.Changes, migrationResult.Changes);
        var socialIdentityResult = socialIdentitySystem.Run(advancementResult.World);
        var interPolityResult = interPolityInteractionSystem.Run(socialIdentityResult.World);
        var politicalScaleResult = politicalScalingSystem.Run(interPolityResult.World);
        var politicalBlocWorld = politicalBlocSystem.Run(politicalScaleResult.World);
        var lawProposalResult = lawProposalSystem.Run(politicalBlocWorld, PlayerPolityId);
        var chronicleResult = chronicleSystem.Run(lawProposalResult.World, pressureResult.Changes, survivalResult.Changes, migrationResult.Changes, biologicalEvolutionResult.Changes, discoveryResult.Changes, advancementResult.Changes, socialIdentityResult.Changes, interPolityResult.Changes, politicalScaleResult.Changes, lawProposalResult.Changes, settlementResult.Changes, materialEconomyResult.Changes);
        var finalizedWorld = FinalizeTick(chronicleResult.World);

        CurrentWorld = finalizedWorld;
        return new SimulationTickResult(finalizedWorld, floraResult.Changes, faunaResult.Changes, protoPressureResult.Changes, pressureResult.Changes, survivalResult.Changes, migrationResult.Changes, settlementResult.Changes, materialEconomyResult.Changes, biologicalEvolutionResult.Changes, discoveryResult.Changes, advancementResult.Changes, socialIdentityResult.Changes, interPolityResult.Changes, politicalScaleResult.Changes, lawProposalResult.Changes, chronicleResult.RecordedEntries, chronicleResult.RevealedEntries);
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
