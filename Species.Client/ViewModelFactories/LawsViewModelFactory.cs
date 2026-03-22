using Species.Domain.Models;
using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;

namespace Species.Client.ViewModelFactories;

public static class LawsViewModelFactory
{
    public static LawsViewModel Build(
        World world,
        string focalPolityId,
        int selectedIndex,
        bool isSimulationRunning = false,
        bool isActionMenuOpen = false,
        int selectedActionIndex = 0)
    {
        var focusPolity = PlayerFocus.Resolve(world, focalPolityId);
        var polityName = focusPolity?.Name ?? "Unknown polity";
        var items = BuildItems(focusPolity);
        var pendingDecisions = items.Where(item => item.Status == Species.Domain.Enums.LawProposalStatus.Active).ToArray();
        var recentDecisions = items.Where(item => item.Status != Species.Domain.Enums.LawProposalStatus.Active).ToArray();
        var clampedIndex = items.Count == 0 ? 0 : Math.Clamp(selectedIndex, 0, items.Count - 1);
        var selected = items.Count == 0 ? null : items[clampedIndex];

        return new LawsViewModel(
            polityName,
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            isSimulationRunning,
            items,
            pendingDecisions,
            recentDecisions,
            selected,
            clampedIndex,
            focusPolity is not null && focusPolity.ActiveLawProposal is not null,
            selected?.Status == Species.Domain.Enums.LawProposalStatus.Active,
            isActionMenuOpen,
            selectedActionIndex,
            BuildGovernanceSummary(focusPolity),
            BuildEnactedLaws(focusPolity),
            [
                "Pending decisions use Pass or Veto once selected.",
                "Proposal backing reflects blocs, governance strain, and current material or frontier pressures.",
                "Passed proposals become active laws whose strength can diverge between core and periphery.",
                "Law status shows whether an enacted order is holding, contested, or failing."
            ],
            items.Count == 0
                ? ["No law proposal is active right now."]
                : ["Recent law outcomes remain listed after they resolve."]);
    }

    private static IReadOnlyList<LawScreenItem> BuildItems(Polity? focusPolity)
    {
        if (focusPolity is null)
        {
            return Array.Empty<LawScreenItem>();
        }

        var items = new List<LawScreenItem>();
        if (focusPolity.ActiveLawProposal is not null)
        {
            items.Add(ToItem(focusPolity.ActiveLawProposal));
        }

        items.AddRange(focusPolity.LawProposalHistory
            .TakeLast(4)
            .Reverse<LawProposal>()
            .Select(ToItem));

        return items;
    }

    private static LawScreenItem ToItem(LawProposal proposal)
    {
        return new LawScreenItem(
            proposal.Id,
            proposal.Title,
            proposal.Status,
            PolityPresentation.DescribeLawCategory(proposal.Category),
            proposal.Summary,
            proposal.ReasonSummary,
            proposal.TradeoffSummary,
            PolityPresentation.DescribeBackingSources(proposal.PrimaryBackingSource, proposal.SecondaryBackingSource),
            proposal.Support,
            proposal.Opposition,
            proposal.Urgency,
            proposal.AgeInMonths,
            proposal.IgnoredMonths,
            proposal.ImpactScale);
    }

    private static IReadOnlyList<EnactedLawScreenItem> BuildEnactedLaws(Polity? focusPolity)
    {
        if (focusPolity is null)
        {
            return Array.Empty<EnactedLawScreenItem>();
        }

        return focusPolity.EnactedLaws
            .Where(law => law.IsActive)
            .OrderByDescending(law => law.EnactedOnYear)
            .ThenByDescending(law => law.EnactedOnMonth)
            .ThenBy(law => law.Title, StringComparer.Ordinal)
            .Select(law => new EnactedLawScreenItem(
                law.DefinitionId,
                law.Title,
                PolityPresentation.DescribeLawCategory(law.Category),
                law.Summary,
                law.IntentSummary,
                law.TradeoffSummary,
                ResolveActiveLawState(law),
                law.ImpactScale,
                PolityPresentation.DescribeLawStrengthBand(law.EnforcementStrength),
                PolityPresentation.DescribeLawStrengthBand(law.ComplianceLevel),
                PolityPresentation.DescribeLawStrengthBand(law.CoreEffectiveness),
                PolityPresentation.DescribeLawStrengthBand(law.PeripheralEffectiveness),
                PolityPresentation.DescribeLawStrengthBand(law.ResistanceLevel)))
            .ToArray();
    }

    private static IReadOnlyList<string> BuildGovernanceSummary(Polity? focusPolity)
    {
        if (focusPolity is null)
        {
            return ["No governance data."];
        }

        return
        [
            $"Legitimacy: {focusPolity.Governance.Legitimacy} [{PolityPresentation.DescribeGovernanceBand(focusPolity.Governance.Legitimacy)}]",
            $"Cohesion: {focusPolity.Governance.Cohesion} [{PolityPresentation.DescribeGovernanceBand(focusPolity.Governance.Cohesion)}]",
            $"Authority: {focusPolity.Governance.Authority} [{PolityPresentation.DescribeGovernanceBand(focusPolity.Governance.Authority)}]",
            $"Governability: {focusPolity.Governance.Governability} [{PolityPresentation.DescribeGovernanceBand(focusPolity.Governance.Governability)}]",
            $"Peripheral strain: {focusPolity.Governance.PeripheralStrain}"
        ];
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

    private static string ResolveActiveLawState(EnactedLaw law)
    {
        if (law.ComplianceLevel < 25 ||
            law.EnforcementStrength < 25 ||
            law.PeripheralEffectiveness < 20 ||
            law.ResistanceLevel >= 75)
        {
            return "Failing";
        }

        if (law.ResistanceLevel >= 50 ||
            law.ComplianceLevel < 45 ||
            Math.Abs(law.CoreEffectiveness - law.PeripheralEffectiveness) >= 30)
        {
            return "Contested";
        }

        if (law.ResistanceLevel >= 30 ||
            law.EnforcementStrength < 60 ||
            law.ComplianceLevel < 60)
        {
            return "Under Pressure";
        }

        return "Active";
    }
}
