using Species.Domain.Catalogs;
using Species.Domain.Models;

namespace Species.Domain.Knowledge;

public sealed class GroupKnowledgeContext
{
    private const float UnknownBaselineMultiplier = 0.65f;
    private const float RumoredBaselineMultiplier = 0.90f;

    private readonly World world;
    private readonly PopulationGroup group;
    private readonly DiscoveryCatalog discoveryCatalog;
    private readonly FloraSpeciesCatalog floraCatalog;
    private readonly FaunaSpeciesCatalog faunaCatalog;
    private readonly Dictionary<string, Region> regionsById;
    private readonly float baselineGatheringPotentialFood;
    private readonly float baselineHuntingPotentialFood;
    private readonly float baselineWaterSupport;
    private readonly float baselineThreatPressure;

    private GroupKnowledgeContext(
        World world,
        PopulationGroup group,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        this.world = world;
        this.group = group;
        this.discoveryCatalog = discoveryCatalog;
        this.floraCatalog = floraCatalog;
        this.faunaCatalog = faunaCatalog;
        regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);

        var knownRegions = world.Regions
            .Where(region => group.KnownRegionIds.Contains(region.Id) || string.Equals(region.Id, group.CurrentRegionId, StringComparison.Ordinal))
            .ToArray();

        if (knownRegions.Length == 0)
        {
            baselineGatheringPotentialFood = Math.Max(1, SubsistenceSupportModel.CalculateMonthlyFoodNeed(group.Population) * 0.85f);
            baselineHuntingPotentialFood = Math.Max(1, SubsistenceSupportModel.CalculateMonthlyFoodNeed(group.Population) * 0.75f);
            baselineWaterSupport = 55.0f;
            baselineThreatPressure = 35.0f;
            return;
        }

