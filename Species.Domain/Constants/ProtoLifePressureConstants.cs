namespace Species.Domain.Constants;

public static class ProtoLifePressureConstants
{
    // Capacity-to-occupancy scaling. Higher values make existing populations fill less of the substrate.
    public const float FloraCapacityPopulationScale = 1200.0f;
    public const float FaunaCapacityPopulationScale = 720.0f;

    // Pressure damping. These are the main anti-hidden-respawn knobs.
    public const float FloraSaturationDragWeight = 0.30f;
    public const float FaunaSaturationDragWeight = 0.34f;
    public const float HarshnessDragWeight = 0.26f;
    public const float CollapseOpeningWeight = 0.14f;
    public const float FloraNeighborInfluenceWeight = 0.18f;
    public const float FaunaNeighborInfluenceWeight = 0.14f;
    public const float FloraRiseRate = 0.28f;
    public const float FloraDecayRate = 0.26f;
    public const float FaunaRiseRate = 0.22f;
    public const float FaunaDecayRate = 0.24f;

    // Proto-flora can slightly help fauna substrate, but active ecology should dominate.
    public const float ProtoFloraSupportContributionWeight = 0.12f;
    public const float RecentCollapseWindowMonths = 18.0f;
}
