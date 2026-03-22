using Species.Domain.Constants;
using Species.Domain.Enums;

namespace Species.Domain.Simulation;

public static class PressureDefinitions
{
    private const int DefaultSafetyBound = 2000;

    public static readonly PressureDefinition Food = new(
        PressureCategory.Food,
        PressureShape.OneSided,
        PressureCurveType.Persistent,
        PressureDecayMode.PassiveTowardZero,
        PressureCalculationConstants.PersistentPressureDecayRate,
        DefaultSafetyBound);

    public static readonly PressureDefinition Water = new(
        PressureCategory.Water,
        PressureShape.OneSided,
        PressureCurveType.Persistent,
        PressureDecayMode.PassiveTowardZero,
        PressureCalculationConstants.PersistentPressureDecayRate,
        DefaultSafetyBound);

    public static readonly PressureDefinition Threat = new(
        PressureCategory.Threat,
        PressureShape.OneSided,
        PressureCurveType.Transient,
        PressureDecayMode.PassiveTowardZero,
        PressureCalculationConstants.TransientPressureDecayRate,
        DefaultSafetyBound);

    public static readonly PressureDefinition Overcrowding = new(
        PressureCategory.Overcrowding,
        PressureShape.OneSided,
        PressureCurveType.Persistent,
        PressureDecayMode.PassiveTowardZero,
        PressureCalculationConstants.PersistentPressureDecayRate,
        DefaultSafetyBound);

    public static readonly PressureDefinition Migration = new(
        PressureCategory.Migration,
        PressureShape.OneSided,
        PressureCurveType.Transient,
        PressureDecayMode.PassiveTowardZero,
        PressureCalculationConstants.MigrationPressureDecayRate,
        DefaultSafetyBound);

    public static readonly PressureDefinition Curiosity = new(
        PressureCategory.Curiosity,
        PressureShape.OneSided,
        PressureCurveType.Persistent,
        PressureDecayMode.PassiveTowardZero,
        PressureCalculationConstants.PersistentPressureDecayRate,
        DefaultSafetyBound);

    public static PressureDefinition Get(PressureCategory category)
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
}
