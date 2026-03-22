using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;
using Species.Domain.Simulation;
using Species.Client.Enums;
using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;

namespace Species.Client.ViewModelFactories;

public static class ChronicleViewModelFactory
{
    private const int ActiveAlertVisibilityMonths = 2;

    public static ChronicleViewModel Build(World world, string focalPolityId, ChronicleViewRequest request, bool isSimulationRunning = false)
    {
        var focusPolity = PlayerFocus.Resolve(world, focalPolityId);
        var focusContext = PlayerFocus.ResolveContext(world, focalPolityId);
        var selectionData = BuildSelectionData(world, focusPolity, focusContext, request);

        return new ChronicleViewModel(
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            focusPolity?.Name ?? "Unknown polity",
            isSimulationRunning,
            request.Mode,
            selectionData.UrgentItems,
            selectionData.ModeEntries,
            selectionData.SelectedUrgent,
            selectionData.SelectedEntry,
            selectionData.SelectedArea,
            BuildConditionSummary(focusPolity, focusContext),
            BuildModeNotes(request.Mode, selectionData.ArchiveEntryCount, selectionData.MilestoneEntryCount));
    }

    public static ChronicleSelectionInfo QuerySelectionInfo(World world, string focalPolityId, ChronicleViewRequest request)
    {
        var focusPolity = PlayerFocus.Resolve(world, focalPolityId);
        var focusContext = PlayerFocus.ResolveContext(world, focalPolityId);
        var urgentItems = BuildUrgentItems(world, focusPolity, focusContext);
        var visibleEntries = GetVisibleEntries(world, focusPolity, focusContext);
        var entryCount = request.Mode switch
        {
            ChronicleMode.Live => BuildLiveEntries(visibleEntries).Count,
            ChronicleMode.Archive => visibleEntries.Count,
            _ => visibleEntries.Count(IsMilestone)
        };
        var selectedArea = ResolveSelectionArea(request.SelectedArea, urgentItems.Count, entryCount);
        var selectedUrgent = selectedArea == ChronicleSelectionArea.Urgent && urgentItems.Count > 0
            ? urgentItems[ClampIndex(request.SelectedUrgentIndex, urgentItems.Count)]
            : null;

        return new ChronicleSelectionInfo(
            urgentItems.Count,
            entryCount,
            selectedUrgent);
    }

    private static IReadOnlyList<ChronicleUrgentItem> BuildUrgentItems(World world, Polity? polity, PolityContext? context)
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

        items.AddRange(BuildChronicleAlerts(world, polity));

