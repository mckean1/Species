using Species.Domain.Enums;
using Species.Domain.Knowledge;

namespace Species.Domain.Models;

public sealed class PolitySpeciesAwarenessState
{
    public string SpeciesId { get; init; } = string.Empty;

    public SpeciesClass SpeciesClass { get; init; }

    public float EncounterProgress { get; set; }

    public float DiscoveryProgress { get; set; }

    public float KnowledgeProgress { get; set; }

    public KnowledgeLevel CurrentLevel =>
        KnowledgeProgress >= 100.0f ? KnowledgeLevel.Knowledge :
        DiscoveryProgress >= 100.0f ? KnowledgeLevel.Discovery :
        EncounterProgress >= 100.0f ? KnowledgeLevel.Encounter :
        KnowledgeLevel.Unknown;

    public PolitySpeciesAwarenessState Clone()
    {
        return new PolitySpeciesAwarenessState
        {
            SpeciesId = SpeciesId,
            SpeciesClass = SpeciesClass,
            EncounterProgress = EncounterProgress,
            DiscoveryProgress = DiscoveryProgress,
            KnowledgeProgress = KnowledgeProgress
        };
    }
}
