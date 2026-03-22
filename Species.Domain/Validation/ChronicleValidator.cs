using Species.Domain.Enums;
using Species.Domain.Models;
using Species.Domain.Simulation;

namespace Species.Domain.Validation;

public static class ChronicleValidator
{
    public static IReadOnlyList<string> Validate(World world, SimulationTickResult? tickResult = null)
    {
        var errors = new List<string>();
        errors.AddRange(ValidateCanonicalDiscoveryTemplates());

        if (world.Chronicle is null)
        {
            errors.Add("World chronicle cannot be null.");
            return errors;
        }

        var visibleEntries = world.Chronicle.GetVisibleFeedEntries();
        var seenRecordSequences = new HashSet<int>();
        var seenRevealSequences = new HashSet<int>();
        for (var index = 1; index < visibleEntries.Count; index++)
        {
            if (visibleEntries[index - 1].RevealSequence < visibleEntries[index].RevealSequence)
            {
                errors.Add("Chronicle visible feed is not newest-first by reveal order.");
                break;
            }
        }

        foreach (var entry in world.Chronicle.Entries)
        {
            if (!seenRecordSequences.Add(entry.RecordSequence))
            {
                errors.Add($"Chronicle entry record sequence {entry.RecordSequence} is duplicated.");
            }

            if (string.IsNullOrWhiteSpace(entry.Message))
            {
                errors.Add($"Chronicle entry {entry.RecordSequence} has a blank message.");
            }

            if (entry.Tokens.Count == 0)
            {
                errors.Add($"Chronicle entry {entry.RecordSequence} is missing render tokens.");
            }
            else
            {
                var tokenText = string.Concat(entry.Tokens.Select(token => token.Text));
                if (!string.Equals(tokenText, entry.Message, StringComparison.Ordinal))
                {
                    errors.Add($"Chronicle entry {entry.RecordSequence} token text does not match the plain fallback message.");
                }
            }

            if (string.IsNullOrWhiteSpace(entry.DedupeKey))
            {
                errors.Add($"Chronicle entry {entry.RecordSequence} is missing a dedupe key.");
            }

            if (string.IsNullOrWhiteSpace(entry.CooldownFamily))
            {
                errors.Add($"Chronicle entry {entry.RecordSequence} is missing a cooldown family.");
            }

            if (entry.IsRevealed)
            {
                if (entry.RevealedYear is null || entry.RevealedMonth is null || entry.RevealSequence is null)
                {
                    errors.Add($"Chronicle entry {entry.RecordSequence} is revealed without complete reveal metadata.");
                }
                else if (!seenRevealSequences.Add(entry.RevealSequence.Value))
                {
                    errors.Add($"Chronicle reveal sequence {entry.RevealSequence.Value} is duplicated.");
                }
            }

            if (entry.Category == ChronicleEventCategory.Discovery &&
                !IsValidDiscoveryChronicleLine(entry.Message))
            {
                errors.Add($"Chronicle discovery entry {entry.RecordSequence} does not use canonical discovery wording.");
            }

            if (entry.Category == ChronicleEventCategory.Advancement &&
                !IsValidCapabilityChronicleLine(entry.Message))
            {
                errors.Add($"Chronicle advancement entry {entry.RecordSequence} does not use valid capability wording.");
            }

            if (entry.Category == ChronicleEventCategory.Law &&
                !IsValidLawChronicleLine(entry.Message))
            {
                errors.Add($"Chronicle law entry {entry.RecordSequence} does not use the canonical pass/veto format.");
            }

            if (entry.Category == ChronicleEventCategory.Material &&
                !entry.Message.EndsWith(".", StringComparison.Ordinal))
            {
                errors.Add($"Chronicle material entry {entry.RecordSequence} is missing terminal punctuation.");
            }

            if (entry.Category == ChronicleEventCategory.Social &&
                !entry.Message.EndsWith(".", StringComparison.Ordinal))
            {
                errors.Add($"Chronicle social entry {entry.RecordSequence} is missing terminal punctuation.");
            }

            if (entry.Category == ChronicleEventCategory.Biological &&
                !entry.Message.EndsWith(".", StringComparison.Ordinal))
            {
                errors.Add($"Chronicle biological entry {entry.RecordSequence} is missing terminal punctuation.");
            }

            if (entry.Category == ChronicleEventCategory.External &&
                !entry.Message.EndsWith(".", StringComparison.Ordinal))
            {
                errors.Add($"Chronicle external entry {entry.RecordSequence} is missing terminal punctuation.");
            }

            if (entry.Category == ChronicleEventCategory.Political &&
                !entry.Message.EndsWith(".", StringComparison.Ordinal))
            {
                errors.Add($"Chronicle political entry {entry.RecordSequence} is missing terminal punctuation.");
            }
        }

        var alertSequences = new HashSet<int>();
        foreach (var alert in world.Chronicle.Alerts)
        {
            if (!alertSequences.Add(alert.Sequence))
            {
                errors.Add($"Chronicle alert sequence {alert.Sequence} is duplicated.");
            }

            if (string.IsNullOrWhiteSpace(alert.Message))
            {
                errors.Add($"Chronicle alert {alert.Sequence} has a blank message.");
            }

            if (string.IsNullOrWhiteSpace(alert.DedupeKey) || string.IsNullOrWhiteSpace(alert.CooldownFamily))
            {
                errors.Add($"Chronicle alert {alert.Sequence} is missing dedupe or cooldown metadata.");
            }
        }

        if (tickResult is null)
        {
            return errors;
        }

        foreach (var entry in tickResult.RevealedChronicleEntries)
        {
            if (!entry.IsRevealed)
            {
                errors.Add($"Chronicle entry {entry.RecordSequence} was reported as revealed without reveal metadata.");
            }
        }

        return errors;
    }

