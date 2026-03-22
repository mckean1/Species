using Species.Domain.Catalogs;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Discovery;

public static class RegionFaunaVisibilityResolver
{
    private const int EncounterVisibleFaunaLimit = 2;
    private const int DiscoveryVisibleFaunaLimit = 3;

    public static RegionFaunaVisibility Resolve(
        Region region,
        RegionDiscoverySnapshot? snapshot,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var simulatedFauna = region.Ecosystem.FaunaPopulations
            .Where(entry => entry.Value > 0)
            .Select(entry =>
            {
                var definition = faunaCatalog.GetById(entry.Key);
                return new VisibleFaunaCandidate(
                    entry.Key,
                    definition?.Name ?? entry.Key,
                    entry.Value,
                    definition?.Conspicuousness ?? 0.35f,
                    definition?.DietCategory == DietCategory.Carnivore);
            })
            .OrderByDescending(candidate => candidate.Population)
            .ThenBy(candidate => candidate.Name, StringComparer.Ordinal)
            .ToArray();

        var hasMeaningfulObservation = snapshot is not null &&
                                       (snapshot.IsCurrentRegion ||
                                        snapshot.IsKnownRegion ||
                                        snapshot.MonthsSpent > 0 ||
                                        snapshot.HuntingEvidenceMonths > 0 ||
                                        snapshot.WaterEvidenceMonths > 0);
        var canShowEncounterFauna = snapshot?.FaunaStage >= DiscoveryStage.Encountered &&
                                    hasMeaningfulObservation &&
                                    simulatedFauna.Length > 0;
        var encounterEntries = simulatedFauna
            .OrderByDescending(candidate => ResolveEncounterPriority(candidate))
            .ThenByDescending(candidate => candidate.Population)
            .ThenBy(candidate => candidate.Name, StringComparer.Ordinal)
            .Take(EncounterVisibleFaunaLimit)
            .Select(candidate => candidate.Name)
            .ToArray();
        var discoveryEntries = simulatedFauna
            .Take(DiscoveryVisibleFaunaLimit)
            .Select(candidate => candidate.Name)
            .ToArray();

        if (!canShowEncounterFauna)
        {
            return new RegionFaunaVisibility(
                "Fauna",
                ["No fauna observed"],
                encounterEntries,
                discoveryEntries,
                "No fauna shown because the polity has not yet encountered local fauna in this region.");
        }

        if (snapshot!.FaunaStage == DiscoveryStage.Discovered)
        {
            return new RegionFaunaVisibility(
                "Discovered fauna",
                discoveryEntries.Length > 0 ? discoveryEntries : ["No fauna observed"],
                encounterEntries,
                discoveryEntries,
                discoveryEntries.Length > 0
                    ? "Discovered fauna can be listed more broadly and reliably."
                    : "Local fauna is discovered here, but no fauna currently qualifies for display.");
        }

        var visibleEntries = encounterEntries.ToList();
        if (simulatedFauna.Length > encounterEntries.Length)
        {
            visibleEntries.Add("Other fauna signs");
        }

        return new RegionFaunaVisibility(
            "Encountered fauna",
            visibleEntries,
            encounterEntries,
            discoveryEntries,
            "Encountered fauna stays partial until the polity fully discovers the local fauna.");
    }

    private static float ResolveEncounterPriority(VisibleFaunaCandidate candidate)
    {
        var abundanceSignal = MathF.Log10(candidate.Population + 1.0f);
        var conspicuousness = Math.Clamp(candidate.Conspicuousness, 0.20f, 1.00f);
        var carnivoreBonus = candidate.IsCarnivore ? 0.30f : 0.0f;
        return abundanceSignal * conspicuousness + carnivoreBonus;
    }

    public sealed record RegionFaunaVisibility(
        string DisplayLabel,
        IReadOnlyList<string> VisibleEntries,
        IReadOnlyList<string> EncounterEligibleEntries,
        IReadOnlyList<string> DiscoveryEligibleEntries,
        string Reason);

    private sealed record VisibleFaunaCandidate(
        string SpeciesId,
        string Name,
        int Population,
        float Conspicuousness,
        bool IsCarnivore);
}
