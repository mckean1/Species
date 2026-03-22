using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class PrimitiveSeedMetadata
{
    public PrimitiveSeedRole Role { get; init; }

    public int Priority { get; init; }

    public IReadOnlyList<TemperatureBand> SupportedTemperatureBands { get; init; } = Array.Empty<TemperatureBand>();

    public IReadOnlyList<TerrainRuggedness> SupportedTerrainRuggednesses { get; init; } = Array.Empty<TerrainRuggedness>();

    public IReadOnlyList<PrimitiveSeedRole> SupportedFoodRoles { get; init; } = Array.Empty<PrimitiveSeedRole>();
}
