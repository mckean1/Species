using Species.Domain.Enums;
using Species.Domain.Discovery;

namespace Species.Domain.Models;

public sealed class PolitySpeciesAwarenessState
{
    public string SpeciesId { get; init; } = string.Empty;

    public SpeciesClass SpeciesClass { get; init; }

    public float EncounterProgress { get; set; }

    public float DiscoveryProgress { get; set; }

    public DiscoveryStage CurrentStage =>
        DiscoveryProgress >= 100.0f ? DiscoveryStage.Discovered :
        EncounterProgress >= 100.0f ? DiscoveryStage.Encountered :
        DiscoveryStage.Unknown;

    public PolitySpeciesAwarenessState Clone()
    {
        return new PolitySpeciesAwarenessState
        {
            SpeciesId = SpeciesId,
            SpeciesClass = SpeciesClass,
            EncounterProgress = EncounterProgress,
            DiscoveryProgress = DiscoveryProgress
        };
    }
}
