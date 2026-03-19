using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class GroupPressureChange
{
    public required string GroupId { get; init; }

    public required string GroupName { get; init; }

    public required string CurrentRegionId { get; init; }

    public required string CurrentRegionName { get; init; }

    public required int Population { get; init; }

    public required int StoredFood { get; init; }

    public required PressureState Pressures { get; init; }

    public required string FoodPressureReason { get; init; }

    public required string WaterPressureReason { get; init; }

    public required string ThreatPressureReason { get; init; }

    public required string OvercrowdingPressureReason { get; init; }

    public required string MigrationPressureReason { get; init; }
}
