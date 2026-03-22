using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Discovery;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Discovery;

public sealed class GroupDiscoveryContext
{
    private const float UnknownBaselineMultiplier = 0.65f;
    private const float EncounterBaselineMultiplier = 0.90f;

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

    private GroupDiscoveryContext(
        World world,
        PopulationGroup group,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
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

    public static GroupDiscoveryContext Create(
        World world,
        PopulationGroup group,
        DiscoveryCatalog discoveryCatalog,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        return new GroupDiscoveryContext(world, group, discoveryCatalog, floraCatalog, faunaCatalog);
    }

    public RegionDiscoverySnapshot ObserveRegion(string regionId, string? fromRegionId = null)
    {
        return regionsById.TryGetValue(regionId, out var region)
            ? ObserveRegion(region, fromRegionId)
            : new RegionDiscoverySnapshot(
                regionId,
                "Unknown",
                false,
                false,
                DiscoveryStage.Unknown,
                DiscoveryStage.Unknown,
                DiscoveryStage.Unknown,
                DiscoveryStage.Unknown,
                DiscoveryStage.Unknown,
                DiscoveryStage.Unknown,
                baselineGatheringPotentialFood * UnknownBaselineMultiplier,
                baselineHuntingPotentialFood * UnknownBaselineMultiplier,
                baselineWaterSupport * UnknownBaselineMultiplier,
                baselineThreatPressure * UnknownBaselineMultiplier,
                0,
                0,
                0,
                0);
    }

    public RegionDiscoverySnapshot ObserveRegion(Region region, string? fromRegionId = null)
    {
        var isCurrentRegion = string.Equals(region.Id, group.CurrentRegionId, StringComparison.Ordinal);
        var isKnownRegion = isCurrentRegion || group.KnownRegionIds.Contains(region.Id);
        var monthsSpent = group.DiscoveryEvidence.MonthsSpentByRegionId.GetValueOrDefault(region.Id);
        var gatheringEvidence = group.DiscoveryEvidence.SuccessfulGatheringMonthsByRegionId.GetValueOrDefault(region.Id);
        var huntingEvidence = group.DiscoveryEvidence.SuccessfulHuntingMonthsByRegionId.GetValueOrDefault(region.Id);
        var waterEvidence = group.DiscoveryEvidence.WaterExposureMonthsByRegionId.GetValueOrDefault(region.Id);
        var hasObservation = isKnownRegion || monthsSpent > 0 || gatheringEvidence > 0 || huntingEvidence > 0 || waterEvidence > 0;

        var routeStage = ResolveRouteStage(region.Id, fromRegionId, isCurrentRegion, isKnownRegion);
        var floraStage = ResolveFieldStage(group.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalFloraDiscoveryId(region.Id)), hasObservation);
        var faunaStage = ResolveFieldStage(group.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalFaunaDiscoveryId(region.Id)), hasObservation);
        var waterStage = ResolveFieldStage(group.KnownDiscoveryIds.Contains(discoveryCatalog.GetLocalWaterSourcesDiscoveryId(region.Id)), hasObservation);
        var regionStage = ResolveFieldStage(discoveryCatalog.IsLocalRegionDiscoveryKnown(group.KnownDiscoveryIds, region.Id), hasObservation);

        var actualGatheringPotential = SubsistenceSupportModel.CalculateGatheringPotentialFood(region, group, floraCatalog);
        var actualHuntingPotential = SubsistenceSupportModel.CalculateHuntingPotentialFood(region, group, faunaCatalog);
        var actualWaterSupport = SubsistenceSupportModel.ResolveWaterSupport(region.WaterAvailability);
        var actualThreatPressure = SubsistenceSupportModel.NormalizeThreatPressure(SubsistenceSupportModel.CalculateCarnivoreThreat(region, faunaCatalog));
        var monthlyFoodNeed = SubsistenceSupportModel.CalculateMonthlyFoodNeed(group.Population);

