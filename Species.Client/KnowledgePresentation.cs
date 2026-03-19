using Species.Domain.Knowledge;

public static class KnowledgePresentation
{
    public static string Describe(KnowledgeLevel level)
    {
        return level switch
        {
            KnowledgeLevel.Known => "Known",
            KnowledgeLevel.Partial => "Partially known",
            KnowledgeLevel.Rumored => "Rumored",
            _ => "Unknown"
        };
    }

    public static string DescribeRegionFamiliarity(RegionKnowledgeSnapshot snapshot)
    {
        return snapshot.OverallKnowledge switch
        {
            KnowledgeLevel.Known => "Known",
            KnowledgeLevel.Partial => "Partial",
            KnowledgeLevel.Rumored => "Rumored",
            _ => "Unknown"
        };
    }

    public static string DescribeWater(RegionKnowledgeSnapshot snapshot)
    {
        return snapshot.WaterKnowledge switch
        {
            KnowledgeLevel.Known => snapshot.WaterSupport switch
            {
                >= 85 => "Reliable water known",
                >= 45 => "Water known, but moderate",
                _ => "Water known to be scarce"
            },
            KnowledgeLevel.Partial => snapshot.WaterSupport switch
            {
                >= 70 => "Water seems steady",
                >= 40 => "Water availability uncertain",
                _ => "Water appears thin"
            },
            KnowledgeLevel.Rumored => "Rumored water access",
            _ => "Water not yet observed"
        };
    }

    public static string DescribeFoodSigns(KnowledgeLevel level, string knownLabel, string partialLabel, string rumoredLabel, string unknownLabel)
    {
        return level switch
        {
            KnowledgeLevel.Known => knownLabel,
            KnowledgeLevel.Partial => partialLabel,
            KnowledgeLevel.Rumored => rumoredLabel,
            _ => unknownLabel
        };
    }

    public static string DescribeThreat(RegionKnowledgeSnapshot snapshot)
    {
        return snapshot.FaunaKnowledge switch
        {
            KnowledgeLevel.Known => snapshot.ThreatPressure switch
            {
                >= 70 => "Predators known to be dangerous",
                >= 40 => "Predator risk is known",
                _ => "No major predator threat known"
            },
            KnowledgeLevel.Partial => snapshot.ThreatPressure switch
            {
                >= 70 => "Predator signs are strong",
                >= 40 => "Predator signs observed",
                _ => "Few predator signs observed"
            },
            KnowledgeLevel.Rumored => "Predator signs rumored",
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
