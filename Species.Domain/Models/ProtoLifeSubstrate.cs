namespace Species.Domain.Models;

/// <summary>
/// Represents the ongoing ecological pressure and genesis state during simulation.
/// This is the active simulation substrate model that evolves during gameplay.
/// 
/// Purpose: Tracks ecological pressure, vacancy, and genesis conditions for species emergence.
/// 
/// Relationship to PrimitiveLifeSubstrate:
/// - PrimitiveLifeSubstrate: Initial primitive-world snapshot (WG-3 output)
/// - ProtoLifeSubstrate: Active simulation state that evolves during gameplay
/// 
/// Both substrates serve different purposes and coexist:
/// - ProtoLifeSubstrate capacity values are initialized from PrimitiveLifeSubstrate
/// - During simulation, ProtoLifeSubstrate pressure and genesis fields evolve via ProtoPressureSystem
/// - ProtoLifeSubstrate drives ongoing ecological development and species emergence
/// 
/// Note: ProtoLifeSubstrate is NOT legacy or transitional. It is the canonical simulation state.
/// </summary>
public sealed class ProtoLifeSubstrate
{
    // Capacity is the region's long-run ecological room for a biological class.
    // Initialized from PrimitiveLifeSubstrate canonical capacity after WG-3.
    // Pressure is the current monthly substrate push for future recolonization/genesis conditions.
    public float ProtoFloraCapacity { get; init; }

    public float ProtoFaunaCapacity { get; init; }

    public float ProtoFloraPressure { get; init; }

    public float ProtoFaunaPressure { get; init; }

    public float FloraOccupancyDeficit { get; init; }

    public float FaunaOccupancyDeficit { get; init; }

    public float FloraSupportDeficit { get; init; }

    public float FaunaSupportDeficit { get; init; }

    public float EcologicalVacancy { get; init; }

    public float RecentCollapseOpening { get; init; }

    public int ProtoFloraReadinessMonths { get; init; }

    public int ProtoFaunaReadinessMonths { get; init; }

    public int ProtoFloraCandidateMonths { get; init; }

    public int ProtoFaunaCandidateMonths { get; init; }

    public int ProtoFloraGenesisCooldownMonths { get; init; }

    public int ProtoFaunaGenesisCooldownMonths { get; init; }

    public ProtoLifeSubstrate Clone()
    {
        return new ProtoLifeSubstrate
        {
            ProtoFloraCapacity = ProtoFloraCapacity,
            ProtoFaunaCapacity = ProtoFaunaCapacity,
            ProtoFloraPressure = ProtoFloraPressure,
            ProtoFaunaPressure = ProtoFaunaPressure,
            FloraOccupancyDeficit = FloraOccupancyDeficit,
            FaunaOccupancyDeficit = FaunaOccupancyDeficit,
            FloraSupportDeficit = FloraSupportDeficit,
            FaunaSupportDeficit = FaunaSupportDeficit,
            EcologicalVacancy = EcologicalVacancy,
            RecentCollapseOpening = RecentCollapseOpening,
            ProtoFloraReadinessMonths = ProtoFloraReadinessMonths,
            ProtoFaunaReadinessMonths = ProtoFaunaReadinessMonths,
            ProtoFloraCandidateMonths = ProtoFloraCandidateMonths,
            ProtoFaunaCandidateMonths = ProtoFaunaCandidateMonths,
            ProtoFloraGenesisCooldownMonths = ProtoFloraGenesisCooldownMonths,
            ProtoFaunaGenesisCooldownMonths = ProtoFaunaGenesisCooldownMonths
        };
    }
}