        baselineGatheringPotentialFood = knownRegions.Average(region => SubsistenceSupportModel.CalculateGatheringPotentialFood(region, group, floraCatalog));
        baselineHuntingPotentialFood = knownRegions.Average(region => SubsistenceSupportModel.CalculateHuntingPotentialFood(region, group, faunaCatalog));
        baselineWaterSupport = (float)knownRegions.Average(region => SubsistenceSupportModel.ResolveWaterSupport(region.WaterAvailability));
        baselineThreatPressure = (float)knownRegions.Average(region => SubsistenceSupportModel.NormalizeThreatPressure(SubsistenceSupportModel.CalculateCarnivoreThreat(region, faunaCatalog)));
    }

    public static GroupKnowledgeContext Create(
        World world,
        PopulationGroup group,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        return new GroupKnowledgeContext(world, group, discoveryCatalog, floraCatalog, faunaCatalog);
    }

    public RegionKnowledgeSnapshot ObserveRegion(string regionId, string? fromRegionId = null)
    {
        return regionsById.TryGetValue(regionId, out var region)
            ? ObserveRegion(region, fromRegionId)
            : new RegionKnowledgeSnapshot(
                regionId,
                "Unknown",
                false,
                false,
                KnowledgeLevel.Unknown,
                KnowledgeLevel.Unknown,
                KnowledgeLevel.Unknown,
                KnowledgeLevel.Unknown,
                KnowledgeLevel.Unknown,
                KnowledgeLevel.Unknown,
                baselineGatheringPotentialFood * UnknownBaselineMultiplier,
                baselineHuntingPotentialFood * UnknownBaselineMultiplier,
                baselineWaterSupport * UnknownBaselineMultiplier,
                baselineThreatPressure * UnknownBaselineMultiplier,
                0,
                0,
                0,
                0);
    }

    public RegionKnowledgeSnapshot ObserveRegion(Region region, string? fromRegionId = null)
    {
        var isCurrentRegion = string.Equals(region.Id, group.CurrentRegionId, StringComparison.Ordinal);
        var isKnownRegion = isCurrentRegion || group.KnownRegionIds.Contains(region.Id);
        var monthsSpent = group.DiscoveryEvidence.MonthsSpentByRegionId.GetValueOrDefault(region.Id);
        var gatheringEvidence = group.DiscoveryEvidence.SuccessfulGatheringMonthsByRegionId.GetValueOrDefault(region.Id);
        var huntingEvidence = group.DiscoveryEvidence.SuccessfulHuntingMonthsByRegionId.GetValueOrDefault(region.Id);
        var waterEvidence = group.DiscoveryEvidence.WaterExposureMonthsByRegionId.GetValueOrDefault(region.Id);
        var hasObservation = isKnownRegion || monthsSpent > 0 || gatheringEvidence > 0 || huntingEvidence > 0 || waterEvidence > 0;

        var routeKnowledge = ResolveRouteKnowledge(region.Id, fromRegionId, isCurrentRegion, isKnownRegion);
        var floraKnowledge = ResolveFieldKnowledge(group.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalFloraDiscoveryId(region.Id)), hasObservation, gatheringEvidence);
        var faunaKnowledge = ResolveFieldKnowledge(group.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalFaunaDiscoveryId(region.Id)), hasObservation, huntingEvidence);
        var waterKnowledge = ResolveFieldKnowledge(group.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalWaterSourcesDiscoveryId(region.Id)), hasObservation, waterEvidence);
        var conditionsKnowledge = ResolveFieldKnowledge(group.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalRegionConditionsDiscoveryId(region.Id)), hasObservation, monthsSpent);

        var actualGatheringPotential = SubsistenceSupportModel.CalculateGatheringPotentialFood(region, group, floraCatalog);
        var actualHuntingPotential = SubsistenceSupportModel.CalculateHuntingPotentialFood(region, group, faunaCatalog);
        var actualWaterSupport = SubsistenceSupportModel.ResolveWaterSupport(region.WaterAvailability);
        var actualThreatPressure = SubsistenceSupportModel.NormalizeThreatPressure(SubsistenceSupportModel.CalculateCarnivoreThreat(region, faunaCatalog));
        var monthlyFoodNeed = SubsistenceSupportModel.CalculateMonthlyFoodNeed(group.Population);

        var snapshot = new RegionKnowledgeSnapshot(
            region.Id,
            region.Name,
            isKnownRegion,
            isCurrentRegion,
            ResolveOverallKnowledge(floraKnowledge, faunaKnowledge, waterKnowledge, conditionsKnowledge, routeKnowledge),
            floraKnowledge,
            faunaKnowledge,
            waterKnowledge,
            conditionsKnowledge,
            routeKnowledge,
            EstimatePotential(floraKnowledge, actualGatheringPotential, baselineGatheringPotentialFood, monthlyFoodNeed),
            EstimatePotential(faunaKnowledge, actualHuntingPotential, baselineHuntingPotentialFood, monthlyFoodNeed),
            EstimateScalar(waterKnowledge, actualWaterSupport, baselineWaterSupport),
            EstimateScalar(conditionsKnowledge == KnowledgeLevel.Unknown ? faunaKnowledge : conditionsKnowledge, actualThreatPressure, baselineThreatPressure),
            monthsSpent,
            gatheringEvidence,
            huntingEvidence,
            waterEvidence);

        return snapshot;
    }

    private KnowledgeLevel ResolveRouteKnowledge(string targetRegionId, string? fromRegionId, bool isCurrentRegion, bool isKnownRegion)
    {
        if (isCurrentRegion)
        {
            return KnowledgeLevel.Known;
        }

        if (string.IsNullOrWhiteSpace(fromRegionId))
        {
            return isKnownRegion ? KnowledgeLevel.Partial : KnowledgeLevel.Unknown;
        }

        if (group.KnownDiscoveryIds.Contains(discoveryCatalog.GetRouteDiscoveryId(fromRegionId, targetRegionId)))
        {
            return KnowledgeLevel.Known;
        }

        return isKnownRegion ? KnowledgeLevel.Partial : KnowledgeLevel.Rumored;
    }

    private static KnowledgeLevel ResolveFieldKnowledge(bool explicitlyKnown, bool hasObservation, int evidenceMonths)
    {
        if (explicitlyKnown)
        {
            return KnowledgeLevel.Known;
        }

        if (evidenceMonths > 0 || hasObservation)
        {
            return KnowledgeLevel.Partial;
        }

        return KnowledgeLevel.Unknown;
    }

    private static KnowledgeLevel ResolveOverallKnowledge(params KnowledgeLevel[] values)
    {
        if (values.Any(value => value == KnowledgeLevel.Known))
        {
            return values.All(value => value == KnowledgeLevel.Known)
                ? KnowledgeLevel.Known
                : KnowledgeLevel.Partial;
        }

        if (values.Any(value => value == KnowledgeLevel.Partial))
        {
            return KnowledgeLevel.Partial;
        }

        if (values.Any(value => value == KnowledgeLevel.Rumored))
        {
            return KnowledgeLevel.Rumored;
        }

        return KnowledgeLevel.Unknown;
    }

    private static float EstimatePotential(KnowledgeLevel level, float actual, float baseline, int monthlyFoodNeed)
    {
        return level switch
        {
            KnowledgeLevel.Known => actual,
            KnowledgeLevel.Partial => BucketPotential(actual, monthlyFoodNeed),
            KnowledgeLevel.Rumored => baseline * RumoredBaselineMultiplier,
            _ => baseline * UnknownBaselineMultiplier
        };
    }

    private static float BucketPotential(float actual, int monthlyFoodNeed)
    {
        if (monthlyFoodNeed <= 0)
        {
            return actual;
        }

        var ratio = actual / monthlyFoodNeed;
        return ratio switch
        {
            < 0.35f => monthlyFoodNeed * 0.25f,
            < 0.85f => monthlyFoodNeed * 0.70f,
            < 1.35f => monthlyFoodNeed * 1.10f,
            _ => monthlyFoodNeed * 1.60f
        };
    }

    private static float EstimateScalar(KnowledgeLevel level, float actual, float baseline)
    {
        return level switch
        {
            KnowledgeLevel.Known => actual,
            KnowledgeLevel.Partial => BucketScalar(actual),
            KnowledgeLevel.Rumored => baseline * RumoredBaselineMultiplier,
            _ => baseline * UnknownBaselineMultiplier
        };
    }

    private static float BucketScalar(float actual)
    {
        return actual switch
        {
            < 30.0f => 25.0f,
            < 65.0f => 55.0f,
            _ => 85.0f
        };
    }
}
