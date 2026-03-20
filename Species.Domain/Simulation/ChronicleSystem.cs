using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class ChronicleSystem
{
    public ChronicleUpdateResult Run(
        World world,
        IReadOnlyList<GroupSurvivalChange> survivalChanges,
        IReadOnlyList<MigrationChange> migrationChanges,
        IReadOnlyList<DiscoveryChange> discoveryChanges,
        IReadOnlyList<AdvancementChange> advancementChanges,
        IReadOnlyList<LawProposalChange> lawProposalChanges,
        IReadOnlyList<SettlementChange> settlementChanges)
    {
        var recordedEntries = BuildRecordedEntries(world, survivalChanges, migrationChanges, discoveryChanges, advancementChanges, lawProposalChanges, settlementChanges);
        var updatedChronicle = RecordEntries(world.Chronicle, recordedEntries, world.CurrentYear, world.CurrentMonth);
        var revealedEntries = RevealEntries(updatedChronicle, world.CurrentYear, world.CurrentMonth, out var revealedChronicle);
        return new ChronicleUpdateResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, world.PopulationGroups, revealedChronicle, world.Polities, world.FocalPolityId),
            recordedEntries,
            revealedEntries);
    }

    private static IReadOnlyList<ChronicleEntry> BuildRecordedEntries(
        World world,
        IReadOnlyList<GroupSurvivalChange> survivalChanges,
        IReadOnlyList<MigrationChange> migrationChanges,
        IReadOnlyList<DiscoveryChange> discoveryChanges,
        IReadOnlyList<AdvancementChange> advancementChanges,
        IReadOnlyList<LawProposalChange> lawProposalChanges,
        IReadOnlyList<SettlementChange> settlementChanges)
    {
        var entries = new List<ChronicleEntry>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var nextRecordSequence = world.Chronicle.NextRecordSequence;

        foreach (var change in migrationChanges.Where(change => change.Moved))
        {
            AddEntry(entries, seenKeys, BuildEntry(
                world,
                nextRecordSequence++,
                change.GroupId,
                change.GroupName,
                ChronicleEventCategory.Migration,
                $"{change.GroupName} migrated to {change.NewRegionName}.",
                "migration",
                change.NewRegionId));
        }

        foreach (var change in survivalChanges)
        {
            if (change.Shortage >= ChronicleConstants.MeaningfulShortageThreshold)
            {
                AddEntry(entries, seenKeys, BuildEntry(
                    world,
                    nextRecordSequence++,
                    change.GroupId,
                    change.GroupName,
                    ChronicleEventCategory.Shortage,
                    $"{change.GroupName} suffered food shortages.",
                    "shortage",
                    change.CurrentRegionId));
            }

            if (change.StarvationLoss >= ChronicleConstants.MeaningfulDeclineThreshold && change.FinalPopulation > 0)
            {
                AddEntry(entries, seenKeys, BuildEntry(
                    world,
                    nextRecordSequence++,
                    change.GroupId,
                    change.GroupName,
                    ChronicleEventCategory.Decline,
                    $"{change.GroupName} declined after hunger and loss.",
                    "decline",
                    change.CurrentRegionId));
            }

            if (change.FinalPopulation == 0)
            {
                AddEntry(entries, seenKeys, BuildEntry(
                    world,
                    nextRecordSequence++,
                    change.GroupId,
                    change.GroupName,
                    ChronicleEventCategory.Extinction,
                    $"{change.GroupName} died out.",
                    "extinction",
                    change.CurrentRegionId));
            }
        }

        foreach (var change in discoveryChanges.Where(change => !string.Equals(change.UnlockedDiscoveriesSummary, "none", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var discoveryName in SplitSummary(change.UnlockedDiscoveriesSummary))
            {
                AddEntry(entries, seenKeys, BuildEntry(
                    world,
                    nextRecordSequence++,
                    change.GroupId,
                    change.GroupName,
                    ChronicleEventCategory.Discovery,
                    $"{change.GroupName} discovered {discoveryName.ToLowerInvariant()}.",
                    "discovery"));
            }
        }

        foreach (var change in advancementChanges.Where(change => !string.Equals(change.UnlockedAdvancementsSummary, "none", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var advancementName in SplitSummary(change.UnlockedAdvancementsSummary))
            {
                AddEntry(entries, seenKeys, BuildEntry(
                    world,
                    nextRecordSequence++,
                    change.GroupId,
                    change.GroupName,
                    ChronicleEventCategory.Advancement,
                    $"{change.GroupName} learned {advancementName.ToLowerInvariant()}.",
                    "advancement"));
            }
        }

        foreach (var change in lawProposalChanges)
        {
            AddEntry(entries, seenKeys, BuildEntry(
                world,
                nextRecordSequence++,
                change.GroupId,
                change.GroupName,
                ChronicleEventCategory.Law,
                change.Status == Species.Domain.Enums.LawProposalStatus.Passed
                    ? $"{change.GroupName} passed {change.ProposalTitle}."
                    : $"{change.GroupName} vetoed {change.ProposalTitle}.",
                "law"));
        }

        foreach (var change in settlementChanges)
        {
            AddEntry(entries, seenKeys, BuildEntry(
                world,
                nextRecordSequence++,
                change.PolityId,
                change.PolityName,
                ChronicleEventCategory.Settlement,
                change.Message,
                "settlement",
                change.RegionId));
        }

        return entries;
    }

    public World RecordLawDecision(World world, LawProposalChange change)
    {
        var message = change.Status == Species.Domain.Enums.LawProposalStatus.Passed
            ? $"{change.GroupName} passed {change.ProposalTitle}."
            : $"{change.GroupName} vetoed {change.ProposalTitle}.";
        var entry = BuildEntry(
            world,
            world.Chronicle.NextRecordSequence,
            change.GroupId,
            change.GroupName,
            ChronicleEventCategory.Law,
            message,
            "law");
        var chronicle = RecordEntries(world.Chronicle, [entry], world.CurrentYear, world.CurrentMonth);
        return new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, world.PopulationGroups, chronicle, world.Polities, world.FocalPolityId);
    }

    private static void AddEntry(ICollection<ChronicleEntry> entries, ISet<string> seenKeys, ChronicleEntry entry)
    {
        var key = $"{entry.EventYear}:{entry.EventMonth}:{entry.GroupId}:{entry.Category}:{entry.Message}";
        if (seenKeys.Add(key))
        {
            entries.Add(entry);
        }
    }

    private static ChronicleEntry BuildEntry(
        World world,
        int recordSequence,
        string groupId,
        string groupName,
        ChronicleEventCategory category,
        string message,
        params string[] tags)
    {
        var (revealYear, revealMonth) = AddMonths(world.CurrentYear, world.CurrentMonth, ChronicleConstants.RevealDelayMonths);
        return new ChronicleEntry
        {
            EventYear = world.CurrentYear,
            EventMonth = world.CurrentMonth,
            RecordSequence = recordSequence,
            RevealAfterYear = revealYear,
            RevealAfterMonth = revealMonth,
            GroupId = groupId,
            GroupName = groupName,
            Category = category,
            Message = message,
            Tags = tags
        };
    }

    private static Chronicle RecordEntries(Chronicle chronicle, IReadOnlyList<ChronicleEntry> recordedEntries, int currentYear, int currentMonth)
    {
        if (recordedEntries.Count == 0)
        {
            return chronicle;
        }

        var mergedEntries = chronicle.Entries
            .Concat(recordedEntries)
            .OrderBy(entry => entry.RecordSequence)
            .ToArray();

        return new Chronicle(mergedEntries, mergedEntries.Max(entry => entry.RecordSequence) + 1, chronicle.NextRevealSequence);
    }

    private static IReadOnlyList<ChronicleEntry> RevealEntries(Chronicle chronicle, int currentYear, int currentMonth, out Chronicle updatedChronicle)
    {
        var revealableEntries = chronicle.GetPendingEntries()
            .Where(entry => IsDue(entry, currentYear, currentMonth))
            .Take(ChronicleConstants.MaxRevealPerTick)
            .ToArray();

        if (revealableEntries.Length == 0)
        {
            updatedChronicle = chronicle;
            return Array.Empty<ChronicleEntry>();
        }

        var revealedEntriesBySequence = new Dictionary<int, ChronicleEntry>();
        var nextRevealSequence = chronicle.NextRevealSequence;
        foreach (var entry in revealableEntries)
        {
            revealedEntriesBySequence[entry.RecordSequence] = entry.Reveal(currentYear, currentMonth, nextRevealSequence++);
        }

        var mergedEntries = chronicle.Entries
            .Select(entry => revealedEntriesBySequence.TryGetValue(entry.RecordSequence, out var revealedEntry) ? revealedEntry : entry)
            .OrderBy(entry => entry.RecordSequence)
            .ToArray();

        updatedChronicle = new Chronicle(mergedEntries, chronicle.NextRecordSequence, nextRevealSequence);
        return revealedEntriesBySequence.Values
            .OrderByDescending(entry => entry.RevealSequence)
            .ToArray();
    }

    private static bool IsDue(ChronicleEntry entry, int currentYear, int currentMonth)
    {
        if (entry.RevealAfterYear < currentYear)
        {
            return true;
        }

        return entry.RevealAfterYear == currentYear && entry.RevealAfterMonth <= currentMonth;
    }

    private static (int Year, int Month) AddMonths(int year, int month, int monthsToAdd)
    {
        var absoluteMonth = ((year - 1) * 12) + (month - 1) + monthsToAdd;
        return (absoluteMonth / 12 + 1, absoluteMonth % 12 + 1);
    }

    private static IReadOnlyList<string> SplitSummary(string summary)
    {
        return summary
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }
}
