using Species.Domain.Enums;
using Species.Domain.Models;
using Species.Domain.Simulation;

public static class ChronicleScreenDataBuilder
{
    public static ChronicleScreenData Build(World world, string focalPolityId, PlayerViewState viewState)
    {
        var focusPolity = PlayerFocus.Resolve(world, focalPolityId);
        var focusContext = PlayerFocus.ResolveContext(world, focalPolityId);
        var urgentItems = BuildUrgentItems(focusPolity, focusContext);
        var visibleEntries = GetVisibleEntries(world, focusPolity, focusContext);
        var liveEntries = BuildLiveEntries(visibleEntries);
        var milestoneEntries = visibleEntries
            .Where(IsMilestone)
            .Select(BuildListItem)
            .ToArray();

        var modeEntries = viewState.CurrentChronicleMode switch
        {
            ChronicleMode.Live => liveEntries,
            ChronicleMode.Archive => visibleEntries.Select(BuildListItem).ToArray(),
            _ => milestoneEntries
        };

        viewState.ClampChronicleSelection(urgentItems.Count, modeEntries.Count);

        var selectedUrgent = viewState.CurrentChronicleSelectionArea == ChronicleSelectionArea.Urgent && urgentItems.Count > 0
            ? urgentItems[viewState.CurrentChronicleUrgentIndex]
            : null;
        var selectedEntry = viewState.CurrentChronicleSelectionArea == ChronicleSelectionArea.Entries && modeEntries.Count > 0
            ? modeEntries[GetEntryIndex(viewState)]
            : null;

        return new ChronicleScreenData(
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            focusPolity?.Name ?? "Unknown polity",
            viewState.CurrentChronicleMode,
            urgentItems,
            modeEntries,
            selectedUrgent,
            selectedEntry,
            viewState.CurrentChronicleSelectionArea,
            BuildConditionSummary(focusPolity, focusContext),
            BuildModeNotes(viewState.CurrentChronicleMode, visibleEntries.Count, milestoneEntries.Length));
    }

    private static IReadOnlyList<ChronicleUrgentItem> BuildUrgentItems(Polity? polity, PolityContext? context)
    {
        var items = new List<ChronicleUrgentItem>();
        if (polity is null || context is null)
        {
            return items;
        }

        if (polity.ActiveLawProposal is not null)
        {
            items.Add(new ChronicleUrgentItem(
                "law-proposal",
                $"Decision pending: {polity.ActiveLawProposal.Title} now needs Pass or Veto.",
                polity.ActiveLawProposal.ReasonSummary,
                polity.ActiveLawProposal.TradeoffSummary,
                PlayerScreen.Laws,
                polity.ActiveLawProposal.Id));
        }

        if (context.Pressures.FoodPressure >= 78 || context.Pressures.WaterPressure >= 78 || context.MaterialShortageMonths >= 3)
        {
            items.Add(new ChronicleUrgentItem(
                "stores-crisis",
                "Stores are under severe strain and shortfalls are spreading.",
                context.Pressures.FoodPressure >= context.Pressures.WaterPressure
                    ? "Food pressure is running ahead of current reserves."
                    : "Water stress is now threatening stability.",
                "A prolonged shortage can trigger settlement loss, legitimacy damage, and migration.",
                PlayerScreen.Polity,
                null));
        }

        if (context.ExternalPressure.RaidPressure >= 55 || context.ExternalPressure.Threat >= 70)
        {
            items.Add(new ChronicleUrgentItem(
                "external-threat",
                "Frontier danger is escalating under outside pressure.",
                context.ExternalPressure.Summary,
                "Further raids or conflict can damage settlements, stores, and cohesion.",
                PlayerScreen.KnownPolities,
                null));
        }

        if (context.ScaleState.FragmentationRisk >= 68 || context.Governance.PeripheralStrain >= 72)
        {
            items.Add(new ChronicleUrgentItem(
                "fragmentation-risk",
                "Breakaway risk is rising on the periphery.",
                context.ScaleState.Summary,
                "If integration strain keeps climbing, the realm may lose outlying regions or attached polities.",
                PlayerScreen.Polity,
                null));
        }

        if (context.Governance.Governability <= 28 || context.Governance.Legitimacy <= 25 || context.Governance.Cohesion <= 25)
        {
            items.Add(new ChronicleUrgentItem(
                "governance-crisis",
                "Governance is faltering under low legitimacy and cohesion.",
                $"Legitimacy {context.Governance.Legitimacy}, cohesion {context.Governance.Cohesion}, authority {context.Governance.Authority}.",
                "Weak governability makes laws, enforcement, and coordinated recovery less reliable.",
                PlayerScreen.Laws,
                null));
        }

        return items
            .Take(3)
            .ToArray();
    }

