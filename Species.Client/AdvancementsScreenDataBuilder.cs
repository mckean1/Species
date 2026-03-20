using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Models;

public static class AdvancementsScreenDataBuilder
{
    public static AdvancementsScreenData Build(
        World world,
        string focalPolityId,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog,
        int selectedIndex)
    {
        var focusGroup = PlayerFocus.ResolveLeadGroup(world, focalPolityId);
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var items = advancementCatalog.Definitions
            .Select(definition => BuildItem(definition, focusGroup, regionsById, discoveryCatalog))
            .OrderBy(item => GetSortOrder(item.Status))
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();

        var clampedIndex = items.Length == 0
            ? 0
            : Math.Clamp(selectedIndex, 0, items.Length - 1);

        return new AdvancementsScreenData(
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            items,
            items.Length == 0 ? null : items[clampedIndex],
            clampedIndex,
            items.Count(item => item.Status == AdvancementScreenStatus.Completed),
            items.Count(item => item.Status == AdvancementScreenStatus.Available),
            items.Count(item => item.Status == AdvancementScreenStatus.Locked));
    }

    private static AdvancementScreenItem BuildItem(
        AdvancementDefinition definition,
        PopulationGroup? focusGroup,
        IReadOnlyDictionary<string, Region> regionsById,
        DiscoveryCatalog discoveryCatalog)
    {
        if (focusGroup is null)
        {
            return new AdvancementScreenItem(
                definition.Id,
                definition.Name,
                definition.Category.ToString(),
                AdvancementScreenStatus.Locked,
                definition.Description,
                definition.PracticalEffectSummary,
                "no polity data",
                ["Current polity state is unknown."],
                ["No polity is currently available to evaluate this advancement."],
                "No requirement data available.",
                "Unknown");
        }

        var currentRegion = regionsById.GetValueOrDefault(focusGroup.CurrentRegionId);
        var regionName = currentRegion?.Name ?? "Current region";
        var learned = focusGroup.LearnedAdvancementIds.Contains(definition.Id);
        var requirement = BuildRequirement(definition, focusGroup, discoveryCatalog, regionName);
        var status = learned
            ? AdvancementScreenStatus.Completed
            : requirement.IsSatisfied
                ? AdvancementScreenStatus.Available
                : AdvancementScreenStatus.Locked;

        var notes = new List<string>();
        if (status == AdvancementScreenStatus.Completed)
        {
            notes.Add("Already part of the polity's usable capabilities.");
        }
        else if (status == AdvancementScreenStatus.Available)
        {
            notes.Add("All known conditions are met for this advancement.");
        }
        else
        {
            notes.Add(requirement.LockReason);
        }

        return new AdvancementScreenItem(
            definition.Id,
            definition.Name,
            definition.Category.ToString(),
            status,
            definition.Description,
            definition.PracticalEffectSummary,
            requirement.ListHint,
            requirement.Requirements,
            notes,
            requirement.ProgressSummary,
            requirement.StatusSummary);
    }

    private static AdvancementRequirementInfo BuildRequirement(
        AdvancementDefinition definition,
        PopulationGroup focusGroup,
        DiscoveryCatalog discoveryCatalog,
        string regionName)
    {
        return definition.Id switch
        {
            AdvancementCatalog.ImprovedGatheringId => BuildGatheringRequirement(focusGroup, discoveryCatalog, regionName),
            AdvancementCatalog.ImprovedHuntingId => BuildHuntingRequirement(focusGroup, discoveryCatalog, regionName),
            AdvancementCatalog.FoodStorageId => BuildStorageRequirement(focusGroup),
            AdvancementCatalog.OrganizedTravelId => BuildTravelRequirement(focusGroup),
            AdvancementCatalog.LocalResourceUseId => BuildLocalResourceRequirement(focusGroup, discoveryCatalog, regionName),
            _ => new AdvancementRequirementInfo(
                false,
                "Requirements are not yet exposed.",
                ["Requirement details are not available."],
                string.Empty,
                "unknown requirements",
                "Requirements are not yet exposed.")
        };
    }

