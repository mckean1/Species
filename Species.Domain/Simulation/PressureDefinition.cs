using Species.Domain.Enums;

namespace Species.Domain.Simulation;

public sealed record PressureDefinition(
    PressureCategory Category,
    PressureShape Shape,
    PressureCurveType CurveType,
    PressureDecayMode DecayMode,
    int DecayRate,
    int SafetyBound);
