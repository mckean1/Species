namespace Species.Domain.Models;

public sealed class RegionEcosystem
{
    public RegionEcosystem(
        IReadOnlyDictionary<string, int>? floraPopulations = null,
        IReadOnlyDictionary<string, int>? faunaPopulations = null,
        IReadOnlyDictionary<string, RegionalBiologicalProfile>? floraProfiles = null,
        IReadOnlyDictionary<string, RegionalBiologicalProfile>? faunaProfiles = null,
        IReadOnlyList<FossilRecord>? fossilRecords = null,
        IReadOnlyList<BiologicalHistoryRecord>? biologicalHistoryRecords = null)
    {
        FloraPopulations = floraPopulations ?? new Dictionary<string, int>(StringComparer.Ordinal);
        FaunaPopulations = faunaPopulations ?? new Dictionary<string, int>(StringComparer.Ordinal);
        FloraProfiles = floraProfiles ?? new Dictionary<string, RegionalBiologicalProfile>(StringComparer.Ordinal);
        FaunaProfiles = faunaProfiles ?? new Dictionary<string, RegionalBiologicalProfile>(StringComparer.Ordinal);
        FossilRecords = fossilRecords ?? Array.Empty<FossilRecord>();
        BiologicalHistoryRecords = biologicalHistoryRecords ?? Array.Empty<BiologicalHistoryRecord>();
    }

    public IReadOnlyDictionary<string, int> FloraPopulations { get; }

    public IReadOnlyDictionary<string, int> FaunaPopulations { get; }

    public IReadOnlyDictionary<string, RegionalBiologicalProfile> FloraProfiles { get; }

    public IReadOnlyDictionary<string, RegionalBiologicalProfile> FaunaProfiles { get; }

    public IReadOnlyList<FossilRecord> FossilRecords { get; }

    public IReadOnlyList<BiologicalHistoryRecord> BiologicalHistoryRecords { get; }
}
