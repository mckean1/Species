using Species.Domain.Constants;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class ProtoPressureSystem
{
    public ProtoPressureResult Run(World world)
    {
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var updatedRegions = new List<Region>(world.Regions.Count);
        var changes = new List<ProtoPressureChange>(world.Regions.Count);

        foreach (var region in world.Regions)
        {
            var substrate = region.Ecosystem.ProtoLifeSubstrate;
            var floraCapacity = ClampNormalized(substrate.ProtoFloraCapacity);
            var faunaCapacity = ClampNormalized(substrate.ProtoFaunaCapacity);
            var floraOccupancy = NormalizePopulation(region.Ecosystem.FloraPopulations.Values.Sum(), floraCapacity, ProtoLifePressureConstants.FloraCapacityPopulationScale);
            var faunaOccupancy = NormalizePopulation(region.Ecosystem.FaunaPopulations.Values.Sum(), faunaCapacity, ProtoLifePressureConstants.FaunaCapacityPopulationScale);
            var floraOccupancyDeficit = ClampNormalized(1.0f - floraOccupancy);
            var faunaOccupancyDeficit = ClampNormalized(1.0f - faunaOccupancy);
            var floraEnvironmentalSuitability = ResolveFloraEnvironmentalSuitability(region);
            var floraSupport = ClampNormalized(Math.Max(floraOccupancy, substrate.ProtoFloraPressure * ProtoLifePressureConstants.ProtoFloraSupportContributionWeight));
            var faunaSupport = ClampNormalized((floraOccupancy * 0.55f) + (floraSupport * 0.25f) + (faunaOccupancy * 0.20f));
            var floraSupportDeficit = ClampNormalized(Math.Max(0.0f, floraCapacity - floraSupport));
            var faunaSupportDeficit = ClampNormalized(Math.Max(0.0f, faunaCapacity - faunaSupport));
            var ecologicalVacancy = ClampNormalized((floraOccupancyDeficit * 0.52f) + (faunaOccupancyDeficit * 0.48f));
            var recentCollapseOpening = ResolveRecentCollapseOpening(region, world.CurrentYear, world.CurrentMonth);
            var harshness = ResolveHarshness(region);
            var floraNeighborInfluence = ResolveNeighborInfluence(region, regionsById, isFlora: true);
            var faunaNeighborInfluence = ResolveNeighborInfluence(region, regionsById, isFlora: false);
            var saturationDrag = ClampNormalized((floraOccupancy * ProtoLifePressureConstants.FloraSaturationDragWeight) + (faunaOccupancy * 0.10f));
            var floraVacancyImpulse = ecologicalVacancy * ClampNormalized(
                (floraEnvironmentalSuitability * 0.56f) +
                (floraNeighborInfluence * 0.24f) +
                (floraCapacity * 0.20f));
            var emptyRegionDrag = ecologicalVacancy * harshness * 0.16f;
            var floraTarget = ClampNormalized(
                (floraCapacity * 0.20f) +
                (floraEnvironmentalSuitability * 0.22f) +
                (floraOccupancyDeficit * 0.18f) +
                (floraSupportDeficit * 0.12f) +
                (floraVacancyImpulse * 0.12f) +
                (floraNeighborInfluence * ProtoLifePressureConstants.FloraNeighborInfluenceWeight) +
                (recentCollapseOpening * ProtoLifePressureConstants.CollapseOpeningWeight) -
                saturationDrag -
                emptyRegionDrag -
                (harshness * ProtoLifePressureConstants.HarshnessDragWeight));

            // Proto-fauna pressure should respond mostly to real active food-base support.
            // Proto-flora pressure contributes only a small residual nudge so empty regions do not "respawn" fauna.
            var floraSupportForFauna = ClampNormalized(
                (floraOccupancy * 0.64f) +
                (faunaOccupancy * 0.20f) +
                (floraSupport * 0.04f) +
                (substrate.ProtoFloraPressure * ProtoLifePressureConstants.ProtoFloraSupportContributionWeight));
            var faunaVacancyImpulse = ecologicalVacancy * ClampNormalized(
                (floraSupportForFauna * 0.58f) +
                (faunaNeighborInfluence * 0.18f) +
                (faunaCapacity * 0.24f));
            var foodBaseDrag = ecologicalVacancy * Math.Max(0.0f, 0.35f - floraSupportForFauna) * 0.30f;
            var faunaTarget = ClampNormalized(
                (faunaCapacity * 0.14f) +
                (faunaOccupancyDeficit * 0.16f) +
                (faunaSupportDeficit * 0.18f) +
                (floraSupportForFauna * 0.24f) +
                (faunaVacancyImpulse * 0.10f) +
                (faunaNeighborInfluence * ProtoLifePressureConstants.FaunaNeighborInfluenceWeight) +
                (recentCollapseOpening * 0.10f) -
                foodBaseDrag -
                ((faunaOccupancy * ProtoLifePressureConstants.FaunaSaturationDragWeight)) -
                (harshness * (ProtoLifePressureConstants.HarshnessDragWeight + 0.04f)));

            var newFloraPressure = MovePressure(substrate.ProtoFloraPressure, floraTarget, ProtoLifePressureConstants.FloraRiseRate, ProtoLifePressureConstants.FloraDecayRate);
            var newFaunaPressure = MovePressure(substrate.ProtoFaunaPressure, faunaTarget, ProtoLifePressureConstants.FaunaRiseRate, ProtoLifePressureConstants.FaunaDecayRate);
            var floraReadiness = ResolveFloraReadinessMonths(substrate, newFloraPressure, floraEnvironmentalSuitability, ecologicalVacancy, harshness);
            var faunaReadiness = ResolveFaunaReadinessMonths(substrate, newFaunaPressure, floraSupportForFauna, ecologicalVacancy, harshness);

            var updatedSubstrate = new ProtoLifeSubstrate
            {
                ProtoFloraCapacity = floraCapacity,
                ProtoFaunaCapacity = faunaCapacity,
                ProtoFloraPressure = newFloraPressure,
                ProtoFaunaPressure = newFaunaPressure,
                FloraOccupancyDeficit = floraOccupancyDeficit,
                FaunaOccupancyDeficit = faunaOccupancyDeficit,
                FloraSupportDeficit = floraSupportDeficit,
                FaunaSupportDeficit = faunaSupportDeficit,
                EcologicalVacancy = ecologicalVacancy,
                RecentCollapseOpening = recentCollapseOpening,
                ProtoFloraReadinessMonths = floraReadiness,
                ProtoFaunaReadinessMonths = faunaReadiness,
                ProtoFloraGenesisCooldownMonths = Math.Max(0, substrate.ProtoFloraGenesisCooldownMonths - 1),
                ProtoFaunaGenesisCooldownMonths = Math.Max(0, substrate.ProtoFaunaGenesisCooldownMonths - 1)
            };

            updatedRegions.Add(new Region(
                region.Id,
                region.Name,
                region.Fertility,
                region.Biome,
                region.WaterAvailability,
                region.NeighborIds,
                new RegionEcosystem(
                    updatedSubstrate,
                    new Dictionary<string, int>(region.Ecosystem.FloraPopulations, StringComparer.Ordinal),
                    new Dictionary<string, int>(region.Ecosystem.FaunaPopulations, StringComparer.Ordinal),
                    region.Ecosystem.FloraProfiles.ToDictionary(entry => entry.Key, entry => entry.Value.Clone(), StringComparer.Ordinal),
                    region.Ecosystem.FaunaProfiles.ToDictionary(entry => entry.Key, entry => entry.Value.Clone(), StringComparer.Ordinal),
                    region.Ecosystem.FossilRecords.ToArray(),
                    region.Ecosystem.BiologicalHistoryRecords.ToArray()),
                region.MaterialProfile.Clone()));

            changes.Add(new ProtoPressureChange
            {
                RegionId = region.Id,
                RegionName = region.Name,
                PreviousProtoFloraPressure = substrate.ProtoFloraPressure,
                NewProtoFloraPressure = newFloraPressure,
                PreviousProtoFaunaPressure = substrate.ProtoFaunaPressure,
                NewProtoFaunaPressure = newFaunaPressure,
                FloraOccupancyDeficit = floraOccupancyDeficit,
                FaunaOccupancyDeficit = faunaOccupancyDeficit,
                FloraSupportDeficit = floraSupportDeficit,
                FaunaSupportDeficit = faunaSupportDeficit,
                EcologicalVacancy = ecologicalVacancy,
                RecentCollapseOpening = recentCollapseOpening,
                Summary = BuildSummary(updatedSubstrate)
            });
        }

        return new ProtoPressureResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, updatedRegions, world.PopulationGroups, world.Chronicle, world.Polities, world.FocalPolityId),
            changes);
    }

    private static float ResolveFloraEnvironmentalSuitability(Region region)
    {
        var fertility = (float)Math.Clamp(region.Fertility, 0.0, 1.0);
        var water = region.WaterAvailability switch
        {
            Enums.WaterAvailability.High => 1.00f,
            Enums.WaterAvailability.Medium => 0.72f,
            _ => 0.40f
        };

        return ClampNormalized((fertility * 0.62f) + (water * 0.38f));
    }

    private static float ResolveHarshness(Region region)
    {
        var waterHarshness = region.WaterAvailability switch
        {
            Enums.WaterAvailability.Low => 0.82f,
            Enums.WaterAvailability.Medium => 0.36f,
            _ => 0.12f
        };
        var biomeHarshness = region.Biome switch
        {
            Enums.Biome.Desert => 0.90f,
            Enums.Biome.Tundra => 0.82f,
            Enums.Biome.Highlands => 0.56f,
            Enums.Biome.Plains => 0.28f,
            Enums.Biome.Forest => 0.22f,
            Enums.Biome.Wetlands => 0.24f,
            _ => 0.35f
        };

        return ClampNormalized((waterHarshness * 0.58f) + (biomeHarshness * 0.42f));
    }

    private static float ResolveNeighborInfluence(Region region, IReadOnlyDictionary<string, Region> regionsById, bool isFlora)
    {
        var neighbors = region.NeighborIds
            .Select(regionsById.GetValueOrDefault)
            .Where(neighbor => neighbor is not null)
            .Cast<Region>()
            .ToArray();
        if (neighbors.Length == 0)
        {
            return 0.0f;
        }

        return ClampNormalized((float)neighbors.Average(neighbor =>
        {
            var substrate = neighbor.Ecosystem.ProtoLifeSubstrate;
            return isFlora
                ? (substrate.ProtoFloraPressure * 0.68f) + (substrate.ProtoFloraCapacity * 0.32f)
                : (substrate.ProtoFaunaPressure * 0.60f) + (substrate.ProtoFaunaCapacity * 0.20f) + (substrate.ProtoFloraPressure * 0.20f);
        }));
    }

    private static float ResolveRecentCollapseOpening(Region region, int currentYear, int currentMonth)
    {
        var opening = region.Ecosystem.BiologicalHistoryRecords
            .Where(record =>
                string.Equals(record.EventKind, "regional-extinction", StringComparison.Ordinal) ||
                string.Equals(record.EventKind, "extinction", StringComparison.Ordinal) ||
                string.Equals(record.EventKind, "speciation", StringComparison.Ordinal))
            .Select(record => ResolveOpeningContribution(record, currentYear, currentMonth))
            .Sum();

        return ClampNormalized(opening);
    }

    private static float ResolveOpeningContribution(BiologicalHistoryRecord record, int currentYear, int currentMonth)
    {
        var monthsAgo = ((currentYear - record.Year) * 12) + (currentMonth - record.Month);
        if (monthsAgo < 0 || monthsAgo > ProtoLifePressureConstants.RecentCollapseWindowMonths)
        {
            return 0.0f;
        }

        var freshness = 1.0f - (monthsAgo / ProtoLifePressureConstants.RecentCollapseWindowMonths);
        return string.Equals(record.EventKind, "speciation", StringComparison.Ordinal)
            ? freshness * 0.12f
            : freshness * 0.28f;
    }

    private static float NormalizePopulation(int totalPopulation, float capacity, float scale)
    {
        if (capacity <= 0.0f || scale <= 0.0f)
        {
            return 0.0f;
        }

        return ClampNormalized(totalPopulation / Math.Max(1.0f, capacity * scale));
    }

    private static float MovePressure(float current, float target, float riseRate, float decayRate)
    {
        var rate = target >= current ? riseRate : decayRate;
        return ClampNormalized(current + ((target - current) * rate));
    }

    private static float ClampNormalized(float value)
    {
        return MathF.Round(Math.Clamp(value, 0.0f, 1.0f), 2);
    }

    private static int ResolveFloraReadinessMonths(ProtoLifeSubstrate substrate, float pressure, float suitability, float vacancy, float harshness)
    {
        if (substrate.ProtoFloraGenesisCooldownMonths > 0)
        {
            return 0;
        }

        var ready = pressure >= ProtoGenesisConstants.FloraReadinessPressureThreshold &&
                    suitability >= ProtoGenesisConstants.FloraSuitabilityThreshold &&
                    vacancy >= ProtoGenesisConstants.FloraVacancyThreshold &&
                    vacancy <= 0.90f &&
                    harshness <= 0.52f;
        return ready
            ? substrate.ProtoFloraReadinessMonths + 1
            : Math.Max(0, substrate.ProtoFloraReadinessMonths - 1);
    }

    private static int ResolveFaunaReadinessMonths(ProtoLifeSubstrate substrate, float pressure, float floraSupportForFauna, float vacancy, float harshness)
    {
        if (substrate.ProtoFaunaGenesisCooldownMonths > 0)
        {
            return 0;
        }

        var ready = pressure >= ProtoGenesisConstants.FaunaReadinessPressureThreshold &&
                    floraSupportForFauna >= ProtoGenesisConstants.FaunaFoodSupportThreshold &&
                    vacancy >= ProtoGenesisConstants.FaunaVacancyThreshold &&
                    vacancy <= 0.84f &&
                    harshness <= 0.54f;
        return ready
            ? substrate.ProtoFaunaReadinessMonths + 1
            : Math.Max(0, substrate.ProtoFaunaReadinessMonths - 1);
    }

    private static string BuildSummary(ProtoLifeSubstrate substrate)
    {
        return $"ProtoFlora={substrate.ProtoFloraPressure:0.00} ({substrate.ProtoFloraReadinessMonths}m ready/{substrate.ProtoFloraGenesisCooldownMonths}m cd), ProtoFauna={substrate.ProtoFaunaPressure:0.00} ({substrate.ProtoFaunaReadinessMonths}m ready/{substrate.ProtoFaunaGenesisCooldownMonths}m cd), Vacancy={substrate.EcologicalVacancy:0.00}, Opening={substrate.RecentCollapseOpening:0.00}";
    }
}
