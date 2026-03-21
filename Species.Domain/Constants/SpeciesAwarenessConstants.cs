namespace Species.Domain.Constants;

public static class SpeciesAwarenessConstants
{
    public const float StageThreshold = 100.0f;
    public const float EncounterMonthlyGainCap = 12.0f;
    public const float DiscoveryMonthlyGainCap = 9.0f;
    public const float KnowledgeMonthlyGainCap = 7.0f;
    public const float EncounterMonthlyDecay = 0.75f;
    public const float DiscoveryMonthlyDecay = 0.75f;
    public const float KnowledgeMonthlyDecay = 0.45f;
    // Only Knowledge unlocks reliable intentional use. Encounter and Discovery remain awareness-only states.
    public const float KnowledgeUsabilityMultiplier = 1.00f;
    public const float MinimumContactScoreForEncounter = 0.08f;
    public const int MinimumPresenceContactsForDiscovery = 2;
    public const int MinimumSuccessfulInteractionsForKnowledge = 2;
}
