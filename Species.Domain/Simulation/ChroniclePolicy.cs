using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

// Central Chronicle routing/filter layer.
// Systems should emit structured candidates; policy decides Chronicle vs alert vs diagnostics vs suppression.
public sealed class ChroniclePolicy
{
    private const int MemoryRetentionMonths = 24;

    public ChroniclePolicyResult Evaluate(World world, IReadOnlyList<ChronicleEventCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return new ChroniclePolicyResult(Array.Empty<ChronicleEntry>(), Array.Empty<ChronicleAlert>(), Array.Empty<ChronicleEventCandidate>(), Array.Empty<ChronicleEventCandidate>());
        }

        var chronicleCandidates = new List<ChronicleEventCandidate>();
        var alertCandidates = new List<ChronicleEventCandidate>();
        var diagnostics = new List<ChronicleEventCandidate>();
        var suppressed = new List<ChronicleEventCandidate>();
        var seenKeysThisTick = new HashSet<string>(StringComparer.Ordinal);
        var eventMemory = world.Chronicle.EventMemory
            .Where(memory => MonthsBetween(memory.EventYear, memory.EventMonth, world.CurrentYear, world.CurrentMonth) <= MemoryRetentionMonths)
            .ToArray();

        foreach (var candidate in candidates)
        {
            var target = ResolveOutputTarget(candidate);
            if (target is ChronicleOutputTarget.Chronicle or ChronicleOutputTarget.Alert &&
                !ChronicleFormatter.CanFormat(candidate))
            {
                diagnostics.Add(candidate);
                continue;
            }

            if (!seenKeysThisTick.Add(candidate.DedupeKey) || IsSuppressedByMemory(candidate, eventMemory, world))
            {
                suppressed.Add(candidate);
                continue;
            }

            switch (target)
            {
                case ChronicleOutputTarget.Chronicle:
                    chronicleCandidates.Add(candidate);
                    break;
                case ChronicleOutputTarget.Alert:
                    alertCandidates.Add(candidate);
                    break;
                case ChronicleOutputTarget.Diagnostics:
                    diagnostics.Add(candidate);
                    break;
                default:
                    suppressed.Add(candidate);
                    break;
            }
        }

