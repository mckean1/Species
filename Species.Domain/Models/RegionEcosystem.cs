namespace Species.Domain.Models;

public sealed class RegionEcosystem
{
    public RegionEcosystem(
        ProtoLifeSubstrate? protoLifeSubstrate = null,
        IReadOnlyDictionary<string, int>? floraPopulations = null,
        IReadOnlyDictionary<string, int>? faunaPopulations = null,
        IReadOnlyDictionary<string, RegionalBiologicalProfile>? floraProfiles = null,
        IReadOnlyDictionary<string, RegionalBiologicalProfile>? faunaProfiles = null,
        IReadOnlyList<FossilRecord>? fossilRecords = null,
        IReadOnlyList<BiologicalHistoryRecord>? biologicalHistoryRecords = null,
        PrimitiveLifeSubstrate? primitiveLifeSubstrate = null)
    {
        ProtoLifeSubstrate = protoLifeSubstrate?.Clone() ?? new ProtoLifeSubstrate();
        PrimitiveLifeSubstrate = primitiveLifeSubstrate?.Clone() ?? new PrimitiveLifeSubstrate();
        FloraPopulations = floraPopulations ?? new Dictionary<string, int>(StringComparer.Ordinal);
        FaunaPopulations = faunaPopulations ?? new Dictionary<string, int>(StringComparer.Ordinal);
        FloraProfiles = floraProfiles ?? new Dictionary<string, RegionalBiologicalProfile>(StringComparer.Ordinal);
        FaunaProfiles = faunaProfiles ?? new Dictionary<string, RegionalBiologicalProfile>(StringComparer.Ordinal);
        FossilRecords = fossilRecords ?? Array.Empty<FossilRecord>();
        BiologicalHistoryRecords = biologicalHistoryRecords ?? Array.Empty<BiologicalHistoryRecord>();
    }

    /// <summary>
    /// Active simulation substrate - tracks ongoing ecological pressure, vacancy, and genesis conditions.
    /// Evolves during gameplay via ProtoPressureSystem and BiologicalEvolutionSystem.
    /// Capacity values initialized from PrimitiveLifeSubstrate after WG-3.
    /// </summary>
    public ProtoLifeSubstrate ProtoLifeSubstrate { get; }

    /// <summary>
    /// Canonical primitive-world snapshot - captures the initial post-WG-3 state.
    /// Represents capacity and strength immediately after primitive life seeding.
    /// Serves as a historical baseline reference.
    /// </summary>
    public PrimitiveLifeSubstrate PrimitiveLifeSubstrate { get; }

    public IReadOnlyDictionary<string, int> FloraPopulations { get; }

    public IReadOnlyDictionary<string, int> FaunaPopulations { get; }

    public IReadOnlyDictionary<string, RegionalBiologicalProfile> FloraProfiles { get; }

    public IReadOnlyDictionary<string, RegionalBiologicalProfile> FaunaProfiles { get; }

    public IReadOnlyList<FossilRecord> FossilRecords { get; }

    public IReadOnlyList<BiologicalHistoryRecord> BiologicalHistoryRecords { get; }
}
