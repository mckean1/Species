namespace Species.Domain.Models;

public sealed class SocialTraditionDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Summary { get; init; }

    public required string IdentityChangeTemplate { get; init; }
}
