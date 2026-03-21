using Species.Domain.Catalogs;
using Species.Domain.Knowledge;
using Species.Domain.Models;

namespace Species.Domain.Diagnostics;

public static class FaunaVisibilityDiagnosticsFormatter
{
    public static string FormatRegion(
        World world,
        string polityId,
        string regionId,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var polity = world.Polities.FirstOrDefault(item => string.Equals(item.Id, polityId, StringComparison.Ordinal));
        var group = world.PopulationGroups.FirstOrDefault(item => string.Equals(item.PolityId, polityId, StringComparison.Ordinal));
        var region = world.Regions.FirstOrDefault(item => string.Equals(item.Id, regionId, StringComparison.Ordinal));

        if (polity is null || group is null || region is null)
        {
            return $"Fauna visibility diagnostic unavailable for polity={polityId}, region={regionId}.";
        }

        var knowledgeContext = GroupKnowledgeContext.Create(world, group, discoveryCatalog, floraCatalog, faunaCatalog);
        var snapshot = knowledgeContext.ObserveRegion(region, group.CurrentRegionId);
        var visibility = RegionFaunaVisibilityResolver.Resolve(region, snapshot, faunaCatalog);
        var localFaunaDiscoveryId = discoveryCatalog.GetLocalFaunaDiscoveryId(region.Id);
        var simulatedFauna = region.Ecosystem.FaunaPopulations.Count == 0
            ? "none"
            : string.Join(", ", region.Ecosystem.FaunaPopulations
                .Where(entry => entry.Value > 0)
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => $"{faunaCatalog.GetById(entry.Key)?.Name ?? entry.Key}:{entry.Value}"));

        var knowledgeReason = group.KnownDiscoveryIds.Contains(localFaunaDiscoveryId)
            ? "Local fauna discovery is known."
            : snapshot.IsCurrentRegion
                ? "Current regional presence grants encounter-level fauna awareness."
                : snapshot.HuntingEvidenceMonths > 0
                    ? $"Hunting exposure months in region: {snapshot.HuntingEvidenceMonths}."
                    : snapshot.MonthsSpent > 0
                        ? $"Residence months in region: {snapshot.MonthsSpent}."
                        : snapshot.IsKnownRegion
                            ? "The region is known to the polity, so fauna remains only encountered."
                            : "No meaningful fauna observation is recorded.";

        var lines = new List<string>
        {
            $"Region={region.Id} ({region.Name})",
            $"SimulatedFauna=[{simulatedFauna}]",
            $"FaunaKnowledge={snapshot.FaunaKnowledge}",
            $"KnowledgeReason={knowledgeReason}",
            $"EncounterDisplay=[{string.Join(", ", visibility.EncounterEligibleEntries.DefaultIfEmpty("none"))}]",
            $"DiscoveryDisplay=[{string.Join(", ", visibility.DiscoveryEligibleEntries.DefaultIfEmpty("none"))}]",
            $"ActiveDisplayLabel={visibility.DisplayLabel}",
            $"ActiveDisplay=[{string.Join(", ", visibility.VisibleEntries.DefaultIfEmpty("none"))}]",
            $"VisibilityReason={visibility.Reason}"
        };

        if (visibility.VisibleEntries.Count == 1 && string.Equals(visibility.VisibleEntries[0], "No fauna observed", StringComparison.Ordinal))
        {
            lines.Add("FallbackReason=No fauna is shown because encounter-level regional fauna awareness is not yet justified.");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
