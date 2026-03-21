using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class SapientSpeciesDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public SpeciesClass SpeciesClass { get; init; } = SpeciesClass.Sapient;

    public string EmergentFromFaunaSpeciesId { get; init; } = string.Empty;

    public string ParentSpeciesId { get; init; } = string.Empty;

    public string OriginRegionId { get; init; } = string.Empty;
}
