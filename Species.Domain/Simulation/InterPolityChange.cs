namespace Species.Domain.Simulation;

public sealed class InterPolityChange
{
    public required string PrimaryPolityId { get; init; }

    public required string PrimaryPolityName { get; init; }

    public required string OtherPolityId { get; init; }

    public required string OtherPolityName { get; init; }

    public required string Kind { get; init; }

    public required string Message { get; init; }
}
