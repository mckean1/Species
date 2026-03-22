namespace Species.Domain.Constants;

public static class SpeciesAwarenessConstants
{
    public const float StageThreshold = 100.0f;
    public const float EncounterMonthlyGainCap = 10.0f;
    public const float DiscoveryMonthlyGainCap = 7.0f;
    public const float EncounterMonthlyDecay = 0.75f;
    public const float DiscoveryMonthlyDecay = 0.75f;
    // Only discovery unlocks reliable intentional use. Encounter remains awareness-only.
    public const float DiscoveryUsabilityMultiplier = 1.00f;
    public const float MinimumContactScoreForEncounter = 0.08f;
    public const int MinimumPresenceContactsForDiscovery = 2;
}
