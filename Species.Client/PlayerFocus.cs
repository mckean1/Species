using Species.Domain.Models;
using Species.Domain.Simulation;

public static class PlayerFocus
{
    public static Polity? Resolve(World world, string focalPolityId)
    {
        return PolityData.Resolve(world, focalPolityId);
    }

    public static string ResolveId(World world, string focalPolityId)
    {
        return Resolve(world, focalPolityId)?.Id ?? string.Empty;
    }

    public static PopulationGroup? ResolveLeadGroup(World world, string focalPolityId)
    {
        var polity = Resolve(world, focalPolityId);
        return polity is null ? null : PolityData.BuildContext(world, polity)?.LeadGroup;
    }

    public static PolityContext? ResolveContext(World world, string focalPolityId)
    {
        var polity = Resolve(world, focalPolityId);
        return polity is null ? null : PolityData.BuildContext(world, polity);
    }
}
