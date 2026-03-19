namespace Species.Domain.Models;

public sealed class PressureState
{
    public int FoodPressure { get; set; }

    public int WaterPressure { get; set; }

    public int ThreatPressure { get; set; }

    public int OvercrowdingPressure { get; set; }

    public int MigrationPressure { get; set; }
}