    private static AdvancementRequirementInfo BuildGatheringRequirement(
        PopulationGroup group,
        DiscoveryCatalog discoveryCatalog,
        string regionName)
    {
        var discoveryId = discoveryCatalog.GetLocalFloraDiscoveryId(group.CurrentRegionId);
        var knowsFlora = group.KnownDiscoveryIds.Contains(discoveryId);
        var progress = group.AdvancementEvidence.SuccessfulGatheringWithKnowledgeMonths;
        var progressSummary = $"Gathering after local flora knowledge: {progress}/{AdvancementConstants.ImprovedGatheringMonthsRequired} months.";
        var requirements = new List<string>
        {
            $"{(knowsFlora ? "Known" : "Missing")} discovery: {regionName} Flora",
            $"Need {AdvancementConstants.ImprovedGatheringMonthsRequired} successful gathering months with that knowledge."
        };

        if (!knowsFlora)
        {
            return new AdvancementRequirementInfo(
                false,
                $"Missing local flora knowledge for {regionName}.",
                requirements,
                progressSummary,
                "needs flora discovery",
                "Waiting on local flora discovery.");
        }

        return BuildEvidenceResult(
            progress,
            AdvancementConstants.ImprovedGatheringMonthsRequired,
            "Local flora knowledge is in place.",
            requirements,
            progressSummary,
            "flora-ready");
    }

    private static AdvancementRequirementInfo BuildHuntingRequirement(
        PopulationGroup group,
        DiscoveryCatalog discoveryCatalog,
        string regionName)
    {
        var discoveryId = discoveryCatalog.GetLocalFaunaDiscoveryId(group.CurrentRegionId);
        var knowsFauna = group.KnownDiscoveryIds.Contains(discoveryId);
        var progress = group.AdvancementEvidence.SuccessfulHuntingWithKnowledgeMonths;
        var progressSummary = $"Hunting after local fauna knowledge: {progress}/{AdvancementConstants.ImprovedHuntingMonthsRequired} months.";
        var requirements = new List<string>
        {
            $"{(knowsFauna ? "Known" : "Missing")} discovery: {regionName} Fauna",
            $"Need {AdvancementConstants.ImprovedHuntingMonthsRequired} successful hunting months with that knowledge."
        };

        if (!knowsFauna)
        {
            return new AdvancementRequirementInfo(
                false,
                $"Missing local fauna knowledge for {regionName}.",
                requirements,
                progressSummary,
                "needs fauna discovery",
                "Waiting on local fauna discovery.");
        }

        return BuildEvidenceResult(
            progress,
            AdvancementConstants.ImprovedHuntingMonthsRequired,
            "Local fauna knowledge is in place.",
            requirements,
            progressSummary,
            "fauna-ready");
    }

    private static AdvancementRequirementInfo BuildStorageRequirement(PopulationGroup group)
    {
        var progress = group.AdvancementEvidence.SurplusStoredFoodMonths;
        var progressSummary = $"Months with stored food on hand: {progress}/{AdvancementConstants.FoodStorageSurplusMonthsRequired}.";
        var requirements = new List<string>
        {
            $"Need {AdvancementConstants.FoodStorageSurplusMonthsRequired} months with stored food available."
        };

        return BuildEvidenceResult(
            progress,
            AdvancementConstants.FoodStorageSurplusMonthsRequired,
            "Repeated food surplus has been achieved.",
            requirements,
            progressSummary,
            "food surplus");
    }

