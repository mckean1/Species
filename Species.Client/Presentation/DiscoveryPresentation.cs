using Species.Domain.Discovery;

namespace Species.Client.Presentation;

public static class DiscoveryPresentation
{
    public static string Describe(DiscoveryStage stage)
    {
        return stage switch
        {
            DiscoveryStage.Discovered => "Discovered",
            DiscoveryStage.Encountered => "Encountered",
            _ => "Unknown"
        };
    }

    public static string DescribeRegionFamiliarity(RegionDiscoverySnapshot snapshot)
    {
        if (snapshot.IsCurrentRegion)
        {
            return "Current region";
        }

        return Describe(snapshot.OverallStage);
    }

    public static string DescribeWater(RegionDiscoverySnapshot snapshot)
    {
        return snapshot.WaterStage switch
        {
            DiscoveryStage.Discovered => snapshot.WaterSupport switch
            {
                >= 85 => "Water sources discovered as reliable",
                >= 45 => "Water sources discovered, but moderate",
                _ => "Water sources discovered as scarce"
            },
            DiscoveryStage.Encountered => "Water has been encountered, but remains uncertain",
            _ => "Water not yet observed"
        };
    }

    public static string DescribeFoodSigns(DiscoveryStage stage, string discoveredLabel, string encounteredLabel, string unknownLabel)
    {
        return stage switch
        {
            DiscoveryStage.Discovered => discoveredLabel,
            DiscoveryStage.Encountered => encounteredLabel,
            _ => unknownLabel
        };
    }

    public static string DescribeThreat(RegionDiscoverySnapshot snapshot)
    {
        return snapshot.FaunaStage switch
        {
            DiscoveryStage.Discovered => snapshot.ThreatPressure switch
            {
                >= 70 => "Predators discovered as dangerous",
                >= 40 => "Predator risk discovered",
                _ => "No major predator threat discovered"
            },
            DiscoveryStage.Encountered => "Predator signs have been encountered",
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