        return new RegionDiscoverySnapshot(
            region.Id,
            region.Name,
            isKnownRegion,
            isCurrentRegion,
            ResolveOverallStage(floraStage, faunaStage, waterStage, regionStage, routeStage),
            floraStage,
            faunaStage,
            waterStage,
            regionStage,
            routeStage,
            EstimatePotential(floraStage, actualGatheringPotential, baselineGatheringPotentialFood, monthlyFoodNeed, isCurrentRegion),
            EstimatePotential(faunaStage, actualHuntingPotential, baselineHuntingPotentialFood, monthlyFoodNeed, isCurrentRegion),
            EstimateWaterSupport(waterStage, actualWaterSupport, baselineWaterSupport, isCurrentRegion),
            EstimateScalar(regionStage == DiscoveryStage.Unknown ? faunaStage : regionStage, actualThreatPressure, baselineThreatPressure, isCurrentRegion),
            monthsSpent,
            gatheringEvidence,
            huntingEvidence,
            waterEvidence);
    }

    public bool CanIntentionallyForage(Region region, string floraSpeciesId)
    {
        return ResolveFloraUseMultiplier(region, floraSpeciesId) > 0.0f;
    }

    public bool CanIntentionallyHunt(Region region, string faunaSpeciesId)
    {
        return ResolveFaunaUseMultiplier(region, faunaSpeciesId) > 0.0f;
    }

    public DiscoveryStage GetFloraDiscoveryStage(Region region, string floraSpeciesId)
    {
        return ResolveSpeciesDiscoveryStage(region, floraSpeciesId, SpeciesClass.Flora);
    }

    public DiscoveryStage GetFaunaDiscoveryStage(Region region, string faunaSpeciesId)
    {
        return ResolveSpeciesDiscoveryStage(region, faunaSpeciesId, SpeciesClass.Fauna);
    }

    public float ResolveFloraUseMultiplier(Region region, string floraSpeciesId)
    {
        return ResolveSpeciesUseMultiplier(region, floraSpeciesId, SpeciesClass.Flora);
    }

    public float ResolveFaunaUseMultiplier(Region region, string faunaSpeciesId)
    {
        return ResolveSpeciesUseMultiplier(region, faunaSpeciesId, SpeciesClass.Fauna);
    }

    private DiscoveryStage ResolveRouteStage(string targetRegionId, string? fromRegionId, bool isCurrentRegion, bool isKnownRegion)
    {
        if (isCurrentRegion)
        {
            return DiscoveryStage.Discovered;
        }

        if (string.IsNullOrWhiteSpace(fromRegionId))
        {
            return isKnownRegion ? DiscoveryStage.Encountered : DiscoveryStage.Unknown;
        }

        if (group.KnownDiscoveryIds.Contains(discoveryCatalog.GetRouteDiscoveryId(fromRegionId, targetRegionId)))
        {
            return DiscoveryStage.Discovered;
        }

        return DiscoveryStage.Encountered;
    }

    private static DiscoveryStage ResolveFieldStage(bool explicitlyDiscovered, bool hasObservation)
    {
        if (explicitlyDiscovered)
        {
            return DiscoveryStage.Discovered;
        }

        return hasObservation ? DiscoveryStage.Encountered : DiscoveryStage.Unknown;
    }

    private static DiscoveryStage ResolveOverallStage(params DiscoveryStage[] values)
    {
        if (values.All(value => value == DiscoveryStage.Discovered))
        {
            return DiscoveryStage.Discovered;
        }

        if (values.Any(value => value == DiscoveryStage.Discovered))
        {
            return DiscoveryStage.Discovered;
        }

        if (values.Any(value => value == DiscoveryStage.Encountered))
        {
            return DiscoveryStage.Encountered;
        }

        return DiscoveryStage.Unknown;
    }

    private static float EstimatePotential(DiscoveryStage stage, float actual, float baseline, int monthlyFoodNeed, bool isCurrentRegion)
    {
        if (isCurrentRegion || stage == DiscoveryStage.Discovered)
        {
            return actual;
        }

        return stage switch
        {
            DiscoveryStage.Encountered => baseline * EncounterBaselineMultiplier,
            _ => baseline * UnknownBaselineMultiplier
        };
    }

    private static float EstimateScalar(DiscoveryStage stage, float actual, float baseline, bool isCurrentRegion)
    {
        if (isCurrentRegion || stage == DiscoveryStage.Discovered)
        {
            return actual;
        }

        return stage switch
        {
            DiscoveryStage.Encountered => baseline * EncounterBaselineMultiplier,
            _ => baseline * UnknownBaselineMultiplier
        };
    }

    private static float EstimateWaterSupport(DiscoveryStage stage, float actual, float baseline, bool isCurrentRegion)
    {
        if (isCurrentRegion || stage == DiscoveryStage.Discovered)
        {
            return actual;
        }

        return stage switch
        {
            DiscoveryStage.Encountered => Math.Clamp(Math.Max(actual - 12.0f, baseline * 0.88f), 0.0f, 100.0f),
            _ => Math.Clamp(baseline * UnknownBaselineMultiplier, 0.0f, 100.0f)
        };
    }

    private DiscoveryStage ResolveSpeciesDiscoveryStage(Region region, string speciesId, SpeciesClass speciesClass)
    {
        var fieldStage = speciesClass == SpeciesClass.Flora
            ? ObserveRegion(region, group.CurrentRegionId).FloraStage
            : ObserveRegion(region, group.CurrentRegionId).FaunaStage;
        if (fieldStage == DiscoveryStage.Unknown)
        {
            return DiscoveryStage.Unknown;
        }

        return speciesAwarenessByKey.TryGetValue(BuildSpeciesKey(speciesClass, speciesId), out var state)
            ? state.CurrentStage
            : DiscoveryStage.Unknown;
    }

    private static float ResolveSpeciesUseMultiplier(DiscoveryStage stage)
    {
        return stage == DiscoveryStage.Discovered
            ? SpeciesAwarenessConstants.DiscoveryUsabilityMultiplier
            : 0.0f;
    }

    private float ResolveSpeciesUseMultiplier(Region region, string speciesId, SpeciesClass speciesClass)
    {
        return ResolveSpeciesUseMultiplier(ResolveSpeciesDiscoveryStage(region, speciesId, speciesClass));
    }

    private static string BuildSpeciesKey(SpeciesClass speciesClass, string speciesId)
    {
        return $"{speciesClass}:{speciesId}";
    }
}
