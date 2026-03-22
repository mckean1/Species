namespace Species.Domain.Models;

/// <summary>
/// Represents the primitive organic capacity and presence in a region.
/// This is the canonical foundational biological state that exists immediately after physical world generation.
/// Unlike ProtoLifeSubstrate (which was a post-population calculation), PrimitiveLifeSubstrate
/// represents the actual primitive organic starting truth.
/// </summary>
public sealed class PrimitiveLifeSubstrate
{
    /// <summary>
    /// The region's long-run capacity for primitive plant-like life (0.0 to 1.0).
    /// Driven by fertility, water, temperature, terrain, and biome.
    /// </summary>
    public float PrimitiveFloraCapacity { get; init; }

    /// <summary>
    /// The region's long-run capacity for primitive animal-like life (0.0 to 1.0).
    /// Driven primarily by flora capacity, plus temperature, terrain, and biome.
    /// </summary>
    public float PrimitiveFaunaCapacity { get; init; }

    /// <summary>
    /// The current primitive flora presence strength (0.0 to 1.0).
    /// Reflects the normalized sum of primitive flora populations.
    /// </summary>
    public float PrimitiveFloraStrength { get; init; }

    /// <summary>
    /// The current primitive fauna presence strength (0.0 to 1.0).
    /// Reflects the normalized sum of primitive fauna populations.
    /// </summary>
    public float PrimitiveFaunaStrength { get; init; }

    public PrimitiveLifeSubstrate Clone()
    {
        return new PrimitiveLifeSubstrate
        {
            PrimitiveFloraCapacity = PrimitiveFloraCapacity,
            PrimitiveFaunaCapacity = PrimitiveFaunaCapacity,
            PrimitiveFloraStrength = PrimitiveFloraStrength,
            PrimitiveFaunaStrength = PrimitiveFaunaStrength
        };
    }
}
