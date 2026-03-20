namespace Species.Domain.Simulation;

public sealed class PoliticalScaleChange
{
    public required string PolityId { get; init; }

    public required string PolityName { get; init; }

    public string OtherPolityId { get; init; } = string.Empty;

    public string OtherPolityName { get; init; } = string.Empty;

    public required string Kind { get; init; }

    public required string Message { get; init; }
}
