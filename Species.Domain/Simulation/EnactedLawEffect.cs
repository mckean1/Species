namespace Species.Domain.Simulation;

public sealed record EnactedLawEffect(
    int FoodPressureModifier,
    int WaterPressureModifier,
    int ThreatPressureModifier,
    int OvercrowdingPressureModifier,
    int MigrationPressureModifier);
