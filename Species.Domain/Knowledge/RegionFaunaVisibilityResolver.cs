using Species.Domain.Catalogs;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Knowledge;

public static class RegionFaunaVisibilityResolver
{
    private const int EncounterVisibleFaunaLimit = 2;
    private const int DiscoveryVisibleFaunaLimit = 3;

    public static RegionFaunaVisibility Resolve(
        Region region,
        RegionKnowledgeSnapshot? snapshot,
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
        var canShowEncounterFauna = snapshot?.FaunaKnowledge >= KnowledgeLevel.Encounter &&
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
                "No fauna shown because the polity lacks encounter-level local fauna awareness for this region.");
        }

        if (snapshot!.FaunaKnowledge >= KnowledgeLevel.Discovery)
        {
            return new RegionFaunaVisibility(
                "Known fauna",
                discoveryEntries.Length > 0 ? discoveryEntries : ["No fauna observed"],
                encounterEntries,
                discoveryEntries,
                discoveryEntries.Length > 0
                    ? "Discovery-level fauna knowledge allows a broader and more reliable local fauna list."
                    : "Discovery-level fauna knowledge is present, but no fauna currently qualifies for display.");
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
            "Encounter-level fauna knowledge shows only the most obvious locally observed fauna and keeps the list partial.");
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
