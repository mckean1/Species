namespace Species.Domain.Simulation;

public sealed class SocialIdentityChange
{
    public required string PolityId { get; init; }

    public required string PolityName { get; init; }

    public required string TraditionId { get; init; }

    public required string TraditionName { get; init; }

    public required string Message { get; init; }
}