    private static AdvancementRequirementInfo BuildTravelRequirement(PopulationGroup group)
    {
        var hasKnownRouteUse = group.AdvancementEvidence.KnownRouteTravelMonths > 0;
        var progress = group.AdvancementEvidence.KnownRouteTravelMonths;
        var progressSummary = $"Known-route travel months: {progress}/{AdvancementConstants.OrganizedTravelKnownRouteMonthsRequired}.";
        var requirements = new List<string>
        {
            $"{(hasKnownRouteUse ? "Has" : "Needs")} successful travel along a known route",
            $"Need {AdvancementConstants.OrganizedTravelKnownRouteMonthsRequired} known-route travel months."
        };

        if (!hasKnownRouteUse)
        {
            return new AdvancementRequirementInfo(
                false,
                "No known-route travel has been completed yet.",
                requirements,
                progressSummary,
                "needs route use",
                "Waiting on known-route travel.");
        }

        return BuildEvidenceResult(
            progress,
            AdvancementConstants.OrganizedTravelKnownRouteMonthsRequired,
            "Known-route travel has begun.",
            requirements,
            progressSummary,
            "route use");
    }

    private static AdvancementRequirementInfo BuildLocalResourceRequirement(
        PopulationGroup group,
        DiscoveryCatalog discoveryCatalog,
        string regionName)
    {
        var discoveryId = discoveryCatalog.GetLocalRegionConditionsDiscoveryId(group.CurrentRegionId);
        var knowsRegion = group.KnownDiscoveryIds.Contains(discoveryId);
        var progress = group.AdvancementEvidence.SuccessfulResidenceWithRegionKnowledgeMonths;
        var progressSummary = $"Stable months with local region knowledge: {progress}/{AdvancementConstants.LocalResourceUseMonthsRequired}.";
        var requirements = new List<string>
        {
            $"{(knowsRegion ? "Known" : "Missing")} discovery: {regionName} Conditions",
            $"Need {AdvancementConstants.LocalResourceUseMonthsRequired} successful residence months with that knowledge."
        };

        if (!knowsRegion)
        {
            return new AdvancementRequirementInfo(
                false,
                $"Missing local region knowledge for {regionName}.",
                requirements,
                progressSummary,
                "needs region discovery",
                "Waiting on local region knowledge.");
        }

        return BuildEvidenceResult(
            progress,
            AdvancementConstants.LocalResourceUseMonthsRequired,
            "Local region knowledge is in place.",
            requirements,
            progressSummary,
            "region-ready");
    }

    private static AdvancementRequirementInfo BuildEvidenceResult(
        int progress,
        int required,
        string prerequisiteMessage,
        IReadOnlyList<string> requirements,
        string progressSummary,
        string listHint)
    {
        if (progress >= required)
        {
            return new AdvancementRequirementInfo(
                true,
                "All known requirements are met.",
                requirements,
                progressSummary,
                listHint,
                "Ready to adopt.");
        }

        var remaining = required - progress;
        return new AdvancementRequirementInfo(
            false,
            $"{prerequisiteMessage} Needs {remaining} more month{(remaining == 1 ? string.Empty : "s")} of matching evidence.",
            requirements,
            progressSummary,
            listHint,
            "Building toward unlock.");
    }

    private static int GetSortOrder(AdvancementScreenStatus status)
    {
        return status switch
        {
            AdvancementScreenStatus.Available => 0,
            AdvancementScreenStatus.Locked => 1,
            AdvancementScreenStatus.Completed => 2,
            _ => 3
        };
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

public sealed record AdvancementsScreenData(
    string CurrentDate,
    IReadOnlyList<AdvancementScreenItem> Items,
    AdvancementScreenItem? SelectedItem,
    int SelectedIndex,
    int CompletedCount,
    int AvailableCount,
    int LockedCount);

public sealed record AdvancementScreenItem(
    string Id,
    string Name,
    string Category,
    AdvancementScreenStatus Status,
    string Description,
    string CapabilitySummary,
    string ListHint,
    IReadOnlyList<string> Requirements,
    IReadOnlyList<string> Notes,
    string ProgressSummary,
    string StatusSummary);

public sealed record AdvancementRequirementInfo(
    bool IsSatisfied,
    string LockReason,
    IReadOnlyList<string> Requirements,
    string ProgressSummary,
    string ListHint,
    string StatusSummary);

public enum AdvancementScreenStatus
{
    Completed,
    Available,
    Locked
}
