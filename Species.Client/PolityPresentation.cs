using Species.Domain.Enums;

public static class PolityPresentation
{
    public static string DescribeGovernmentForm(GovernmentForm governmentForm)
    {
        return governmentForm switch
        {
            GovernmentForm.TribalClanRule => "Tribal / Clan Rule",
            GovernmentForm.Confederation => "Confederation",
            GovernmentForm.CouncilRule => "Council Rule",
            GovernmentForm.MerchantRule => "Merchant Rule",
            GovernmentForm.FeudalRule => "Feudal Rule",
            GovernmentForm.ImperialBureaucracy => "Imperial Bureaucracy",
            GovernmentForm.Republic => "Republic",
            GovernmentForm.AbsoluteRule => "Absolute Rule",
            GovernmentForm.DespoticRule => "Despotic Rule",
            GovernmentForm.Theocracy => "Theocracy",
            _ => "Unknown"
        };
    }

    public static string DescribeLawCategory(LawProposalCategory category)
    {
        return category.ToString();
    }

    public static string DescribeLawStatus(LawProposalStatus status)
    {
        return status switch
        {
            LawProposalStatus.Active => "Active",
            LawProposalStatus.Passed => "Passed",
            LawProposalStatus.Vetoed => "Vetoed",
            LawProposalStatus.Abstained => "Abstained",
            _ => "Unknown"
        };
    }

    public static string DescribeBackingSource(ProposalBackingSource source)
    {
        return source switch
        {
            ProposalBackingSource.Priests => "Priesthood",
            ProposalBackingSource.Warriors => "Warrior Elite",
            ProposalBackingSource.Merchants => "Merchant Interests",
            ProposalBackingSource.Elders => "Elders / Traditionalists",
            ProposalBackingSource.CommonFolk => "Common Folk",
            ProposalBackingSource.FrontierSettlers => "Frontier Interests",
            _ => "Unknown"
        };
    }

    public static string DescribeBackingSources(ProposalBackingSource primary, ProposalBackingSource? secondary)
    {
        var primaryLabel = DescribeBackingSource(primary);
        if (secondary is null)
        {
            return primaryLabel;
        }

        return $"{primaryLabel} and {DescribeBackingSource(secondary.Value)}";
    }

    public static string DescribeLawStrengthBand(int value)
    {
        return value switch
        {
            >= 67 => "High",
            >= 34 => "Medium",
            _ => "Low"
        };
    }
}