        var prioritizedChronicle = PrioritizeChronicleCandidates(chronicleCandidates);
        var entries = BuildChronicleEntries(world, prioritizedChronicle);
        var alerts = BuildAlerts(world, alertCandidates);
        return new ChroniclePolicyResult(entries, alerts, diagnostics, suppressed);
    }

    public Chronicle Apply(World world, ChroniclePolicyResult result)
    {
        var retainedMemory = world.Chronicle.EventMemory
            .Where(memory => MonthsBetween(memory.EventYear, memory.EventMonth, world.CurrentYear, world.CurrentMonth) <= MemoryRetentionMonths)
            .ToList();
        retainedMemory.AddRange(result.ChronicleEntries.Select(entry => new ChronicleEventMemory
        {
            DedupeKey = entry.DedupeKey,
            CooldownFamily = entry.CooldownFamily,
            PolityId = entry.GroupId,
            OutputTarget = ChronicleOutputTarget.Chronicle,
            EventYear = entry.EventYear,
            EventMonth = entry.EventMonth
        }));
        retainedMemory.AddRange(result.Alerts.Select(alert => new ChronicleEventMemory
        {
            DedupeKey = alert.DedupeKey,
            CooldownFamily = alert.CooldownFamily,
            PolityId = alert.PolityId,
            OutputTarget = ChronicleOutputTarget.Alert,
            EventYear = alert.EventYear,
            EventMonth = alert.EventMonth
        }));

        var mergedEntries = world.Chronicle.Entries
            .Concat(result.ChronicleEntries)
            .OrderBy(entry => entry.RecordSequence)
            .ToArray();
        var mergedAlerts = world.Chronicle.Alerts
            .Concat(result.Alerts)
            .OrderBy(alert => alert.Sequence)
            .ToArray();

        return new Chronicle(
            mergedEntries,
            mergedAlerts,
            retainedMemory,
            mergedEntries.Length == 0 ? world.Chronicle.NextRecordSequence : mergedEntries.Max(entry => entry.RecordSequence) + 1,
            world.Chronicle.NextRevealSequence,
            mergedAlerts.Length == 0 ? world.Chronicle.NextAlertSequence : mergedAlerts.Max(alert => alert.Sequence) + 1);
    }

    private static ChronicleOutputTarget ResolveOutputTarget(ChronicleEventCandidate candidate)
    {
        if (candidate.PreferredOutputTarget != ChronicleOutputTarget.Chronicle)
        {
            return candidate.PreferredOutputTarget;
        }

        return candidate.Category switch
        {
            ChronicleCandidateCategory.Discovery => ChronicleOutputTarget.Chronicle,
            ChronicleCandidateCategory.Advancement => ChronicleOutputTarget.Chronicle,
            ChronicleCandidateCategory.Settlement => ChronicleOutputTarget.Chronicle,
            ChronicleCandidateCategory.Politics => ChronicleOutputTarget.Chronicle,
            ChronicleCandidateCategory.Polity => ChronicleOutputTarget.Chronicle,
            ChronicleCandidateCategory.Conflict when candidate.Severity >= ChronicleEventSeverity.Major => ChronicleOutputTarget.Chronicle,
            ChronicleCandidateCategory.Survival when candidate.TriggerKind is ChronicleTriggerKind.Collapsed or ChronicleTriggerKind.Escalated => ChronicleOutputTarget.Chronicle,
            ChronicleCandidateCategory.Environment => ChronicleOutputTarget.Chronicle,
            ChronicleCandidateCategory.Demography when candidate.Severity >= ChronicleEventSeverity.Notable => ChronicleOutputTarget.Chronicle,
            _ => ChronicleOutputTarget.Suppressed
        };
    }

    private static bool IsSuppressedByMemory(ChronicleEventCandidate candidate, IReadOnlyList<ChronicleEventMemory> memory, World world)
    {
        var cooldownMonths = ResolveCooldownMonths(candidate.CooldownFamily, candidate.Severity);
        return memory.Any(previous =>
            string.Equals(previous.PolityId, candidate.PolityId, StringComparison.Ordinal) &&
            (string.Equals(previous.DedupeKey, candidate.DedupeKey, StringComparison.Ordinal) ||
             string.Equals(previous.CooldownFamily, candidate.CooldownFamily, StringComparison.Ordinal)) &&
            MonthsBetween(previous.EventYear, previous.EventMonth, world.CurrentYear, world.CurrentMonth) < cooldownMonths);
    }

    private static int ResolveCooldownMonths(string cooldownFamily, ChronicleEventSeverity severity)
    {
        if (cooldownFamily.StartsWith("active-", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (cooldownFamily.Contains("collapse", StringComparison.OrdinalIgnoreCase) ||
            cooldownFamily.Contains("extinction", StringComparison.OrdinalIgnoreCase) ||
            severity == ChronicleEventSeverity.Critical)
        {
            return 12;
        }

        return severity switch
        {
            ChronicleEventSeverity.Major => 6,
            ChronicleEventSeverity.Notable => 4,
            _ => 2
        };
    }

    private static IReadOnlyList<ChronicleEventCandidate> PrioritizeChronicleCandidates(IReadOnlyList<ChronicleEventCandidate> candidates)
    {
        return candidates
            .GroupBy(candidate => candidate.PolityId, StringComparer.Ordinal)
            .SelectMany(group =>
            {
                var ordered = group
                    .OrderByDescending(candidate => candidate.Severity)
                    .ThenByDescending(candidate => candidate.Category == ChronicleCandidateCategory.Discovery || candidate.Category == ChronicleCandidateCategory.Advancement)
                    .ThenBy(candidate => candidate.EventType, StringComparer.Ordinal)
                    .ThenBy(candidate => candidate.DedupeKey, StringComparer.Ordinal)
                    .ToArray();
                var budget = ResolveChronicleBudget(ordered);
                return ordered.Take(budget);
            })
            .ToArray();
    }

    private static int ResolveChronicleBudget(IReadOnlyList<ChronicleEventCandidate> candidates)
    {
        if (candidates.Any(candidate => candidate.Severity == ChronicleEventSeverity.Critical))
        {
            return 3;
        }

        if (candidates.Count(candidate => candidate.Severity >= ChronicleEventSeverity.Major) >= 2 ||
            candidates.Count(candidate => candidate.Severity >= ChronicleEventSeverity.Notable) >= 3)
        {
            return 2;
        }

        return 1;
    }

    private static IReadOnlyList<ChronicleEntry> BuildChronicleEntries(World world, IReadOnlyList<ChronicleEventCandidate> candidates)
    {
        var entries = new List<ChronicleEntry>(candidates.Count);
        var nextRecordSequence = world.Chronicle.NextRecordSequence;
        foreach (var candidate in candidates)
        {
            var tokens = ChronicleFormatter.FormatTokens(candidate);
            var message = string.Concat(tokens.Select(token => token.Text));
            var (revealYear, revealMonth) = AddMonths(world.CurrentYear, world.CurrentMonth, ChronicleConstants.RevealDelayMonths);
            entries.Add(new ChronicleEntry
            {
                EventYear = candidate.EventYear,
                EventMonth = candidate.EventMonth,
                RecordSequence = nextRecordSequence++,
                RevealAfterYear = revealYear,
                RevealAfterMonth = revealMonth,
                GroupId = candidate.PolityId,
                GroupName = candidate.PolityName,
                Category = MapCategory(candidate),
                Message = message,
                Tags = candidate.Tags.ToArray(),
                DedupeKey = candidate.DedupeKey,
                CooldownFamily = candidate.CooldownFamily,
                Severity = candidate.Severity,
                TriggerKind = candidate.TriggerKind,
                Tokens = tokens
            });
        }

        return entries;
    }

    private static IReadOnlyList<ChronicleAlert> BuildAlerts(World world, IReadOnlyList<ChronicleEventCandidate> candidates)
    {
        var alerts = new List<ChronicleAlert>(candidates.Count);
        var nextSequence = world.Chronicle.NextAlertSequence;
        foreach (var candidate in candidates.OrderByDescending(candidate => candidate.Severity))
        {
            alerts.Add(new ChronicleAlert
            {
                EventYear = candidate.EventYear,
                EventMonth = candidate.EventMonth,
                Sequence = nextSequence++,
                PolityId = candidate.PolityId,
                PolityName = candidate.PolityName,
                Category = candidate.Category,
                Severity = candidate.Severity,
                TriggerKind = candidate.TriggerKind,
                Message = ChronicleFormatter.Format(candidate),
                DedupeKey = candidate.DedupeKey,
                CooldownFamily = candidate.CooldownFamily
            });
        }

        return alerts;
    }

    private static ChronicleEventCategory MapCategory(ChronicleEventCandidate candidate)
    {
        if (candidate.EventType == "law")
        {
            return ChronicleEventCategory.Law;
        }

        return candidate.Category switch
        {
            ChronicleCandidateCategory.Discovery => ChronicleEventCategory.Discovery,
            ChronicleCandidateCategory.Advancement => ChronicleEventCategory.Advancement,
            ChronicleCandidateCategory.Settlement => ChronicleEventCategory.Settlement,
            ChronicleCandidateCategory.Politics => ChronicleEventCategory.Political,
            ChronicleCandidateCategory.Conflict => ChronicleEventCategory.External,
            ChronicleCandidateCategory.Environment => ChronicleEventCategory.Biological,
            ChronicleCandidateCategory.Demography => ChronicleEventCategory.Decline,
            ChronicleCandidateCategory.Survival => ChronicleEventCategory.Shortage,
            ChronicleCandidateCategory.Territory => ChronicleEventCategory.Political,
            ChronicleCandidateCategory.Polity => ChronicleEventCategory.Political,
            _ => ChronicleEventCategory.Political
        };
    }

    private static int MonthsBetween(int fromYear, int fromMonth, int toYear, int toMonth)
    {
        return ((toYear - fromYear) * 12) + (toMonth - fromMonth);
    }

    private static (int Year, int Month) AddMonths(int year, int month, int monthsToAdd)
    {
        var absoluteMonth = ((year - 1) * 12) + (month - 1) + monthsToAdd;
        return (absoluteMonth / 12 + 1, absoluteMonth % 12 + 1);
    }
}
