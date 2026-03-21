using Species.Domain.Knowledge;

namespace Species.Client.Presentation;

public static class KnowledgePresentation
{
    public static string Describe(KnowledgeLevel level)
    {
        return level switch
        {
            KnowledgeLevel.Knowledge => "Knowledge",
            KnowledgeLevel.Discovery => "Discovery",
            KnowledgeLevel.Encounter => "Encounter",
            _ => "Unknown"
        };
    }

    public static string DescribeRegionFamiliarity(RegionKnowledgeSnapshot snapshot)
    {
        return snapshot.OverallKnowledge switch
        {
            KnowledgeLevel.Knowledge => "Knowledge",
            KnowledgeLevel.Discovery => "Discovery",
            KnowledgeLevel.Encounter => "Encounter",
            _ => "Unknown"
        };
    }

    public static string DescribeWater(RegionKnowledgeSnapshot snapshot)
    {
        return snapshot.WaterKnowledge switch
        {
            KnowledgeLevel.Knowledge => snapshot.WaterSupport switch
            {
                >= 85 => "Water is well known and reliable",
                >= 45 => "Water is known, but moderate",
                _ => "Water is known to be scarce"
            },
            KnowledgeLevel.Discovery => snapshot.WaterSupport switch
            {
                >= 70 => "Water sources have been discovered as steady",
                >= 40 => "Water sources are discovered, but uneven",
                _ => "Water sources are discovered as thin"
            },
            KnowledgeLevel.Encounter => "Water has been encountered, but remains uncertain",
            _ => "Water not yet observed"
        };
    }

    public static string DescribeFoodSigns(KnowledgeLevel level, string knowledgeLabel, string discoveryLabel, string encounterLabel, string unknownLabel)
    {
        return level switch
        {
            KnowledgeLevel.Knowledge => knowledgeLabel,
            KnowledgeLevel.Discovery => discoveryLabel,
            KnowledgeLevel.Encounter => encounterLabel,
            _ => unknownLabel
        };
    }

    public static string DescribeThreat(RegionKnowledgeSnapshot snapshot)
    {
        return snapshot.FaunaKnowledge switch
        {
            KnowledgeLevel.Knowledge => snapshot.ThreatPressure switch
            {
                >= 70 => "Predators are known to be dangerous",
                >= 40 => "Predator risk is known",
                _ => "No major predator threat is known"
            },
            KnowledgeLevel.Discovery => snapshot.ThreatPressure switch
            {
                >= 70 => "Predator danger has been discovered",
                >= 40 => "Predator signs have been discovered",
                _ => "Only light predator signs are discovered"
            },
            KnowledgeLevel.Encounter => "Predator signs have been encountered",
            _ => "Threats not yet observed"
        };
    }

    public static string ApproximatePopulation(int population, bool exactAllowed)
    {
        if (exactAllowed)
        {
            return population.ToString("N0");
        }

        return population switch
        {
            >= 120 => "large",
            >= 50 => "moderate",
            >= 1 => "small",
            _ => "unknown"
        };
    }
}
