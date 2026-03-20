using Species.Domain.Enums;

namespace Species.Domain.Models;

public sealed class InterPolityRelation
{
    public string OtherPolityId { get; init; } = string.Empty;

    public InterPolityStance Stance { get; set; }

    public int ContactIntensity { get; set; }

    public int Trust { get; set; } = 50;

    public int Hostility { get; set; }

    public int Cooperation { get; set; }

    public int FrontierFriction { get; set; }

    public int Escalation { get; set; }

    public int RaidPressure { get; set; }

    public int RaidsInflicted { get; set; }

    public int RaidsSuffered { get; set; }

    public int CooperationMonths { get; set; }

    public int SharedThreatMonths { get; set; }

    public int PeaceMonths { get; set; }

    public string RecentSummary { get; set; } = string.Empty;

    public InterPolityRelation Clone()
    {
        return new InterPolityRelation
        {
            OtherPolityId = OtherPolityId,
            Stance = Stance,
            ContactIntensity = ContactIntensity,
            Trust = Trust,
            Hostility = Hostility,
            Cooperation = Cooperation,
            FrontierFriction = FrontierFriction,
            Escalation = Escalation,
            RaidPressure = RaidPressure,
            RaidsInflicted = RaidsInflicted,
            RaidsSuffered = RaidsSuffered,
            CooperationMonths = CooperationMonths,
            SharedThreatMonths = SharedThreatMonths,
            PeaceMonths = PeaceMonths,
            RecentSummary = RecentSummary
        };
    }
}
