namespace Species.Domain.Models;

public sealed class ExternalPressureState
{
    public int Threat { get; set; }

    public int Cooperation { get; set; }

    public int FrontierFriction { get; set; }

    public int RaidPressure { get; set; }

    public int HostileNeighborCount { get; set; }

    public string Summary { get; set; } = "No major outside pressure.";

    public ExternalPressureState Clone()
    {
        return new ExternalPressureState
        {
            Threat = Threat,
            Cooperation = Cooperation,
            FrontierFriction = FrontierFriction,
            RaidPressure = RaidPressure,
            HostileNeighborCount = HostileNeighborCount,
            Summary = Summary
        };
    }
}
