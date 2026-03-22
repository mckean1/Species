using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Domain.Simulation;
using Species.Client.Enums;
using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;

namespace Species.Client.ViewModelFactories;

public static class AdvancementViewModelFactory
{
    public static int GetAdvancementCount(AdvancementCatalog advancementCatalog)
    {
        return advancementCatalog.Definitions.Count;
    }

    public static AdvancementsViewModel Build(
        World world,
        string focalPolityId,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        int selectedIndex,
        bool isSimulationRunning = false)
    {
        var focusPolity = PlayerFocus.Resolve(world, focalPolityId);
        var focusGroup = PlayerFocus.ResolveLeadGroup(world, focalPolityId);
        var focusContext = PlayerFocus.ResolveContext(world, focalPolityId);
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var region = focusGroup is null ? null : regionsById.GetValueOrDefault(focusGroup.CurrentRegionId);
        var items = advancementCatalog.Definitions
            .Select(definition => BuildItem(definition, focusPolity, focusGroup, focusContext, region, advancementCatalog, floraCatalog, faunaCatalog))
            .OrderBy(item => GetSortOrder(item.Status))
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();

        var clampedIndex = items.Length == 0
            ? 0
            : Math.Clamp(selectedIndex, 0, items.Length - 1);

        return new AdvancementsViewModel(
            focusPolity?.Name ?? "Unknown polity",
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            isSimulationRunning,
            items,
            items.Length == 0 ? null : items[clampedIndex],
            clampedIndex,
            items.Count(item => item.Status == AdvancementStatus.Completed),
            items.Count(item => item.Status == AdvancementStatus.Available),
            items.Count(item => item.Status == AdvancementStatus.Locked));
    }

    private static AdvancementScreenItem BuildItem(
        AdvancementDefinition definition,
        Polity? focusPolity,
        PopulationGroup? focusGroup,
        PolityContext? focusContext,
        Region? region,
        AdvancementCatalog advancementCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        if (focusGroup is null)
        {
            return new AdvancementScreenItem(
                definition.Id,
                definition.Name,
                definition.Category.ToString(),
                AdvancementStatus.Locked,
                definition.Description,
                definition.PracticalEffectSummary,
                "no polity data",
                ["Current polity state is unknown."],
                ["No polity is currently available to evaluate this advancement."],
                "No requirement data available.",
                "Unknown");
        }

        var requirement = BuildRequirement(definition, focusGroup, focusPolity, focusContext, region, floraCatalog, faunaCatalog);
        var learned = focusGroup.LearnedAdvancementIds.Contains(definition.Id);
        var status = learned
            ? AdvancementStatus.Completed
            : requirement.IsSatisfied
                ? AdvancementStatus.Available
                : AdvancementStatus.Locked;

        var notes = new List<string>();
        if (status == AdvancementStatus.Completed)
        {
            notes.Add("Already part of the polity's usable capabilities.");
        }
        else if (status == AdvancementStatus.Available)
        {
            notes.Add("The causal prerequisites are in place and learning can progress now.");
        }
        else
        {
            notes.Add(requirement.LockReason);
        }

        if (!string.IsNullOrWhiteSpace(definition.PrerequisiteSummary))
        {
            notes.Add(definition.PrerequisiteSummary);
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
        PopulationGroup group,
        Polity? polity,
        PolityContext? polityContext,
        Region? region,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var evaluation = FirstWaveAdvancementEvaluator.Evaluate(definition, group, polity, polityContext, region, floraCatalog, faunaCatalog);
        var capability = group.AdvancementEvidence.AdvancementProgressById.GetValueOrDefault(definition.Id);
        var adoption = group.AdvancementEvidence.AdoptionProgressById.GetValueOrDefault(definition.Id);
        var progressSummary = $"Opportunity: {evaluation.OpportunityCount}/{Math.Max(1, evaluation.RequiredOpportunityCount)}. Capability progress: {capability:0}/100. Adoption progress: {adoption:0}/100.";

        return new AdvancementRequirementInfo(
            evaluation.PrerequisitesMet,
            string.IsNullOrWhiteSpace(evaluation.LockReason) ? "Waiting on additional causal opportunity." : evaluation.LockReason,
            evaluation.Requirements,
            progressSummary,
            evaluation.ListHint,
            evaluation.StatusSummary);
    }

    private static int GetSortOrder(AdvancementStatus status)
    {
        return status switch
        {
            AdvancementStatus.Available => 0,
            AdvancementStatus.Locked => 1,
            AdvancementStatus.Completed => 2,
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
