namespace Species.Domain.Models;

public sealed class GovernanceState
{
    public int Legitimacy { get; set; } = 50;

    public int Cohesion { get; set; } = 50;

    public int Authority { get; set; } = 50;

    public int Governability { get; set; } = 50;

    public int PeripheralStrain { get; set; } = 0;

    public GovernanceState Clone()
    {
        return new GovernanceState
        {
            Legitimacy = Legitimacy,
            Cohesion = Cohesion,
            Authority = Authority,
            Governability = Governability,
            PeripheralStrain = PeripheralStrain
        };
    }
}
