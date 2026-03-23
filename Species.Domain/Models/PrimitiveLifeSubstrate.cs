namespace Species.Domain.Models;

/// <summary>
/// Represents the primitive organic capacity and presence in a region immediately after WG-3 seeding.
/// This is the canonical foundational biological state snapshot.
/// 
/// Purpose: Captures the initial post-WG-3 primitive-world state as a historical reference.
/// 
/// Relationship to ProtoLifeSubstrate:
/// - PrimitiveLifeSubstrate: Initial primitive-world snapshot (WG-3 output)
/// - ProtoLifeSubstrate: Active simulation state that evolves during gameplay
/// 
/// Both substrates serve different purposes and coexist:
/// - Use PrimitiveLifeSubstrate when you need the initial primitive-world baseline
/// - Use ProtoLifeSubstrate for ongoing ecological simulation and genesis mechanics
/// </summary>
public sealed class PrimitiveLifeSubstrate
{
    /// <summary>
    /// The region's long-run capacity for primitive plant-like life (0.0 to 1.0).
    /// Driven by fertility, water, temperature, terrain, and biome.
    /// This is the canonical capacity calculation source for both substrate models.
    /// </summary>
    public float PrimitiveFloraCapacity { get; init; }

    /// <summary>
    /// The region's long-run capacity for primitive animal-like life (0.0 to 1.0).
    /// Driven primarily by flora capacity, plus temperature, terrain, and biome.
    /// This is the canonical capacity calculation source for both substrate models.
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
