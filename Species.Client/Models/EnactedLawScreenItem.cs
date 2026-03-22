namespace Species.Client.Models;

public sealed class EnactedLawScreenItem
{
    public EnactedLawScreenItem(
        string id,
        string name,
        string category,
        string summary,
        string intentSummary,
        string tradeoffSummary,
        string state,
        int impactScale,
        string enforcement,
        string compliance,
        string coreEffectiveness,
        string peripheralEffectiveness,
        string resistance)
    {
        Id = id;
        Name = name;
        Category = category;
        Summary = summary;
        IntentSummary = intentSummary;
        TradeoffSummary = tradeoffSummary;
        State = state;
        ImpactScale = impactScale;
        Enforcement = enforcement;
        Compliance = compliance;
        CoreEffectiveness = coreEffectiveness;
        PeripheralEffectiveness = peripheralEffectiveness;
        Resistance = resistance;
    }

    public string Id { get; }

    public string Name { get; }

    public string Category { get; }

    public string Summary { get; }

    public string IntentSummary { get; }

    public string TradeoffSummary { get; }

    public string State { get; }

    public int ImpactScale { get; }

    public string Enforcement { get; }

    public string Compliance { get; }

    public string CoreEffectiveness { get; }

    public string PeripheralEffectiveness { get; }

    public string Resistance { get; }
}
