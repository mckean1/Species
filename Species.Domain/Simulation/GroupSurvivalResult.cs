using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class GroupSurvivalResult
{
    public GroupSurvivalResult(World world, IReadOnlyList<GroupSurvivalChange> changes)
    {
        World = world;
        Changes = changes;
    }

    public World World { get; }

    public IReadOnlyList<GroupSurvivalChange> Changes { get; }
}