        return items
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .Take(3)
            .ToArray();
    }

    private static IReadOnlyList<ChronicleUrgentItem> BuildChronicleAlerts(World world, Polity polity)
    {
        return world.Chronicle
            .GetCurrentAlerts(world.CurrentYear, world.CurrentMonth, ActiveAlertVisibilityMonths)
            .Where(alert => string.Equals(alert.PolityId, polity.Id, StringComparison.Ordinal))
            .OrderByDescending(alert => alert.Severity)
            .ThenByDescending(alert => alert.Sequence)
            .Select(BuildAlertItem)
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
            entry.Tokens,
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

    private static (
        IReadOnlyList<ChronicleUrgentItem> UrgentItems,
        IReadOnlyList<ChronicleListItem> ModeEntries,
        ChronicleSelectionArea SelectedArea,
        ChronicleUrgentItem? SelectedUrgent,
        ChronicleListItem? SelectedEntry,
        int ArchiveEntryCount,
        int MilestoneEntryCount) BuildSelectionData(
        World world,
        Polity? focusPolity,
        PolityContext? focusContext,
        ChronicleViewRequest request)
    {
        var urgentItems = BuildUrgentItems(world, focusPolity, focusContext);
        var visibleEntries = GetVisibleEntries(world, focusPolity, focusContext);
        var liveEntries = BuildLiveEntries(visibleEntries);
        var archiveEntries = visibleEntries.Select(BuildListItem).ToArray();
        var milestoneEntries = visibleEntries
            .Where(IsMilestone)
            .Select(BuildListItem)
            .ToArray();

        var modeEntries = request.Mode switch
        {
            ChronicleMode.Live => liveEntries,
            ChronicleMode.Archive => archiveEntries,
            _ => milestoneEntries
        };

        var selectedArea = ResolveSelectionArea(request.SelectedArea, urgentItems.Count, modeEntries.Count);
        var selectedUrgentIndex = ClampIndex(request.SelectedUrgentIndex, urgentItems.Count);
        var selectedEntryIndex = ClampIndex(GetEntryIndex(request), modeEntries.Count);
        var selectedUrgent = selectedArea == ChronicleSelectionArea.Urgent && urgentItems.Count > 0
            ? urgentItems[selectedUrgentIndex]
            : null;
        var selectedEntry = selectedArea == ChronicleSelectionArea.Entries && modeEntries.Count > 0
            ? modeEntries[selectedEntryIndex]
            : null;

        return (
            urgentItems,
            modeEntries,
            selectedArea,
            selectedUrgent,
            selectedEntry,
            archiveEntries.Length,
            milestoneEntries.Length);
    }

    private static int GetEntryIndex(ChronicleViewRequest request)
    {
        return request.Mode switch
        {
            ChronicleMode.Live => request.SelectedLiveEntryIndex,
            ChronicleMode.Archive => request.SelectedArchiveEntryIndex,
            _ => request.SelectedMilestoneEntryIndex
        };
    }

    private static ChronicleSelectionArea ResolveSelectionArea(ChronicleSelectionArea selectionArea, int urgentCount, int entryCount)
    {
        if (urgentCount <= 0 && entryCount <= 0)
        {
            return ChronicleSelectionArea.Entries;
        }

        if (selectionArea == ChronicleSelectionArea.Urgent && urgentCount <= 0)
        {
            return ChronicleSelectionArea.Entries;
        }

        if (selectionArea == ChronicleSelectionArea.Entries && entryCount <= 0)
        {
            return urgentCount > 0
                ? ChronicleSelectionArea.Urgent
                : ChronicleSelectionArea.Entries;
        }

        return selectionArea;
    }

    private static int ClampIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        if (index < 0)
        {
            return 0;
        }

        return index >= count ? count - 1 : index;
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

        if (context.MaterialSurplusMonths >= 3 && context.Pressures.Food.DisplayValue < ChronicleConstants.StablePressureDisplayThreshold)
        {
            return "Support conditions and food are supporting stability.";
        }

        if (context.Pressures.Migration.DisplayValue >= ChronicleConstants.PressureConcernDisplayThreshold)
        {
            return "Movement pressure is pushing the polity to reposition.";
        }

        if (context.Pressures.Curiosity.DisplayValue >= ChronicleConstants.PressureConcernDisplayThreshold)
        {
            return "Curiosity pressure is pushing the polity toward nearby unknown ground.";
        }

        return "No single direction dominates the current month.";
    }

    private static string SummarizeTopPressure(PolityContext context)
    {
        var pressures = new (string Label, int Value, string SeverityLabel)[]
        {
            ("Food", context.Pressures.Food.DisplayValue, context.Pressures.Food.SeverityLabel),
            ("Water", context.Pressures.Water.DisplayValue, context.Pressures.Water.SeverityLabel),
            ("Threat", context.Pressures.Threat.DisplayValue, context.Pressures.Threat.SeverityLabel),
            ("Crowding", context.Pressures.Overcrowding.DisplayValue, context.Pressures.Overcrowding.SeverityLabel),
            ("Migration", context.Pressures.Migration.DisplayValue, context.Pressures.Migration.SeverityLabel),
            ("Curiosity", context.Pressures.Curiosity.DisplayValue, context.Pressures.Curiosity.SeverityLabel)
        };

        var strongest = pressures
            .OrderByDescending(item => item.Value)
            .First();

        return $"{strongest.Label} ({strongest.SeverityLabel})";
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

    private static ChronicleUrgentItem BuildAlertItem(ChronicleAlert alert)
    {
        var targetScreen = alert.Category switch
        {
            ChronicleCandidateCategory.Conflict => PlayerScreen.KnownPolities,
            ChronicleCandidateCategory.Settlement => PlayerScreen.Regions,
            ChronicleCandidateCategory.Territory => PlayerScreen.Regions,
            _ => PlayerScreen.Polity
        };
        var cause = alert.Category switch
        {
            ChronicleCandidateCategory.Survival => "Current living conditions remain under strain.",
            ChronicleCandidateCategory.Conflict => "Outside pressure remains active.",
            ChronicleCandidateCategory.Territory => "Movement pressure is still unresolved.",
            ChronicleCandidateCategory.Settlement => "Settlement support is weak right now.",
            _ => "This condition is still active."
        };
        var impact = alert.Category switch
        {
            ChronicleCandidateCategory.Survival => "If it persists, losses, migration, or instability can follow.",
            ChronicleCandidateCategory.Conflict => "Further raids or conflict can damage cohesion and stores.",
            ChronicleCandidateCategory.Territory => "Continued pressure can push movement or territorial loss.",
            ChronicleCandidateCategory.Settlement => "A vulnerable settlement is easier to lose or abandon.",
            _ => "Continued strain can worsen the polity's position."
        };

        return new ChronicleUrgentItem(
            $"{alert.PolityId}:{alert.DedupeKey}",
            alert.Message,
            cause,
            impact,
            targetScreen,
            null);
    }
}
