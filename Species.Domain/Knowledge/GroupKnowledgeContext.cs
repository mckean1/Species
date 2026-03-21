using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Knowledge;

public sealed class GroupKnowledgeContext
{
    private const float UnknownBaselineMultiplier = 0.65f;
    private const float EncounterBaselineMultiplier = 0.90f;

    private readonly World world;
    private readonly PopulationGroup group;
    private readonly DiscoveryCatalog discoveryCatalog;
    private readonly FloraSpeciesCatalog floraCatalog;
    private readonly FaunaSpeciesCatalog faunaCatalog;
    private readonly Dictionary<string, Region> regionsById;
    private readonly Dictionary<string, PolitySpeciesAwarenessState> speciesAwarenessByKey;
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
        speciesAwarenessByKey = world.Polities
            .FirstOrDefault(polity => string.Equals(polity.Id, group.PolityId, StringComparison.Ordinal))?
            .SpeciesAwareness
            .ToDictionary(state => BuildSpeciesKey(state.SpeciesClass, state.SpeciesId), StringComparer.Ordinal)
            ?? new Dictionary<string, PolitySpeciesAwarenessState>(StringComparer.Ordinal);

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
        var floraKnowledge = ResolveFieldKnowledge(group.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalFloraDiscoveryId(region.Id)), hasObservation, gatheringEvidence, isCurrentRegion);
        var faunaKnowledge = ResolveFieldKnowledge(group.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalFaunaDiscoveryId(region.Id)), hasObservation, huntingEvidence, isCurrentRegion);
        var waterKnowledge = ResolveFieldKnowledge(group.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalWaterSourcesDiscoveryId(region.Id)), hasObservation, waterEvidence, isCurrentRegion);
        var conditionsKnowledge = ResolveFieldKnowledge(group.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalRegionConditionsDiscoveryId(region.Id)), hasObservation, monthsSpent, isCurrentRegion);

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

    public bool CanIntentionallyForage(Region region, string floraSpeciesId)
    {
        return ResolveFloraUseMultiplier(region, floraSpeciesId) > 0.0f;
    }

    public bool CanIntentionallyHunt(Region region, string faunaSpeciesId)
    {
        return ResolveFaunaUseMultiplier(region, faunaSpeciesId) > 0.0f;
    }

    public KnowledgeLevel GetFloraKnowledgeLevel(Region region, string floraSpeciesId)
    {
        return ResolveSpeciesKnowledge(region, floraSpeciesId, SpeciesClass.Flora);
    }

    public KnowledgeLevel GetFaunaKnowledgeLevel(Region region, string faunaSpeciesId)
    {
        return ResolveSpeciesKnowledge(region, faunaSpeciesId, SpeciesClass.Fauna);
    }

    public float ResolveFloraUseMultiplier(Region region, string floraSpeciesId)
    {
        return ResolveSpeciesUseMultiplier(region, floraSpeciesId, SpeciesClass.Flora);
    }

    public float ResolveFaunaUseMultiplier(Region region, string faunaSpeciesId)
    {
        return ResolveSpeciesUseMultiplier(region, faunaSpeciesId, SpeciesClass.Fauna);
    }

    private KnowledgeLevel ResolveRouteKnowledge(string targetRegionId, string? fromRegionId, bool isCurrentRegion, bool isKnownRegion)
    {
        if (isCurrentRegion)
        {
            return KnowledgeLevel.Knowledge;
        }

        if (string.IsNullOrWhiteSpace(fromRegionId))
        {
            return isKnownRegion ? KnowledgeLevel.Encounter : KnowledgeLevel.Unknown;
        }

        if (group.KnownDiscoveryIds.Contains(discoveryCatalog.GetRouteDiscoveryId(fromRegionId, targetRegionId)))
        {
            return KnowledgeLevel.Discovery;
        }

        return KnowledgeLevel.Encounter;
    }

    private static KnowledgeLevel ResolveFieldKnowledge(bool explicitlyKnown, bool hasObservation, int evidenceMonths, bool isCurrentRegion)
    {
        if (explicitlyKnown)
        {
            return isCurrentRegion ? KnowledgeLevel.Knowledge : KnowledgeLevel.Discovery;
        }

        if (evidenceMonths > 0 || hasObservation)
        {
            return KnowledgeLevel.Encounter;
        }

        return KnowledgeLevel.Unknown;
    }

    private static KnowledgeLevel ResolveOverallKnowledge(params KnowledgeLevel[] values)
    {
        if (values.All(value => value >= KnowledgeLevel.Discovery))
        {
            return values.Any(value => value == KnowledgeLevel.Knowledge)
                ? KnowledgeLevel.Knowledge
                : KnowledgeLevel.Discovery;
        }

        if (values.Any(value => value == KnowledgeLevel.Discovery))
        {
            return KnowledgeLevel.Discovery;
        }

        if (values.Any(value => value == KnowledgeLevel.Encounter))
        {
            return KnowledgeLevel.Encounter;
        }

        return KnowledgeLevel.Unknown;
    }

    private static float EstimatePotential(KnowledgeLevel level, float actual, float baseline, int monthlyFoodNeed)
    {
        return level switch
        {
            KnowledgeLevel.Knowledge => actual,
            KnowledgeLevel.Discovery => BucketPotential(actual, monthlyFoodNeed),
            KnowledgeLevel.Encounter => baseline * EncounterBaselineMultiplier,
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
            KnowledgeLevel.Knowledge => actual,
            KnowledgeLevel.Discovery => BucketScalar(actual),
            KnowledgeLevel.Encounter => baseline * EncounterBaselineMultiplier,
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

    private KnowledgeLevel ResolveSpeciesKnowledge(Region region, string speciesId, SpeciesClass speciesClass)
    {
        // Sapient intentional use needs both:
        // 1. enough regional field familiarity to meaningfully recognize the local ecology, and
        // 2. polity species-awareness that has reached Knowledge for this specific species.
        var fieldKnowledge = speciesClass == SpeciesClass.Flora
            ? ObserveRegion(region, group.CurrentRegionId).FloraKnowledge
            : ObserveRegion(region, group.CurrentRegionId).FaunaKnowledge;
        if (fieldKnowledge == KnowledgeLevel.Unknown)
        {
            return KnowledgeLevel.Unknown;
        }

        return speciesAwarenessByKey.TryGetValue(BuildSpeciesKey(speciesClass, speciesId), out var state)
            ? state.CurrentLevel
            : KnowledgeLevel.Unknown;
    }

    private static float ResolveSpeciesUseMultiplier(KnowledgeLevel level)
    {
        return level switch
        {
            // Species may exist in the world and still be unusable to this actor.
            // Intentional exploitation requires Knowledge, not merely Discovery.
            KnowledgeLevel.Knowledge => SpeciesAwarenessConstants.KnowledgeUsabilityMultiplier,
            _ => 0.0f
        };
    }

    private float ResolveSpeciesUseMultiplier(Region region, string speciesId, SpeciesClass speciesClass)
    {
        return ResolveSpeciesUseMultiplier(ResolveSpeciesKnowledge(region, speciesId, speciesClass));
    }

    private static string BuildSpeciesKey(SpeciesClass speciesClass, string speciesId)
    {
        return $"{speciesClass}:{speciesId}";
    }
}
