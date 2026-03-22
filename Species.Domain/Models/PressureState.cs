using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class PressureState
{
    public PressureValue Food { get; set; } = new();

    public PressureValue Water { get; set; } = new();

    public PressureValue Threat { get; set; } = new();

    public PressureValue Overcrowding { get; set; } = new();

    public PressureValue Migration { get; set; } = new();

    public PressureValue Curiosity { get; set; } = new();

    public int FoodPressure => Food.DisplayValue;

    public int WaterPressure => Water.DisplayValue;

    public int ThreatPressure => Threat.DisplayValue;

    public int OvercrowdingPressure => Overcrowding.DisplayValue;

    public int MigrationPressure => Migration.DisplayValue;

    public int CuriosityPressure => Curiosity.DisplayValue;

    public PressureValue Get(PressureCategory category)
    {
        return category switch
        {
            PressureCategory.Food => Food,
            PressureCategory.Water => Water,
            PressureCategory.Threat => Threat,
            PressureCategory.Overcrowding => Overcrowding,
            PressureCategory.Migration => Migration,
            PressureCategory.Curiosity => Curiosity,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }

    public void Set(PressureCategory category, PressureValue value)
    {
        switch (category)
        {
            case PressureCategory.Food:
                Food = value;
                break;
            case PressureCategory.Water:
                Water = value;
                break;
            case PressureCategory.Threat:
                Threat = value;
                break;
            case PressureCategory.Overcrowding:
                Overcrowding = value;
                break;
            case PressureCategory.Migration:
                Migration = value;
                break;
            case PressureCategory.Curiosity:
                Curiosity = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category), category, null);
        }
    }

    public PressureState Clone()
    {
        return new PressureState
        {
            Food = Food.Clone(),
            Water = Water.Clone(),
            Threat = Threat.Clone(),
            Overcrowding = Overcrowding.Clone(),
            Migration = Migration.Clone(),
            Curiosity = Curiosity.Clone()
        };
    }
}
