namespace Species.Client.Models;

public sealed class LawScreenItem
{
    public LawScreenItem(
        string id,
        string name,
        Domain.Enums.LawProposalStatus status,
        string category,
        string summary,
        string reasonSummary,
        string tradeoffSummary,
        string backedBy,
        int support,
        int opposition,
        int urgency,
        int ageInMonths,
        int ignoredMonths,
        int impactScale)
    {
        Id = id;
        Name = name;
        Status = status;
        Category = category;
        Summary = summary;
        ReasonSummary = reasonSummary;
        TradeoffSummary = tradeoffSummary;
        BackedBy = backedBy;
        Support = support;
        Opposition = opposition;
        Urgency = urgency;
        AgeInMonths = ageInMonths;
        IgnoredMonths = ignoredMonths;
        ImpactScale = impactScale;
    }

    public string Id { get; }

    public string Name { get; }

    public Domain.Enums.LawProposalStatus Status { get; }

    public string Category { get; }

    public string Summary { get; }

    public string ReasonSummary { get; }

    public string TradeoffSummary { get; }

    public string BackedBy { get; }

    public int Support { get; }

    public int Opposition { get; }

    public int Urgency { get; }

    public int AgeInMonths { get; }

    public int IgnoredMonths { get; }

    public int ImpactScale { get; }
}
