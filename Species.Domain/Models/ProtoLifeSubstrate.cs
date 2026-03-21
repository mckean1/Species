namespace Species.Domain.Models;

public sealed class ProtoLifeSubstrate
{
    // Capacity is the region's long-run ecological room for a biological class.
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
