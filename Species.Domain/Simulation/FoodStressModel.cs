using Species.Domain.Enums;

namespace Species.Domain.Simulation;

public static class FoodStressModel
{
    public static float ResolveHungerPressure(float currentHungerPressure, float usableFoodRatio, float riseRate, float decayRate)
    {
        var targetHunger = 1.0f - Math.Clamp(usableFoodRatio, 0.0f, 1.0f);
        var rate = targetHunger >= currentHungerPressure ? riseRate : decayRate;
        return ClampNormalized(currentHungerPressure + ((targetHunger - currentHungerPressure) * rate));
    }

    public static int ResolveShortageMonths(int currentShortageMonths, float usableFoodRatio)
    {
        if (usableFoodRatio >= 0.97f)
        {
            return Math.Max(0, currentShortageMonths - 1);
        }

        if (usableFoodRatio <= 0.05f)
        {
            return currentShortageMonths + 2;
        }

        return currentShortageMonths + 1;
    }

    public static FoodStressState ResolveState(float usableFoodRatio, float hungerPressure, int shortageMonths)
    {
        if (usableFoodRatio <= 0.05f || hungerPressure >= 0.88f || shortageMonths >= 4)
        {
            return FoodStressState.Starvation;
        }

        if (usableFoodRatio < 0.60f || hungerPressure >= 0.55f || shortageMonths >= 2)
        {
            return FoodStressState.SevereShortage;
        }

        if (usableFoodRatio < 0.97f || hungerPressure >= 0.18f)
        {
            return FoodStressState.HungerPressure;
        }

        return FoodStressState.FedStable;
    }

    private static float ClampNormalized(float value)
    {
        return MathF.Round(Math.Clamp(value, 0.0f, 1.0f), 2);
    }
}
