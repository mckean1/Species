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

    public static string DescribeGovernanceBand(int value)
    {
        return value switch
        {
            >= 75 => "Strong",
            >= 55 => "Steady",
            >= 35 => "Strained",
            _ => "Weak"
        };
    }

    public static string DescribeAnchoringKind(PolityAnchoringKind anchoringKind)
    {
        return anchoringKind switch
        {
            PolityAnchoringKind.Mobile => "Mobile",
            PolityAnchoringKind.Seasonal => "Seasonal",
            PolityAnchoringKind.SemiRooted => "Semi-Rooted",
            PolityAnchoringKind.Anchored => "Anchored",
            _ => "Unknown"
        };
    }

    public static string DescribePresenceKind(PolityPresenceKind presenceKind)
    {
        return presenceKind switch
        {
            PolityPresenceKind.PassingThrough => "Passing Through",
            PolityPresenceKind.Seasonal => "Seasonal",
            PolityPresenceKind.Habitation => "Habitation",
            PolityPresenceKind.Core => "Core",
            _ => "Unknown"
        };
    }

    public static string DescribeSettlementType(SettlementType settlementType)
    {
        return settlementType switch
        {
            SettlementType.CampHub => "Camp Hub",
            SettlementType.SeasonalBase => "Seasonal Base",
            SettlementType.Village => "Village",
            _ => "Unknown"
        };
    }

    public static string DescribePoliticalScaleForm(PoliticalScaleForm form)
    {
        return form switch
        {
            PoliticalScaleForm.LocalPolity => "Local Polity",
            PoliticalScaleForm.RegionalState => "Regional State",
            PoliticalScaleForm.KingdomRealm => "Kingdom-Like Realm",
            PoliticalScaleForm.CompositeRealm => "Composite Realm",
            PoliticalScaleForm.EmpireLike => "Empire-Like State",
            PoliticalScaleForm.Fragmenting => "Fragmenting Realm",
            _ => "Unknown"
        };
    }

    public static string DescribePoliticalAttachmentKind(PoliticalAttachmentKind kind)
    {
        return kind switch
        {
            PoliticalAttachmentKind.DirectIntegration => "Direct Integration",
            PoliticalAttachmentKind.LooseAttachment => "Loose Attachment",
            PoliticalAttachmentKind.Subordinate => "Subordinate",
            PoliticalAttachmentKind.FederatedAttachment => "Federated Attachment",
            PoliticalAttachmentKind.BreakawaySuccessor => "Breakaway Successor",
            _ => "Unknown"
        };
    }
}
