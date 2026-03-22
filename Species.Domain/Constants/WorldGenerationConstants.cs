namespace Species.Domain.Constants;

public static class WorldGenerationConstants
{
    public const int DefaultRegionCount = 12;
    public const int MinimumRegionCount = 3;
    public const int TargetNeighborCount = 3;
    public const int MaximumLocalNeighborDistance = 3;
    public const double MinimumFertility = 0.05;
    public const double MaximumFertility = 0.95;
}
