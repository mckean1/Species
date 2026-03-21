using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class SimulationTickResult
{
    public SimulationTickResult(
        World world,
        IReadOnlyList<FloraPopulationChange> floraChanges,
        IReadOnlyList<FaunaPopulationChange> faunaChanges,
        IReadOnlyList<ProtoPressureChange> protoPressureChanges,
        IReadOnlyList<GroupPressureChange> groupPressureChanges,
        IReadOnlyList<GroupSurvivalChange> groupSurvivalChanges,
        IReadOnlyList<MigrationChange> migrationChanges,
        IReadOnlyList<SettlementChange> settlementChanges,
        IReadOnlyList<MaterialEconomyChange> materialEconomyChanges,
        IReadOnlyList<BiologicalHistoryChange> biologicalHistoryChanges,
        IReadOnlyList<DiscoveryChange> discoveryChanges,
        IReadOnlyList<AdvancementChange> advancementChanges,
        IReadOnlyList<SocialIdentityChange> socialIdentityChanges,
        IReadOnlyList<InterPolityChange> interPolityChanges,
        IReadOnlyList<PoliticalScaleChange> politicalScaleChanges,
        IReadOnlyList<LawProposalChange> lawProposalChanges,
        IReadOnlyList<ChronicleEntry> recordedChronicleEntries,
        IReadOnlyList<ChronicleEntry> revealedChronicleEntries)
    {
        World = world;
        FloraChanges = floraChanges;
        FaunaChanges = faunaChanges;
        ProtoPressureChanges = protoPressureChanges;
        GroupPressureChanges = groupPressureChanges;
        GroupSurvivalChanges = groupSurvivalChanges;
        MigrationChanges = migrationChanges;
        SettlementChanges = settlementChanges;
        MaterialEconomyChanges = materialEconomyChanges;
        BiologicalHistoryChanges = biologicalHistoryChanges;
        DiscoveryChanges = discoveryChanges;
        AdvancementChanges = advancementChanges;
        SocialIdentityChanges = socialIdentityChanges;
        InterPolityChanges = interPolityChanges;
        PoliticalScaleChanges = politicalScaleChanges;
        LawProposalChanges = lawProposalChanges;
        RecordedChronicleEntries = recordedChronicleEntries;
        RevealedChronicleEntries = revealedChronicleEntries;
    }

    public World World { get; }

    public IReadOnlyList<FloraPopulationChange> FloraChanges { get; }

    public IReadOnlyList<FaunaPopulationChange> FaunaChanges { get; }

    public IReadOnlyList<ProtoPressureChange> ProtoPressureChanges { get; }

    public IReadOnlyList<GroupPressureChange> GroupPressureChanges { get; }

    public IReadOnlyList<GroupSurvivalChange> GroupSurvivalChanges { get; }

    public IReadOnlyList<MigrationChange> MigrationChanges { get; }

    public IReadOnlyList<SettlementChange> SettlementChanges { get; }

    public IReadOnlyList<MaterialEconomyChange> MaterialEconomyChanges { get; }

    public IReadOnlyList<BiologicalHistoryChange> BiologicalHistoryChanges { get; }

    public IReadOnlyList<DiscoveryChange> DiscoveryChanges { get; }

    public IReadOnlyList<AdvancementChange> AdvancementChanges { get; }

    public IReadOnlyList<SocialIdentityChange> SocialIdentityChanges { get; }

    public IReadOnlyList<InterPolityChange> InterPolityChanges { get; }

    public IReadOnlyList<PoliticalScaleChange> PoliticalScaleChanges { get; }

    public IReadOnlyList<LawProposalChange> LawProposalChanges { get; }

    public IReadOnlyList<ChronicleEntry> RecordedChronicleEntries { get; }

    public IReadOnlyList<ChronicleEntry> RevealedChronicleEntries { get; }
}
