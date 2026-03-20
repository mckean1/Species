namespace Species.Domain.Models;

public sealed class BiologicalTraitProfile
{
    public int ColdTolerance { get; set; } = 50;

    public int HeatTolerance { get; set; } = 50;

    public int DroughtTolerance { get; set; } = 50;

    public int Flexibility { get; set; } = 50;

    public int BodySize { get; set; } = 50;

    public int Reproduction { get; set; } = 50;

    public int Mobility { get; set; } = 50;

    public int Defense { get; set; } = 50;

    public int Resilience { get; set; } = 50;

    public BiologicalTraitProfile Clone()
    {
        return new BiologicalTraitProfile
        {
            ColdTolerance = ColdTolerance,
            HeatTolerance = HeatTolerance,
            DroughtTolerance = DroughtTolerance,
            Flexibility = Flexibility,
            BodySize = BodySize,
            Reproduction = Reproduction,
            Mobility = Mobility,
            Defense = Defense,
            Resilience = Resilience
        };
    }

    public int DistanceFrom(BiologicalTraitProfile other)
    {
        return Math.Abs(ColdTolerance - other.ColdTolerance) +
               Math.Abs(HeatTolerance - other.HeatTolerance) +
               Math.Abs(DroughtTolerance - other.DroughtTolerance) +
               Math.Abs(Flexibility - other.Flexibility) +
               Math.Abs(BodySize - other.BodySize) +
               Math.Abs(Reproduction - other.Reproduction) +
               Math.Abs(Mobility - other.Mobility) +
               Math.Abs(Defense - other.Defense) +
               Math.Abs(Resilience - other.Resilience);
    }

    public string ToSummary()
    {
        var highlights = new List<string>();
        if (ColdTolerance >= 60)
        {
            highlights.Add("cold-hardy");
        }

        if (HeatTolerance >= 60)
        {
            highlights.Add("heat-tolerant");
        }

        if (DroughtTolerance >= 60)
        {
            highlights.Add("dry-adapted");
        }

        if (Flexibility >= 60)
        {
            highlights.Add("flexible-feeding");
        }

        if (BodySize >= 60)
        {
            highlights.Add("large-bodied");
        }

        if (Mobility >= 60)
        {
            highlights.Add("far-ranging");
        }

        if (Defense >= 60)
        {
            highlights.Add("well-defended");
        }

        if (Resilience >= 60)
        {
            highlights.Add("robust");
        }

        return highlights.Count == 0 ? "mixed traits" : string.Join(", ", highlights.Take(3));
    }
}
