namespace Species.Domain.Models;

public sealed class PolityCuriosityState
{
    public int MonthsSinceLastNovelty { get; set; }

    public int StoredRawPressure { get; set; }

    public PolityCuriosityState Clone()
    {
        return new PolityCuriosityState
        {
            MonthsSinceLastNovelty = MonthsSinceLastNovelty,
            StoredRawPressure = StoredRawPressure
        };
    }
}