    private static bool IsValidLawChronicleLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || !message.EndsWith(".", StringComparison.Ordinal))
        {
            return false;
        }

        return message.Contains(' ');
    }

    private static bool IsValidDiscoveryChronicleLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || !message.EndsWith(".", StringComparison.Ordinal))
        {
            return false;
        }

        if (message.Contains("conditions", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return message.Contains(" discovered ", StringComparison.Ordinal) ||
               message.Contains(" encountered ", StringComparison.Ordinal);
    }

    private static bool IsValidCapabilityChronicleLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || !message.EndsWith(".", StringComparison.Ordinal))
        {
            return false;
        }

        return message.Contains(" learned ", StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> ValidateCanonicalDiscoveryTemplates()
    {
        var errors = new List<string>();

        var discovered = ChronicleFormatter.Format(BuildDiscoveryCandidate(
            ChronicleTriggerKind.Discovered,
            discoveryName: "Mist Hollow"));
        if (!string.Equals(discovered, "Blue River discovered Mist Hollow.", StringComparison.Ordinal))
        {
            errors.Add("Chronicle non-encounter discovery template no longer uses the canonical discovered wording.");
        }

        var scoutedDiscovery = ChronicleFormatter.Format(BuildDiscoveryCandidate(
            ChronicleTriggerKind.Discovered,
            discoveryName: "Mist Hollow",
            regionName: "Mist Hollow",
            isScoutSourced: true));
        if (!string.Equals(scoutedDiscovery, "Blue River scouts discovered Mist Hollow in Mist Hollow.", StringComparison.Ordinal))
        {
            errors.Add("Chronicle scout discovery template no longer uses the canonical discovered wording.");
        }

        var encountered = ChronicleFormatter.Format(BuildDiscoveryCandidate(
            ChronicleTriggerKind.Encountered,
            otherPartyName: "Hill Folk"));
        if (!string.Equals(encountered, "Blue River encountered Hill Folk.", StringComparison.Ordinal))
        {
            errors.Add("Chronicle encounter template no longer uses the canonical encountered wording.");
        }

        var scoutedEncounter = ChronicleFormatter.Format(BuildDiscoveryCandidate(
            ChronicleTriggerKind.Encountered,
            otherPartyName: "Hill Folk",
            regionName: "Mist Hollow",
            isScoutSourced: true));
        if (!string.Equals(scoutedEncounter, "Blue River scouts encountered Hill Folk in Mist Hollow.", StringComparison.Ordinal))
        {
            errors.Add("Chronicle scout encounter template no longer uses the canonical encountered wording.");
        }

        if (new[] { discovered, scoutedDiscovery, encountered, scoutedEncounter }
            .Any(message => message.Contains("conditions", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Chronicle canonical discovery templates still include forbidden discovery wording.");
        }

        return errors;
    }

    private static ChronicleEventCandidate BuildDiscoveryCandidate(
        ChronicleTriggerKind triggerKind,
        string? discoveryName = null,
        string? otherPartyName = null,
        string? regionName = null,
        bool isScoutSourced = false)
    {
        return new ChronicleEventCandidate
        {
            EventYear = 1,
            EventMonth = 1,
            PolityId = "polity-blue-river",
            PolityName = "Blue River",
            EventType = "discovery",
            Category = ChronicleCandidateCategory.Discovery,
            Severity = ChronicleEventSeverity.Notable,
            TriggerKind = triggerKind,
            DiscoveryName = discoveryName,
            OtherPartyName = otherPartyName,
            RegionName = regionName,
            IsScoutSourced = isScoutSourced,
            DedupeKey = $"validation:{triggerKind}:{discoveryName}:{otherPartyName}:{regionName}:{isScoutSourced}",
            CooldownFamily = "validation",
            Tags = ["validation"]
        };
    }
}
