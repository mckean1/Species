namespace Species.Domain.Models;

public sealed class SocialIdentityState
{
    public int Rootedness { get; set; }

    public int Mobility { get; set; }

    public int Communalism { get; set; }

    public int AutonomyOrientation { get; set; }

    public int OrderOrientation { get; set; }

    public int FrontierDistinctiveness { get; set; }

    public List<string> TraditionIds { get; init; } = [];

    public SocialIdentityState Clone()
    {
        return new SocialIdentityState
        {
            Rootedness = Rootedness,
            Mobility = Mobility,
            Communalism = Communalism,
            AutonomyOrientation = AutonomyOrientation,
            OrderOrientation = OrderOrientation,
            FrontierDistinctiveness = FrontierDistinctiveness,
            TraditionIds = [.. TraditionIds]
        };
    }
}
