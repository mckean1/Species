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

    public required PressureChangeDetail Food { get; init; }

    public required PressureChangeDetail Water { get; init; }

    public required PressureChangeDetail Threat { get; init; }

    public required PressureChangeDetail Overcrowding { get; init; }

    public required PressureChangeDetail Migration { get; init; }

    public string FoodPressureReason => Food.ReasonText;

    public string WaterPressureReason => Water.ReasonText;

    public string ThreatPressureReason => Threat.ReasonText;

    public string OvercrowdingPressureReason => Overcrowding.ReasonText;

    public string MigrationPressureReason => Migration.ReasonText;
}
