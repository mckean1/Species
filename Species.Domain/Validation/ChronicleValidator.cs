using Species.Domain.Enums;
using Species.Domain.Models;
using Species.Domain.Simulation;

namespace Species.Domain.Validation;

public static class ChronicleValidator
{
    public static IReadOnlyList<string> Validate(World world, SimulationTickResult? tickResult = null)
    {
        var errors = new List<string>();

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
                !IsValidKnowledgeChronicleLine(entry.Message))
            {
                errors.Add($"Chronicle discovery entry {entry.RecordSequence} does not use valid knowledge wording.");
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

    private static bool IsValidKnowledgeChronicleLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || !message.EndsWith(".", StringComparison.Ordinal))
        {
            return false;
        }

        return message.Contains(" discovered ", StringComparison.Ordinal) ||
               message.Contains(" learned ", StringComparison.Ordinal) ||
               message.Contains(" exposed ", StringComparison.Ordinal);
    }

    private static bool IsValidCapabilityChronicleLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || !message.EndsWith(".", StringComparison.Ordinal))
        {
            return false;
        }

        return message.Contains(" learned ", StringComparison.Ordinal) ||
               message.Contains(" adopted ", StringComparison.Ordinal) ||
               message.Contains(" improved ", StringComparison.Ordinal);
    }
}
