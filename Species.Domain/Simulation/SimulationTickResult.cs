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
        IReadOnlyList<DiscoveryChange> discoveryChanges,
        IReadOnlyList<AdvancementChange> advancementChanges,
        IReadOnlyList<ChronicleEntry> recordedChronicleEntries,
        IReadOnlyList<ChronicleEntry> revealedChronicleEntries)
    {
        World = world;
        FloraChanges = floraChanges;
        FaunaChanges = faunaChanges;
        GroupPressureChanges = groupPressureChanges;
        GroupSurvivalChanges = groupSurvivalChanges;
        MigrationChanges = migrationChanges;
        DiscoveryChanges = discoveryChanges;
        AdvancementChanges = advancementChanges;
        RecordedChronicleEntries = recordedChronicleEntries;
        RevealedChronicleEntries = revealedChronicleEntries;
    }

    public World World { get; }

    public IReadOnlyList<FloraPopulationChange> FloraChanges { get; }

    public IReadOnlyList<FaunaPopulationChange> FaunaChanges { get; }

    public IReadOnlyList<GroupPressureChange> GroupPressureChanges { get; }

    public IReadOnlyList<GroupSurvivalChange> GroupSurvivalChanges { get; }

    public IReadOnlyList<MigrationChange> MigrationChanges { get; }

    public IReadOnlyList<DiscoveryChange> DiscoveryChanges { get; }

    public IReadOnlyList<AdvancementChange> AdvancementChanges { get; }

    public IReadOnlyList<ChronicleEntry> RecordedChronicleEntries { get; }

    public IReadOnlyList<ChronicleEntry> RevealedChronicleEntries { get; }
}
