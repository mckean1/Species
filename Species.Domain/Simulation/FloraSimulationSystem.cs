using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Knowledge;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class FloraSimulationSystem
{
    public FloraSimulationResult Run(World world, FloraSpeciesCatalog floraCatalog)
    {
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var sourcePlansByTargetRegionId = BuildSpreadPlans(world, floraCatalog, regionsById);
        var updatedRegions = new List<Region>(world.Regions.Count);
        var changes = new List<FloraPopulationChange>();

        foreach (var region in world.Regions)
        {
            var updatedFlora = new Dictionary<string, int>(region.Ecosystem.FloraPopulations, StringComparer.Ordinal);
            var plannedIncoming = sourcePlansByTargetRegionId.GetValueOrDefault(region.Id, []);

            foreach (var floraEntry in region.Ecosystem.FloraPopulations.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                var species = floraCatalog.GetById(floraEntry.Key);
                if (species is null || species.IsExtinct)
                {
                    continue;
                }

                var profile = region.Ecosystem.FloraProfiles.GetValueOrDefault(species.Id);
                var traits = profile?.Traits ?? species.BaselineTraits;
                var support = ResolveSupport(region, species, traits);
                var targetPopulation = ResolveTargetPopulation(region, species, support);
                var harshness = ResolveHarshness(region, species, support);
                var consumptionPressure = ResolveConsumptionPressure(floraEntry.Value, profile);
                var growth = ResolveGrowth(floraEntry.Value, targetPopulation, species, support);
                var decline = ResolveDecline(floraEntry.Value, targetPopulation, species, support, harshness, consumptionPressure);
                var newPopulation = Math.Max(0, floraEntry.Value + growth - decline);

                if (newPopulation <= FloraSimulationConstants.ExtinctionThresholdPopulation && support < 0.12f)
                {
                    newPopulation = 0;
                }

                if (newPopulation > 0)
                {
                    updatedFlora[floraEntry.Key] = newPopulation;
                }
                else
                {
                    updatedFlora.Remove(floraEntry.Key);
                }

                changes.Add(new FloraPopulationChange
                {
                    RegionId = region.Id,
                    RegionName = region.Name,
                    FloraSpeciesId = species.Id,
                    FloraSpeciesName = species.Name,
                    PreviousPopulation = floraEntry.Value,
                    TargetPopulation = targetPopulation,
                    NewPopulation = newPopulation,
                    Outcome = ResolveOutcome(floraEntry.Value, newPopulation),
                    WaterSupported = species.SupportedWaterAvailabilities.Contains(region.WaterAvailability),
                    CoreBiomeFit = species.CoreBiomes.Contains(region.Biome),
                    FertilityFit = GetFertilityFit((float)region.Fertility, species.HabitatFertilityMin, species.HabitatFertilityMax),
                    BiologicalFit = support,
                    PrimaryCause = ResolvePrimaryCause(floraEntry.Value, newPopulation, support, harshness, consumptionPressure)
                });
            }

            foreach (var incoming in plannedIncoming)
            {
                updatedFlora[incoming.SpeciesId] = updatedFlora.TryGetValue(incoming.SpeciesId, out var existing)
                    ? existing + incoming.Population
                    : incoming.Population;

                var species = floraCatalog.GetById(incoming.SpeciesId);
                if (species is null)
                {
                    continue;
                }

                var incomingTraits = region.Ecosystem.FloraProfiles.GetValueOrDefault(species.Id)?.Traits ?? species.BaselineTraits;
                var incomingSupport = ResolveSupport(region, species, incomingTraits);

                changes.Add(new FloraPopulationChange
                {
                    RegionId = region.Id,
                    RegionName = region.Name,
                    FloraSpeciesId = incoming.SpeciesId,
                    FloraSpeciesName = species.Name,
                    PreviousPopulation = 0,
                    TargetPopulation = ResolveTargetPopulation(region, species, incomingSupport),
                    NewPopulation = updatedFlora[incoming.SpeciesId],
                    Outcome = "Spread",
                    WaterSupported = species.SupportedWaterAvailabilities.Contains(region.WaterAvailability),
                    CoreBiomeFit = species.CoreBiomes.Contains(region.Biome),
                    FertilityFit = GetFertilityFit((float)region.Fertility, species.HabitatFertilityMin, species.HabitatFertilityMax),
                    BiologicalFit = incomingSupport,
                    PrimaryCause = $"Spread from neighboring region {incoming.FromRegionName}"
                });
            }

            var updatedRegion = new Region(
                region.Id,
                region.Name,
                region.Fertility,
                region.Biome,
                region.WaterAvailability,
                region.NeighborIds,
                new RegionEcosystem(
                    region.Ecosystem.ProtoLifeSubstrate,
                    updatedFlora,
                    new Dictionary<string, int>(region.Ecosystem.FaunaPopulations, StringComparer.Ordinal),
                    CloneProfiles(region.Ecosystem.FloraProfiles),
                    CloneProfiles(region.Ecosystem.FaunaProfiles),
                    region.Ecosystem.FossilRecords.ToArray(),
                    region.Ecosystem.BiologicalHistoryRecords.ToArray()),
                region.MaterialProfile.Clone());

            updatedRegions.Add(updatedRegion);
        }

        return new FloraSimulationResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, updatedRegions, world.PopulationGroups, world.Chronicle, world.Polities, world.FocalPolityId),
            changes);
    }

    private static Dictionary<string, List<SpreadPlan>> BuildSpreadPlans(
        World world,
        FloraSpeciesCatalog floraCatalog,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        var plansByTargetRegionId = new Dictionary<string, List<SpreadPlan>>(StringComparer.Ordinal);

        foreach (var region in world.Regions)
        {
            foreach (var floraEntry in region.Ecosystem.FloraPopulations.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                var species = floraCatalog.GetById(floraEntry.Key);
                if (species is null || species.IsExtinct || floraEntry.Value <= 0)
                {
                    continue;
                }

                var sourceSupport = ResolveSupport(region, species, region.Ecosystem.FloraProfiles.GetValueOrDefault(species.Id)?.Traits ?? species.BaselineTraits);
                var sourceOccupancy = ResolveRegionalOccupancy(floraEntry.Value, species);
                if (sourceSupport < FloraSimulationConstants.SpreadThreshold || sourceOccupancy < FloraSimulationConstants.SpreadSourceOccupancyThreshold)
                {
                    continue;
                }

                foreach (var neighborId in region.NeighborIds.OrderBy(id => id, StringComparer.Ordinal))
                {
                    if (!regionsById.TryGetValue(neighborId, out var neighbor))
                    {
                        continue;
                    }

                    if (neighbor.Ecosystem.FloraPopulations.ContainsKey(species.Id))
                    {
                        continue;
                    }

                    var targetSupport = ResolveSupport(neighbor, species, species.BaselineTraits);
                    if (targetSupport < FloraSimulationConstants.SpreadThreshold)
                    {
                        continue;
                    }

                    var spreadPressure = neighbor.Ecosystem.ProtoLifeSubstrate.ProtoFloraPressure;
                    var spreadStrength = (sourceSupport * 0.42f) +
                                         (species.SpreadTendency * 0.28f) +
                                         (species.RegionalAbundance * 0.12f) +
                                         (spreadPressure * FloraSimulationConstants.SpreadNeighborPressureWeight) +
                                         (targetSupport * 0.18f);
                    if (spreadStrength < FloraSimulationConstants.SpreadThreshold)
                    {
                        continue;
                    }

                    var spreadPopulation = Math.Max(
                        FloraSimulationConstants.MinimumSpreadPopulation,
                        (int)MathF.Round(floraEntry.Value * species.SpreadTendency * FloraSimulationConstants.SpreadPopulationFactor * targetSupport, MidpointRounding.AwayFromZero));

                    if (!plansByTargetRegionId.TryGetValue(neighbor.Id, out var plans))
                    {
                        plans = [];
                        plansByTargetRegionId.Add(neighbor.Id, plans);
                    }

                    plans.Add(new SpreadPlan(species.Id, region.Name, spreadPopulation));
                }
            }
        }

        return plansByTargetRegionId;
    }

    private static float ResolveSupport(Region region, FloraSpeciesDefinition species, BiologicalTraitProfile traits)
    {
        if (!species.SupportedWaterAvailabilities.Contains(region.WaterAvailability))
        {
            return 0.0f;
        }

        var fertilityFit = GetFertilityFit((float)region.Fertility, species.HabitatFertilityMin, species.HabitatFertilityMax);
        var waterFit = species.SupportedWaterAvailabilities.Contains(region.WaterAvailability) ? 1.0f : 0.0f;
        var biomeFit = species.CoreBiomes.Contains(region.Biome)
            ? FloraSimulationConstants.CoreBiomeFitMultiplier
            : FloraSimulationConstants.NonCoreBiomeFitMultiplier;
        var climateFit = ResolveClimateFit(region, traits);
        var resilienceBuffer = (species.ConsumptionResilience * 0.55f) + ((traits.Resilience / 100.0f) * 0.45f);

        return ClampNormalized(
            (fertilityFit * 0.30f) +
            (waterFit * 0.24f) +
            (biomeFit * 0.18f) +
            (climateFit * 0.18f) +
            (resilienceBuffer * 0.10f));
    }

    private static int ResolveTargetPopulation(Region region, FloraSpeciesDefinition species, float support)
    {
        var protoSupport = region.Ecosystem.ProtoLifeSubstrate.ProtoFloraPressure;
        var vacancy = region.Ecosystem.ProtoLifeSubstrate.FloraOccupancyDeficit;
        var targetNormalized =
            (support * FloraSimulationConstants.SupportTargetWeight) +
            (species.RegionalAbundance * FloraSimulationConstants.AbundanceTargetWeight) +
            (protoSupport * FloraSimulationConstants.ProtoPressureTargetWeight) +
            (vacancy * FloraSimulationConstants.VacancyTargetWeight);

        return ToPopulationCount(ClampNormalized(targetNormalized));
    }

    private static int ResolveGrowth(int currentPopulation, int targetPopulation, FloraSpeciesDefinition species, float support)
    {
        if (support <= 0.0f)
        {
            return 0;
        }

        var occupancyDeficit = targetPopulation <= 0
            ? 0.0f
            : Math.Clamp((targetPopulation - currentPopulation) / (float)Math.Max(1, targetPopulation), 0.0f, 1.0f);
        var growthRate =
            FloraSimulationConstants.MinimumGrowthRate +
            (species.GrowthRate * FloraSimulationConstants.GrowthRateWeight) +
            (species.RecoveryRate * FloraSimulationConstants.RecoveryRateWeight) +
            (species.UsableBiomass * FloraSimulationConstants.BiomassGrowthWeight) +
            (occupancyDeficit * FloraSimulationConstants.RecoveryOccupancyWeight);

        return Math.Max(0, (int)MathF.Round(currentPopulation * support * growthRate, MidpointRounding.AwayFromZero));
    }

    private static int ResolveDecline(int currentPopulation, int targetPopulation, FloraSpeciesDefinition species, float support, float harshness, float consumptionPressure)
    {
        if (currentPopulation <= 0)
        {
            return 0;
        }

        var poorSupport = 1.0f - support;
        var underTargetRelief = targetPopulation <= 0
            ? 0.0f
            : Math.Clamp((targetPopulation - currentPopulation) / (float)Math.Max(1, targetPopulation), 0.0f, 1.0f);
        var declineRate =
            (harshness * FloraSimulationConstants.HarshnessDeclineWeight) +
            (consumptionPressure * FloraSimulationConstants.ConsumptionDeclineWeight) +
            (poorSupport * FloraSimulationConstants.PoorSupportDeclineWeight);

        declineRate *= 1.0f - (species.ConsumptionResilience * 0.42f);
        declineRate = Math.Max(0.0f, declineRate - (underTargetRelief * FloraSimulationConstants.UnderTargetDeclineReliefWeight));
        return Math.Max(0, (int)MathF.Round(currentPopulation * declineRate, MidpointRounding.AwayFromZero));
    }

    private static float ResolveConsumptionPressure(int currentPopulation, RegionalBiologicalProfile? profile)
    {
        if (profile is null || profile.LastPopulation <= 0 || currentPopulation >= profile.LastPopulation)
        {
            return 0.0f;
        }

        return ClampNormalized((profile.LastPopulation - currentPopulation) / (float)Math.Max(1, profile.LastPopulation));
    }

    private static float ResolveHarshness(Region region, FloraSpeciesDefinition species, float support)
    {
        var waterHarshness = region.WaterAvailability switch
        {
            WaterAvailability.Low => 0.72f,
            WaterAvailability.Medium => 0.34f,
            _ => 0.12f
        };
        var biomeHarshness = region.Biome switch
        {
            Biome.Desert => 0.86f,
            Biome.Tundra => 0.74f,
            Biome.Highlands => 0.52f,
            Biome.Wetlands => 0.18f,
            Biome.Forest => 0.20f,
            _ => 0.28f
        };

        var mismatch = species.CoreBiomes.Contains(region.Biome) ? 0.0f : 0.18f;
        return ClampNormalized((waterHarshness * 0.38f) + (biomeHarshness * 0.42f) + mismatch + ((1.0f - support) * 0.20f));
    }

    private static float ResolveClimateFit(Region region, BiologicalTraitProfile traits)
    {
        var coldDemand = region.Biome switch
        {
            Biome.Tundra => 78,
            Biome.Highlands => 60,
            _ => 34
        };
        var heatDemand = region.Biome switch
        {
            Biome.Desert => 82,
            Biome.Wetlands => 56,
            Biome.Plains => 50,
            _ => 38
        };
        var droughtDemand = region.WaterAvailability switch
        {
            WaterAvailability.Low => 82,
            WaterAvailability.Medium => 46,
            _ => 24
        };

        var coldFit = 1.0f - MathF.Abs(traits.ColdTolerance - coldDemand) / 100.0f;
        var heatFit = 1.0f - MathF.Abs(traits.HeatTolerance - heatDemand) / 100.0f;
        var droughtFit = 1.0f - MathF.Abs(traits.DroughtTolerance - droughtDemand) / 100.0f;
        return ClampNormalized((coldFit * 0.28f) + (heatFit * 0.28f) + (droughtFit * 0.28f) + ((traits.Flexibility / 100.0f) * 0.16f));
    }

    private static float ResolveRegionalOccupancy(int currentPopulation, FloraSpeciesDefinition species)
    {
        return ClampNormalized(currentPopulation / Math.Max(1.0f, species.RegionalAbundance * EcologySeedingConstants.PopulationScale));
    }

    private static float GetFertilityFit(float fertility, float preferredMin, float preferredMax)
    {
        if (fertility >= preferredMin && fertility <= preferredMax)
        {
            return 1.0f;
        }

        var distance = fertility < preferredMin
            ? preferredMin - fertility
            : fertility - preferredMax;

        return ClampNormalized(1.0f - (distance / FloraSimulationConstants.FertilityFitFalloffRange));
    }

    private static string ResolveOutcome(int previousPopulation, int newPopulation)
    {
        if (newPopulation == 0 && previousPopulation > 0)
        {
            return "WentExtinct";
        }

        if (newPopulation > previousPopulation)
        {
            return "Grew";
        }

        if (newPopulation < previousPopulation)
        {
            return "Declined";
        }

        return "Stabilized";
    }

    private static string ResolvePrimaryCause(int previousPopulation, int newPopulation, float support, float harshness, float consumptionPressure)
    {
        if (newPopulation == 0 && previousPopulation > 0)
        {
            return "Regional extinction";
        }

        if (consumptionPressure >= 0.30f)
        {
            return "Consumption pressure";
        }

        if (harshness >= 0.55f)
        {
            return "Harsh regional support";
        }

        if (support >= 0.65f && newPopulation > previousPopulation)
        {
            return "Strong local support";
        }

        if (support < 0.35f)
        {
            return "Poor support";
        }

        return "Near ecological equilibrium";
    }

    private static Dictionary<string, RegionalBiologicalProfile> CloneProfiles(IReadOnlyDictionary<string, RegionalBiologicalProfile> profiles)
    {
        return profiles.ToDictionary(entry => entry.Key, entry => entry.Value.Clone(), StringComparer.Ordinal);
    }

    private static int ToPopulationCount(float value)
    {
        return (int)MathF.Round(value * EcologySeedingConstants.PopulationScale, MidpointRounding.AwayFromZero);
    }

    private static float ClampNormalized(float value)
    {
        return MathF.Round(Math.Clamp(value, SpeciesDefinitionConstants.NormalizedMinimum, SpeciesDefinitionConstants.NormalizedMaximum), 2);
    }

    private sealed record SpreadPlan(string SpeciesId, string FromRegionName, int Population);
}
