namespace Species.Domain.Models;

public sealed class MaterialProductionState
{
    public int ShelterSupport { get; set; }

    public int StorageSupport { get; set; }

    public int ToolSupport { get; set; }

    public int TextileSupport { get; set; }

    public int ResilienceBonus { get; set; }

    public int SurplusScore { get; set; }

    public int DeficitScore { get; set; }

    public string ConditionSummary { get; set; } = "Material conditions are steady.";

    public MaterialProductionState Clone()
    {
        return new MaterialProductionState
        {
            ShelterSupport = ShelterSupport,
            StorageSupport = StorageSupport,
            ToolSupport = ToolSupport,
            TextileSupport = TextileSupport,
            ResilienceBonus = ResilienceBonus,
            SurplusScore = SurplusScore,
            DeficitScore = DeficitScore,
            ConditionSummary = ConditionSummary
        };
    }
}
