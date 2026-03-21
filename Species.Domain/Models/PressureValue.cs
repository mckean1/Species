namespace Species.Domain.Models;

public sealed class PressureValue
{
    public int RawValue { get; set; }

    public int EffectiveValue { get; set; }

    public int DisplayValue { get; set; }

    public string SeverityLabel { get; set; } = "Inactive";

    public PressureValue Clone()
    {
        return new PressureValue
        {
            RawValue = RawValue,
            EffectiveValue = EffectiveValue,
            DisplayValue = DisplayValue,
            SeverityLabel = SeverityLabel
        };
    }
}
