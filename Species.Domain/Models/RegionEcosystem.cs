namespace Species.Domain.Models;

public sealed class RegionEcosystem
{
    public RegionEcosystem(
        IReadOnlyDictionary<string, int>? floraPopulations = null,
        IReadOnlyDictionary<string, int>? faunaPopulations = null)
    {
        FloraPopulations = floraPopulations ?? new Dictionary<string, int>(StringComparer.Ordinal);
        FaunaPopulations = faunaPopulations ?? new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, int> FloraPopulations { get; }

    public IReadOnlyDictionary<string, int> FaunaPopulations { get; }
}