    private static IReadOnlyList<ChronicleEntry> GetVisibleEntries(World world, Polity? focusPolity, PolityContext? focusContext)
    {
        if (focusPolity is null)
        {
            return Array.Empty<ChronicleEntry>();
        }

        var memberGroupIds = focusContext?.MemberGroups
            .Select(group => group.Id)
            .ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);

        return world.Chronicle.GetVisibleFeedEntries()
            .Where(entry =>
                string.Equals(entry.GroupId, focusPolity.Id, StringComparison.Ordinal) ||
                memberGroupIds.Contains(entry.GroupId))
            .ToArray();
    }

    private static IReadOnlyList<ChronicleListItem> BuildLiveEntries(IReadOnlyList<ChronicleEntry> entries)
    {
        var recentKeys = new Queue<string>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var items = new List<ChronicleListItem>();

        foreach (var entry in entries)
        {
            var key = $"{entry.Category}:{Normalize(entry.Message)}";
            if (seenKeys.Contains(key))
            {
                continue;
            }

            seenKeys.Add(key);
            recentKeys.Enqueue(key);
            if (recentKeys.Count > 12)
            {
                seenKeys.Remove(recentKeys.Dequeue());
            }

            items.Add(BuildListItem(entry));
        }

        return items;
    }

    private static ChronicleListItem BuildListItem(ChronicleEntry entry)
    {
        return new ChronicleListItem(
            $"{entry.RecordSequence}:{entry.RevealSequence}",
            entry.EventYear,
            entry.EventMonth,
            FormatMonthYear(entry.EventMonth, entry.EventYear),
            entry.Message,
            DescribeCategory(entry.Category),
            BuildEntryImpact(entry),
            IsMilestone(entry));
    }

    private static bool IsMilestone(ChronicleEntry entry)
    {
        return entry.Category switch
        {
            ChronicleEventCategory.Advancement => true,
            ChronicleEventCategory.Discovery => true,
            ChronicleEventCategory.Law => true,
            ChronicleEventCategory.Settlement => true,
            ChronicleEventCategory.External => true,
            ChronicleEventCategory.Political => true,
            ChronicleEventCategory.Social => true,
            ChronicleEventCategory.Biological => true,
            ChronicleEventCategory.Extinction => true,
            ChronicleEventCategory.Decline => true,
            ChronicleEventCategory.Shortage => entry.Message.Contains("crisis", StringComparison.OrdinalIgnoreCase) ||
                                               entry.Message.Contains("forced", StringComparison.OrdinalIgnoreCase),
            ChronicleEventCategory.Material => entry.Message.Contains("stabil", StringComparison.OrdinalIgnoreCase) ||
                                              entry.Message.Contains("shortage", StringComparison.OrdinalIgnoreCase) ||
                                              entry.Message.Contains("contract", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static IReadOnlyList<string> BuildConditionSummary(Polity? polity, PolityContext? context)
    {
        if (polity is null || context is null)
        {
            return ["No focal polity is currently available."];
        }

        var lines = new List<string>
        {
            $"Condition: {context.Governance.Governability} governability with {context.ScaleState.Form} scale.",
            $"Direction: {SummarizeDirection(context)}",
            $"Top pressure: {SummarizeTopPressure(context)}",
            $"External: {context.ExternalPressure.Summary}",
            $"Stores: Food {context.TotalStoredFood:N0}; timber {context.TotalMaterialStores.Timber}; stone {context.TotalMaterialStores.Stone}.",
            $"Laws: {(polity.EnactedLaws.Count(law => law.IsActive) == 0 ? "No active laws." : $"{polity.EnactedLaws.Count(law => law.IsActive)} active law{(polity.EnactedLaws.Count(law => law.IsActive) == 1 ? string.Empty : "s")}.")}"
        };

        if (polity.ActiveLawProposal is not null)
        {
            lines.Add($"Decision waiting: {polity.ActiveLawProposal.Title}.");
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildModeNotes(ChronicleMode mode, int archiveCount, int milestoneCount)
    {
        return mode switch
        {
            ChronicleMode.Live =>
            [
                "Live follows current developments and suppresses near-duplicate noise.",
                "Use Backspace to snap back here after browsing.",
                archiveCount == 0 ? "No visible history yet." : $"Visible history: {archiveCount} entries."
            ],
            ChronicleMode.Archive =>
            [
                "Archive keeps your place while newer events continue to arrive.",
                "Use Up/Down to browse older entries without losing the live feed.",
                archiveCount == 0 ? "No archived entries are visible yet." : $"Archive entries: {archiveCount}."
            ],
            _ =>
            [
                "Milestones keeps only higher-value turning points.",
                "Formation, crisis, law, discovery, conflict, and breakaway events rise here.",
                milestoneCount == 0 ? "No milestones are visible yet." : $"Milestones: {milestoneCount}."
            ]
        };
    }

    private static int GetEntryIndex(PlayerViewState viewState)
    {
        return viewState.CurrentChronicleMode switch
        {
            ChronicleMode.Live => viewState.CurrentChronicleLiveIndex,
            ChronicleMode.Archive => viewState.CurrentChronicleArchiveIndex,
            _ => viewState.CurrentChronicleMilestoneIndex
        };
    }

    private static string SummarizeDirection(PolityContext context)
    {
        if (context.ScaleState.FragmentationRisk >= 65)
        {
            return "Integration strain is pulling against the realm.";
        }

        if (context.ExternalPressure.Cooperation >= 55 && context.ExternalPressure.Threat < 45)
        {
            return "Outside conditions are opening room for steadier coexistence.";
        }

        if (context.MaterialSurplusMonths >= 3 && context.Pressures.FoodPressure < 45)
        {
            return "Material and food conditions are supporting stability.";
        }

        if (context.Pressures.MigrationPressure >= 60)
        {
            return "Movement pressure is pushing the polity to reposition.";
        }

        return "No single direction dominates the current month.";
    }

    private static string SummarizeTopPressure(PolityContext context)
    {
        var pressures = new (string Label, int Value)[]
        {
            ("Food", context.Pressures.FoodPressure),
            ("Water", context.Pressures.WaterPressure),
            ("Threat", context.Pressures.ThreatPressure),
            ("Crowding", context.Pressures.OvercrowdingPressure),
            ("Migration", context.Pressures.MigrationPressure)
        };

        var strongest = pressures
            .OrderByDescending(item => item.Value)
            .First();

        return $"{strongest.Label} pressure at {strongest.Value}.";
    }

    private static string DescribeCategory(ChronicleEventCategory category)
    {
        return category switch
        {
            ChronicleEventCategory.External => "External",
            ChronicleEventCategory.Political => "Political",
            ChronicleEventCategory.Biological => "Biological",
            ChronicleEventCategory.Advancement => "Advancement",
            ChronicleEventCategory.Discovery => "Discovery",
            ChronicleEventCategory.Settlement => "Settlement",
            ChronicleEventCategory.Shortage => "Shortage",
            ChronicleEventCategory.Material => "Material",
            ChronicleEventCategory.Social => "Social",
            _ => category.ToString()
        };
    }

    private static string BuildEntryImpact(ChronicleEntry entry)
    {
        return entry.Category switch
        {
            ChronicleEventCategory.Law => "This changed how the polity is being directed or contested.",
            ChronicleEventCategory.External => "This changed outside pressure, rivalry, or cooperation.",
            ChronicleEventCategory.Political => "This changed the structure or reach of the polity.",
            ChronicleEventCategory.Settlement => "This changed where the polity is anchored on the map.",
            ChronicleEventCategory.Advancement => "This added a usable capability to the polity.",
            ChronicleEventCategory.Discovery => "This expanded what the polity knows about the world.",
            ChronicleEventCategory.Material => "This changed material stability, extraction, or resilience.",
            ChronicleEventCategory.Social => "This changed the polity's social tendencies or traditions.",
            ChronicleEventCategory.Biological => "This changed the living world in a way the polity can feel.",
            ChronicleEventCategory.Shortage or ChronicleEventCategory.Decline => "This reflects immediate strain that can cascade into migration, instability, or collapse.",
            _ => "This is part of the unfolding history around the polity."
        };
    }

    private static string Normalize(string text)
    {
        return new string(text
            .ToLowerInvariant()
            .Where(character => !char.IsWhiteSpace(character) && !char.IsPunctuation(character))
            .ToArray());
    }

    private static string FormatMonthYear(int month, int year)
    {
        var monthText = month switch
        {
            1 => "Jan",
            2 => "Feb",
            3 => "Mar",
            4 => "Apr",
            5 => "May",
            6 => "Jun",
            7 => "Jul",
            8 => "Aug",
            9 => "Sep",
            10 => "Oct",
            11 => "Nov",
            12 => "Dec",
            _ => "Jan"
        };

        return $"{monthText} {year:D3}";
    }
}

public sealed record ChronicleScreenData(
    string CurrentDate,
    string PolityName,
    ChronicleMode Mode,
    IReadOnlyList<ChronicleUrgentItem> UrgentItems,
    IReadOnlyList<ChronicleListItem> Entries,
    ChronicleUrgentItem? SelectedUrgent,
    ChronicleListItem? SelectedEntry,
    ChronicleSelectionArea SelectedArea,
    IReadOnlyList<string> ConditionSummary,
    IReadOnlyList<string> ModeNotes);

public sealed record ChronicleUrgentItem(
    string Id,
    string Text,
    string Cause,
    string Impact,
    PlayerScreen TargetScreen,
    string? TargetId);

public sealed record ChronicleListItem(
    string Id,
    int EventYear,
    int EventMonth,
    string DateText,
    string Headline,
    string Category,
    string Impact,
    bool IsMilestone);
