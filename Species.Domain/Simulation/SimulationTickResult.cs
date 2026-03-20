using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class SimulationTickResult
{
    public SimulationTickResult(
        World world,
        IReadOnlyList<FloraPopulationChange> floraChanges,
        IReadOnlyList<FaunaPopulationChange> faunaChanges,
        IReadOnlyList<GroupPressureChange> groupPressureChanges,
        IReadOnlyList<GroupSurvivalChange> groupSurvivalChanges,
        IReadOnlyList<MigrationChange> migrationChanges,
        IReadOnlyList<SettlementChange> settlementChanges,
        IReadOnlyList<DiscoveryChange> discoveryChanges,
        IReadOnlyList<AdvancementChange> advancementChanges,
        IReadOnlyList<LawProposalChange> lawProposalChanges,
        IReadOnlyList<ChronicleEntry> recordedChronicleEntries,
        IReadOnlyList<ChronicleEntry> revealedChronicleEntries)
    {
        World = world;
        FloraChanges = floraChanges;
        FaunaChanges = faunaChanges;
        GroupPressureChanges = groupPressureChanges;
        GroupSurvivalChanges = groupSurvivalChanges;
        MigrationChanges = migrationChanges;
        SettlementChanges = settlementChanges;
        DiscoveryChanges = discoveryChanges;
        AdvancementChanges = advancementChanges;
        LawProposalChanges = lawProposalChanges;
        RecordedChronicleEntries = recordedChronicleEntries;
        RevealedChronicleEntries = revealedChronicleEntries;
    }

    public World World { get; }

    public IReadOnlyList<FloraPopulationChange> FloraChanges { get; }

    public IReadOnlyList<FaunaPopulationChange> FaunaChanges { get; }

    public IReadOnlyList<GroupPressureChange> GroupPressureChanges { get; }

    public IReadOnlyList<GroupSurvivalChange> GroupSurvivalChanges { get; }

    public IReadOnlyList<MigrationChange> MigrationChanges { get; }

    public IReadOnlyList<SettlementChange> SettlementChanges { get; }

    public IReadOnlyList<DiscoveryChange> DiscoveryChanges { get; }

    public IReadOnlyList<AdvancementChange> AdvancementChanges { get; }

    public IReadOnlyList<LawProposalChange> LawProposalChanges { get; }

    public IReadOnlyList<ChronicleEntry> RecordedChronicleEntries { get; }

    public IReadOnlyList<ChronicleEntry> RevealedChronicleEntries { get; }
}
