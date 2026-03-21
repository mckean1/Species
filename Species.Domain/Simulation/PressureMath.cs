using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public static class PressureMath
{
    public static int MoveTowardZero(int value, int amount)
    {
        if (amount <= 0 || value == 0)
        {
            return value;
        }

        if (value > 0)
        {
            return Math.Max(0, value - amount);
        }

        return Math.Min(0, value + amount);
    }

    public static int ApplySafetyBounds(int rawValue, PressureDefinition definition)
    {
        return definition.Shape switch
        {
            PressureShape.OneSided => Math.Clamp(rawValue, 0, definition.SafetyBound),
            PressureShape.Signed => Math.Clamp(rawValue, -definition.SafetyBound, definition.SafetyBound),
            _ => rawValue
        };
    }

    public static int ComputeEffectiveValue(PressureDefinition definition, int rawValue)
    {
        var sign = Math.Sign(rawValue);
        var magnitude = Math.Abs(rawValue);
        var effectiveMagnitude = definition.CurveType switch
        {
            PressureCurveType.Persistent => CompressMagnitude(magnitude, 0.50, 0.25, 0.10),
            PressureCurveType.Transient => CompressMagnitude(magnitude, 0.40, 0.20, 0.08),
            _ => magnitude
        };

        return sign * effectiveMagnitude;
    }

    public static int ComputeDisplayValue(int effectiveValue)
    {
        var magnitude = Math.Abs(effectiveValue);
        int displayMagnitude;

        if (magnitude <= 40)
        {
            displayMagnitude = magnitude;
        }
        else if (magnitude <= 80)
        {
            displayMagnitude = 40 + (int)Math.Round((magnitude - 40) * 0.75, MidpointRounding.AwayFromZero);
        }
        else if (magnitude <= 120)
        {
            displayMagnitude = 70 + (int)Math.Round((magnitude - 80) * 0.50, MidpointRounding.AwayFromZero);
        }
        else
        {
            displayMagnitude = 90 + (int)Math.Round((magnitude - 120) * 0.10, MidpointRounding.AwayFromZero);
        }

        return Math.Clamp(displayMagnitude, 0, 100);
    }

    public static string ComputeSeverityLabel(int displayValue)
    {
        return displayValue switch
        {
            < 10 => "Inactive",
            < 25 => "Active",
            < 50 => "Moderate",
            < 75 => "Severe",
            _ => "Critical"
        };
    }

    public static PressureValue CreateValue(PressureDefinition definition, int rawValue)
    {
        var boundedRaw = ApplySafetyBounds(rawValue, definition);
        var effective = ComputeEffectiveValue(definition, boundedRaw);
        var display = ComputeDisplayValue(effective);
        return new PressureValue
        {
            RawValue = boundedRaw,
            EffectiveValue = effective,
            DisplayValue = display,
            SeverityLabel = ComputeSeverityLabel(display)
        };
    }

    public static PressureValue ApplyRawAdjustment(PressureDefinition definition, PressureValue currentValue, int rawDelta)
    {
        return CreateValue(definition, currentValue.RawValue + rawDelta);
    }

    private static int CompressMagnitude(int magnitude, double secondBandRate, double thirdBandRate, double tailRate)
    {
        if (magnitude <= 100)
        {
            return magnitude;
        }

        if (magnitude <= 200)
        {
            return 100 + (int)Math.Round((magnitude - 100) * secondBandRate, MidpointRounding.AwayFromZero);
        }

        if (magnitude <= 400)
        {
            return 100 +
                   (int)Math.Round(100 * secondBandRate, MidpointRounding.AwayFromZero) +
                   (int)Math.Round((magnitude - 200) * thirdBandRate, MidpointRounding.AwayFromZero);
        }

        return 100 +
               (int)Math.Round(100 * secondBandRate, MidpointRounding.AwayFromZero) +
               (int)Math.Round(200 * thirdBandRate, MidpointRounding.AwayFromZero) +
               (int)Math.Round((magnitude - 400) * tailRate, MidpointRounding.AwayFromZero);
    }
}
