using Species.Domain.Models;

public static class PlayerFocus
{
    public static PopulationGroup? Resolve(World world, string focalGroupId)
    {
        if (!string.IsNullOrWhiteSpace(focalGroupId))
        {
            var matchingGroup = world.PopulationGroups.FirstOrDefault(group =>
                string.Equals(group.Id, focalGroupId, StringComparison.Ordinal));

            if (matchingGroup is not null)
            {
                return matchingGroup;
            }
        }

        return SelectDefault(world);
    }

    public static string ResolveId(World world, string focalGroupId)
    {
        return Resolve(world, focalGroupId)?.Id ?? string.Empty;
    }

    private static PopulationGroup? SelectDefault(World world)
    {
        return world.PopulationGroups
            .OrderByDescending(group => group.Population)
            .ThenBy(group => group.Name, StringComparer.Ordinal)
            .ThenBy(group => group.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
