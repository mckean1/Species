using Species.Domain.Models;

public static class LawsScreenDataBuilder
{
    public static LawsScreenData Build(World world, string focalGroupId, int selectedIndex)
    {
        var focusGroup = PlayerFocus.Resolve(world, focalGroupId);
        var polityName = focusGroup?.Name ?? "Unknown polity";
        var items = BuildItems(focusGroup);
        var clampedIndex = items.Count == 0 ? 0 : Math.Clamp(selectedIndex, 0, items.Count - 1);
        var selected = items.Count == 0 ? null : items[clampedIndex];

        return new LawsScreenData(
            polityName,
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            items,
            selected,
            clampedIndex,
            focusGroup is not null && focusGroup.ActiveLawProposal is not null,
            BuildEnactedLaws(focusGroup),
            [
                "Only one active proposal can exist at a time.",
                "Proposal backing reflects current internal political blocs.",
                "Passed proposals become enacted laws with ongoing monthly effects.",
                "Ignored proposals can linger for years before they pass, fail, or fade."
            ],
            items.Count == 0
                ? ["No law proposal is active right now."]
                : ["Recent law outcomes remain listed after they resolve."]);
    }

    private static IReadOnlyList<LawScreenItem> BuildItems(PopulationGroup? focusGroup)
    {
        if (focusGroup is null)
        {
            return Array.Empty<LawScreenItem>();
        }

        var items = new List<LawScreenItem>();
        if (focusGroup.ActiveLawProposal is not null)
        {
            items.Add(ToItem(focusGroup.ActiveLawProposal));
        }

        items.AddRange(focusGroup.LawProposalHistory
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
            PolityPresentation.DescribeBackingSources(proposal.PrimaryBackingSource, proposal.SecondaryBackingSource),
            proposal.Support,
            proposal.Opposition,
            proposal.Urgency,
            proposal.AgeInMonths,
            proposal.IgnoredMonths,
            proposal.ImpactScale);
    }

    private static IReadOnlyList<EnactedLawScreenItem> BuildEnactedLaws(PopulationGroup? focusGroup)
    {
        if (focusGroup is null)
        {
            return Array.Empty<EnactedLawScreenItem>();
        }

        return focusGroup.EnactedLaws
            .Where(law => law.IsActive)
            .OrderByDescending(law => law.EnactedOnYear)
            .ThenByDescending(law => law.EnactedOnMonth)
            .ThenBy(law => law.Title, StringComparer.Ordinal)
            .Select(law => new EnactedLawScreenItem(
                law.DefinitionId,
                law.Title,
                PolityPresentation.DescribeLawCategory(law.Category),
                law.Summary,
                law.ImpactScale,
                PolityPresentation.DescribeLawStrengthBand(law.EnforcementStrength),
                PolityPresentation.DescribeLawStrengthBand(law.ComplianceLevel)))
            .ToArray();
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

public sealed record LawsScreenData(
    string PolityName,
    string CurrentDate,
    IReadOnlyList<LawScreenItem> Laws,
    LawScreenItem? SelectedLaw,
    int SelectedIndex,
    bool HasActiveProposal,
    IReadOnlyList<EnactedLawScreenItem> EnactedLaws,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> EmptyStateNotes);

public sealed record LawScreenItem(
    string Id,
    string Name,
    Species.Domain.Enums.LawProposalStatus Status,
    string Category,
    string Summary,
    string BackedBy,
    int Support,
    int Opposition,
    int Urgency,
    int AgeInMonths,
    int IgnoredMonths,
    int ImpactScale);

public sealed record EnactedLawScreenItem(
    string Id,
    string Name,
    string Category,
    string Summary,
    int ImpactScale,
    string Enforcement,
    string Compliance);
