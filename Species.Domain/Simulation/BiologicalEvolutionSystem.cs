using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Knowledge;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class BiologicalEvolutionSystem
{
    public BiologicalEvolutionResult Run(
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        SapientSpeciesCatalog sapientCatalog,
        IReadOnlyList<FloraPopulationChange> floraChanges,
        IReadOnlyList<FaunaPopulationChange> faunaChanges)
    {
        var floraChangesByRegion = floraChanges.GroupBy(change => change.RegionId, StringComparer.Ordinal)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray(), StringComparer.Ordinal);
        var faunaChangesByRegion = faunaChanges.GroupBy(change => change.RegionId, StringComparer.Ordinal)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray(), StringComparer.Ordinal);
        var updatedRegions = new List<Region>(world.Regions.Count);
        var chronicleChanges = new List<BiologicalHistoryChange>();
        var extinctionCandidates = new List<(string RegionId, string SpeciesId, string SpeciesName, bool IsFlora)>();

        foreach (var region in world.Regions)
        {
            var mutableFloraProfiles = CloneProfiles(region.Ecosystem.FloraProfiles);
            var mutableFaunaProfiles = CloneProfiles(region.Ecosystem.FaunaProfiles);
            var mutableFlora = new Dictionary<string, int>(region.Ecosystem.FloraPopulations, StringComparer.Ordinal);
            var mutableFauna = new Dictionary<string, int>(region.Ecosystem.FaunaPopulations, StringComparer.Ordinal);
            var fossils = region.Ecosystem.FossilRecords.ToList();
            var history = region.Ecosystem.BiologicalHistoryRecords.ToList();

            UpdateFloraProfiles(region, floraCatalog, floraChangesByRegion.GetValueOrDefault(region.Id, Array.Empty<FloraPopulationChange>()), mutableFlora, mutableFloraProfiles, fossils, history, chronicleChanges, extinctionCandidates, world.CurrentYear, world.CurrentMonth);
            UpdateFaunaProfiles(region, faunaCatalog, sapientCatalog, faunaChangesByRegion.GetValueOrDefault(region.Id, Array.Empty<FaunaPopulationChange>()), mutableFauna, mutableFaunaProfiles, fossils, history, chronicleChanges, extinctionCandidates, world.Seed, world.CurrentYear, world.CurrentMonth);
            var updatedSubstrate = ApplyProtoGenesis(world, region, floraCatalog, faunaCatalog, mutableFlora, mutableFauna, mutableFloraProfiles, mutableFaunaProfiles, history, chronicleChanges);

            updatedRegions.Add(new Region(
                region.Id,
                region.Name,
                region.Fertility,
                region.Biome,
                region.WaterAvailability,
                region.NeighborIds,
                new RegionEcosystem(updatedSubstrate, mutableFlora, mutableFauna, mutableFloraProfiles, mutableFaunaProfiles, fossils, history),
                region.MaterialProfile.Clone()));
        }

        MarkGloballyExtinct(updatedRegions, floraCatalog, faunaCatalog, extinctionCandidates, chronicleChanges, world.CurrentYear, world.CurrentMonth);

        return new BiologicalEvolutionResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, updatedRegions, world.PopulationGroups, world.Chronicle, world.Polities, world.FocalPolityId),
            chronicleChanges);
    }

    private ProtoLifeSubstrate ApplyProtoGenesis(
        World world,
        Region region,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        IDictionary<string, int> floraPopulations,
        IDictionary<string, int> faunaPopulations,
        IDictionary<string, RegionalBiologicalProfile> floraProfiles,
        IDictionary<string, RegionalBiologicalProfile> faunaProfiles,
        IList<BiologicalHistoryRecord> history,
        ICollection<BiologicalHistoryChange> chronicleChanges)
    {
        var substrate = region.Ecosystem.ProtoLifeSubstrate.Clone();
        substrate = TryCreateProtoFlora(world, region, floraCatalog, floraPopulations, floraProfiles, history, chronicleChanges, substrate);
        substrate = TryCreateProtoFauna(world, region, floraCatalog, faunaCatalog, floraPopulations, faunaPopulations, faunaProfiles, history, chronicleChanges, substrate);
        return substrate;
    }

    private ProtoLifeSubstrate TryCreateProtoFlora(
        World world,
        Region region,
        FloraSpeciesCatalog floraCatalog,
        IDictionary<string, int> floraPopulations,
        IDictionary<string, RegionalBiologicalProfile> floraProfiles,
        IList<BiologicalHistoryRecord> history,
        ICollection<BiologicalHistoryChange> chronicleChanges,
        ProtoLifeSubstrate substrate)
    {
        if (substrate.ProtoFloraGenesisCooldownMonths > 0 ||
            substrate.ProtoFloraReadinessMonths < ProtoGenesisConstants.FloraReadinessMonthsRequired)
        {
            return substrate;
        }

        var suitability = ResolveFloraSuitability(region);
        var vacancy = substrate.EcologicalVacancy;
        var fullnessSuppression = ResolveFullnessSuppression(vacancy);
        var triggerChance = Math.Min(
            ProtoGenesisConstants.FloraMonthlyTriggerChanceCap,
            ((substrate.ProtoFloraPressure - ProtoGenesisConstants.FloraReadinessPressureThreshold) * 0.32f) +
            ((substrate.ProtoFloraReadinessMonths - ProtoGenesisConstants.FloraReadinessMonthsRequired + 1) * 0.015f) +
            (suitability * 0.06f) +
            (vacancy * 0.08f)) * fullnessSuppression;
        if (!PassesTrigger(world, region.Id, "proto-flora-genesis", triggerChance))
        {
            return substrate;
        }

        var seedStrength = Math.Clamp((substrate.ProtoFloraPressure * 0.46f) + (suitability * 0.30f) + (vacancy * 0.24f), 0.0f, 1.0f);
        var startingPopulation = Math.Max(
            4,
            (int)Math.Round(seedStrength * EcologySeedingConstants.PopulationScale * 0.14f, MidpointRounding.AwayFromZero));
        var viability = Math.Clamp((int)Math.Round((seedStrength * 60.0f) + (vacancy * 20.0f) + (suitability * 20.0f), MidpointRounding.AwayFromZero), 0, 100);

        if (startingPopulation < 4 || viability < 58)
        {
            history.Add(CreateHistory(region.Id, $"failed-protoflora:{world.CurrentYear:D3}{world.CurrentMonth:D2}", "genesis-failed", world.CurrentYear, world.CurrentMonth, $"Proto-flora lineage attempt in {region.Name} failed to establish."));
            return ReduceGenesisPressure(substrate, isFlora: true, success: false);
        }

        var species = CreateProtoFloraSpecies(region, substrate, world.CurrentYear, world.CurrentMonth);
        floraCatalog.AddOrReplace(species);
        floraPopulations[species.Id] = startingPopulation;
        floraProfiles[species.Id] = new RegionalBiologicalProfile
        {
            SpeciesId = species.Id,
            RegionId = region.Id,
            Traits = species.BaselineTraits.Clone(),
            LastPopulation = startingPopulation,
            ViabilityScore = viability,
            ContinuityMonths = 1,
            SuccessfulMonths = 1
        };

        substrate = ReduceGenesisPressure(substrate, isFlora: true, success: true);
        var message = $"{species.Name} established from proto-flora pressure in {region.Name}.";
        history.Add(CreateHistory(region.Id, species.Id, "genesis", world.CurrentYear, world.CurrentMonth, message));
        chronicleChanges.Add(new BiologicalHistoryChange
        {
            RegionId = region.Id,
            RegionName = region.Name,
            SpeciesId = species.Id,
            SpeciesName = species.Name,
            Kind = "Genesis",
            Message = $"{species.Name} emerged from proto-flora conditions in {region.Name}."
        });
        return substrate;
    }

    private ProtoLifeSubstrate TryCreateProtoFauna(
        World world,
        Region region,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        IDictionary<string, int> floraPopulations,
        IDictionary<string, int> faunaPopulations,
        IDictionary<string, RegionalBiologicalProfile> faunaProfiles,
        IList<BiologicalHistoryRecord> history,
        ICollection<BiologicalHistoryChange> chronicleChanges,
        ProtoLifeSubstrate substrate)
    {
        if (substrate.ProtoFaunaGenesisCooldownMonths > 0 ||
            substrate.ProtoFaunaReadinessMonths < ProtoGenesisConstants.FaunaReadinessMonthsRequired)
        {
            return substrate;
        }

        var suitability = ResolveFaunaSuitability(region);
        var vacancy = substrate.EcologicalVacancy;
        var floraSupport = ResolveProtoFaunaFloraSupport(region, floraCatalog, substrate);
        var preySupport = ResolveProtoFaunaPreySupport(faunaPopulations, faunaCatalog, substrate);
        var activeFoodSupport = Math.Max(floraSupport, preySupport);
        var foodSupport = Math.Clamp((floraSupport * 0.68f) + (preySupport * 0.32f), 0.0f, 1.0f);
        if (foodSupport < ProtoGenesisConstants.FaunaFoodSupportThreshold ||
            activeFoodSupport < ProtoGenesisConstants.FaunaActiveFoodSupportThreshold)
        {
            return ReduceGenesisPressure(substrate, isFlora: false, success: false);
        }

        var fullnessSuppression = ResolveFullnessSuppression(vacancy);
        var triggerChance = Math.Min(
            ProtoGenesisConstants.FaunaMonthlyTriggerChanceCap,
            ((substrate.ProtoFaunaPressure - ProtoGenesisConstants.FaunaReadinessPressureThreshold) * 0.20f) +
            ((substrate.ProtoFaunaReadinessMonths - ProtoGenesisConstants.FaunaReadinessMonthsRequired + 1) * 0.010f) +
            (suitability * 0.04f) +
            (foodSupport * 0.05f) +
            (activeFoodSupport * 0.05f) +
            (vacancy * 0.04f)) * fullnessSuppression;
        if (!PassesTrigger(world, region.Id, "proto-fauna-genesis", triggerChance))
        {
            return substrate;
        }

        var dietLinks = BuildProtoFaunaDietLinks(floraPopulations, faunaPopulations, floraCatalog, faunaCatalog);
        var seedStrength = Math.Clamp((substrate.ProtoFaunaPressure * 0.30f) + (suitability * 0.18f) + (foodSupport * 0.34f) + (activeFoodSupport * 0.10f) + (vacancy * 0.08f), 0.0f, 1.0f);
        var startingPopulation = Math.Max(
            3,
            (int)Math.Round(seedStrength * EcologySeedingConstants.PopulationScale * 0.08f, MidpointRounding.AwayFromZero));
        var viability = Math.Clamp((int)Math.Round((seedStrength * 52.0f) + (foodSupport * 30.0f) + (activeFoodSupport * 10.0f) + (vacancy * 8.0f), MidpointRounding.AwayFromZero), 0, 100);

        if (dietLinks.Length == 0 ||
            foodSupport < ProtoGenesisConstants.FaunaFoodSupportThreshold ||
            activeFoodSupport < ProtoGenesisConstants.FaunaActiveFoodSupportThreshold ||
            startingPopulation < 3 ||
            viability < 60)
        {
            history.Add(CreateHistory(region.Id, $"failed-protofauna:{world.CurrentYear:D3}{world.CurrentMonth:D2}", "genesis-failed", world.CurrentYear, world.CurrentMonth, $"Proto-fauna lineage attempt in {region.Name} failed to establish."));
            return ReduceGenesisPressure(substrate, isFlora: false, success: false);
        }

        var species = CreateProtoFaunaSpecies(region, substrate, dietLinks, floraSupport, preySupport, world.CurrentYear, world.CurrentMonth);
        faunaCatalog.AddOrReplace(species);
        faunaPopulations[species.Id] = startingPopulation;
        faunaProfiles[species.Id] = new RegionalBiologicalProfile
        {
            SpeciesId = species.Id,
            RegionId = region.Id,
            Traits = species.BaselineTraits.Clone(),
            LastPopulation = startingPopulation,
            ViabilityScore = viability,
            ContinuityMonths = 1,
            SuccessfulMonths = 1
        };

        substrate = ReduceGenesisPressure(substrate, isFlora: false, success: true);
        var message = $"{species.Name} established from proto-fauna pressure in {region.Name}.";
        history.Add(CreateHistory(region.Id, species.Id, "genesis", world.CurrentYear, world.CurrentMonth, message));
        chronicleChanges.Add(new BiologicalHistoryChange
        {
            RegionId = region.Id,
            RegionName = region.Name,
            SpeciesId = species.Id,
            SpeciesName = species.Name,
            Kind = "Genesis",
            Message = $"{species.Name} emerged from proto-fauna conditions in {region.Name}."
        });
        return substrate;
    }

    private static float ResolveFloraSuitability(Region region)
    {
        var water = region.WaterAvailability switch
        {
            WaterAvailability.High => 1.00f,
            WaterAvailability.Medium => 0.74f,
            _ => 0.42f
        };

        return Math.Clamp(((float)region.Fertility * 0.62f) + (water * 0.38f), 0.0f, 1.0f);
    }

    private static float ResolveFaunaSuitability(Region region)
    {
        var water = region.WaterAvailability switch
        {
            WaterAvailability.High => 1.00f,
            WaterAvailability.Medium => 0.70f,
            _ => 0.46f
        };
        var biome = region.Biome switch
        {
            Biome.Forest => 0.92f,
            Biome.Plains => 0.88f,
            Biome.Wetlands => 0.84f,
            Biome.Highlands => 0.62f,
            Biome.Tundra => 0.44f,
            Biome.Desert => 0.32f,
            _ => 0.55f
        };

        return Math.Clamp((((float)region.Fertility) * 0.40f) + (water * 0.30f) + (biome * 0.30f), 0.0f, 1.0f);
    }

    private static float ResolveFullnessSuppression(float vacancy)
    {
        var fullness = 1.0f - Math.Clamp(vacancy, 0.0f, 1.0f);
        if (fullness <= ProtoGenesisConstants.FullnessSuppressionStart)
        {
            return 1.0f;
        }

        if (fullness >= ProtoGenesisConstants.FullnessSuppressionEnd)
        {
            return 0.12f;
        }

        var t = (fullness - ProtoGenesisConstants.FullnessSuppressionStart) /
                (ProtoGenesisConstants.FullnessSuppressionEnd - ProtoGenesisConstants.FullnessSuppressionStart);
        return 1.0f - (t * 0.94f);
    }

    private static bool PassesTrigger(World world, string regionId, string key, float chance)
    {
        if (chance <= 0.0f)
        {
            return false;
        }

        return PassesTrigger(world.Seed, world.CurrentYear, world.CurrentMonth, regionId, key, chance);
    }

    private static bool PassesTrigger(int seed, int year, int month, string regionId, string key, float chance)
    {
        if (chance <= 0.0f)
        {
            return false;
        }

        unchecked
        {
            var hash = 2166136261u;
            foreach (var value in $"{seed}|{year}|{month}|{regionId}|{key}")
            {
                hash ^= value;
                hash *= 16777619;
            }

            var roll = (hash % 10000) / 10000.0f;
            return roll < chance;
        }
    }

    private static ProtoLifeSubstrate ReduceGenesisPressure(ProtoLifeSubstrate substrate, bool isFlora, bool success)
    {
        return new ProtoLifeSubstrate
        {
            ProtoFloraCapacity = substrate.ProtoFloraCapacity,
            ProtoFaunaCapacity = substrate.ProtoFaunaCapacity,
            ProtoFloraPressure = isFlora
                ? Math.Max(0.0f, substrate.ProtoFloraPressure - (success ? ProtoGenesisConstants.FloraPressureReductionOnSuccess : ProtoGenesisConstants.PressureReductionOnFailure))
                : substrate.ProtoFloraPressure,
            ProtoFaunaPressure = isFlora
                ? substrate.ProtoFaunaPressure
                : Math.Max(0.0f, substrate.ProtoFaunaPressure - (success ? ProtoGenesisConstants.FaunaPressureReductionOnSuccess : ProtoGenesisConstants.PressureReductionOnFailure)),
            FloraOccupancyDeficit = substrate.FloraOccupancyDeficit,
            FaunaOccupancyDeficit = substrate.FaunaOccupancyDeficit,
            FloraSupportDeficit = substrate.FloraSupportDeficit,
            FaunaSupportDeficit = substrate.FaunaSupportDeficit,
            EcologicalVacancy = substrate.EcologicalVacancy,
            RecentCollapseOpening = substrate.RecentCollapseOpening,
            ProtoFloraReadinessMonths = isFlora ? 0 : substrate.ProtoFloraReadinessMonths,
            ProtoFaunaReadinessMonths = isFlora ? substrate.ProtoFaunaReadinessMonths : 0,
            ProtoFloraGenesisCooldownMonths = isFlora
                ? (success ? ProtoGenesisConstants.FloraCooldownMonthsOnSuccess : ProtoGenesisConstants.CooldownMonthsOnFailure)
                : substrate.ProtoFloraGenesisCooldownMonths,
            ProtoFaunaGenesisCooldownMonths = isFlora
                ? substrate.ProtoFaunaGenesisCooldownMonths
                : (success ? ProtoGenesisConstants.FaunaCooldownMonthsOnSuccess : ProtoGenesisConstants.CooldownMonthsOnFailure)
        };
    }

    private static FaunaDietLink[] BuildProtoFaunaDietLinks(
        IDictionary<string, int> floraPopulations,
        IDictionary<string, int> faunaPopulations,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var floraTargets = floraPopulations
            .Where(entry => entry.Value > 0)
            .Select(entry =>
            {
                var flora = floraCatalog.GetById(entry.Key);
                var support = flora is null ? 0.0f : entry.Value * SubsistenceSupportModel.ResolveFloraSupportPerPopulation(flora);
                return new { entry.Key, Support = support };
            })
            .Where(entry => entry.Support > 0.0f)
            .OrderByDescending(entry => entry.Support)
            .Take(3)
            .ToArray();
        var preyTargets = faunaPopulations
            .Where(entry => entry.Value > 0)
            .Select(entry =>
            {
                var fauna = faunaCatalog.GetById(entry.Key);
                var support = fauna is null ? 0.0f : entry.Value * fauna.FoodYield * fauna.PredatorVulnerability * 0.70f;
                return new { entry.Key, Support = support };
            })
            .Where(entry => entry.Support > 0.0f)
            .OrderByDescending(entry => entry.Support)
            .Take(2)
            .ToArray();

        var links = new List<FaunaDietLink>();
        var floraSupport = floraTargets.Sum(entry => entry.Support);
        var preySupport = preyTargets.Sum(entry => entry.Support);

        if (floraSupport >= preySupport || preyTargets.Length == 0)
        {
            foreach (var target in floraTargets)
            {
                links.Add(new FaunaDietLink
                {
                    TargetKind = FaunaDietTargetKind.FloraSpecies,
                    TargetSpeciesId = target.Key,
                    Weight = (float)(target.Support / Math.Max(1.0, floraSupport)),
                    IsFallback = false
                });
            }

            if (preyTargets.Length > 0)
            {
                links.Add(new FaunaDietLink
                {
                    TargetKind = FaunaDietTargetKind.FaunaSpecies,
                    TargetSpeciesId = preyTargets[0].Key,
                    Weight = 0.18f,
                    IsFallback = true
                });
            }
        }
        else
        {
            foreach (var target in preyTargets)
            {
                links.Add(new FaunaDietLink
                {
                    TargetKind = FaunaDietTargetKind.FaunaSpecies,
                    TargetSpeciesId = target.Key,
                    Weight = (float)(target.Support / Math.Max(1.0, preyTargets.Sum(entry => entry.Support))),
                    IsFallback = false
                });
            }

            if (floraTargets.Length > 0)
            {
                links.Add(new FaunaDietLink
                {
                    TargetKind = FaunaDietTargetKind.FloraSpecies,
                    TargetSpeciesId = floraTargets[0].Key,
                    Weight = 0.20f,
                    IsFallback = true
                });
            }
        }

        return links
            .Where(link => !string.IsNullOrWhiteSpace(link.TargetSpeciesId))
            .ToArray();
    }

    private static float ResolveProtoFaunaFloraSupport(Region region, FloraSpeciesCatalog floraCatalog, ProtoLifeSubstrate substrate)
    {
        return Math.Clamp(
            SubsistenceSupportModel.CalculateFloraEcologySupport(region, floraCatalog) /
            Math.Max(1.0f, substrate.ProtoFaunaCapacity * ProtoLifePressureConstants.FaunaCapacityPopulationScale),
            0.0f,
            1.0f);
    }

    private static float ResolveProtoFaunaPreySupport(
        IDictionary<string, int> faunaPopulations,
        FaunaSpeciesCatalog faunaCatalog,
        ProtoLifeSubstrate substrate)
    {
        var support = faunaPopulations.Sum(entry =>
        {
            var fauna = faunaCatalog.GetById(entry.Key);
            return fauna is null ? 0.0 : entry.Value * fauna.FoodYield * fauna.PredatorVulnerability * 0.70f;
        });

        return Math.Clamp(
            (float)(support / Math.Max(1.0f, substrate.ProtoFaunaCapacity * ProtoLifePressureConstants.FaunaCapacityPopulationScale)),
            0.0f,
            1.0f);
    }

    private static SapientSpeciesDefinition CreateSapientSpecies(
        FaunaSpeciesDefinition parent,
        Region region,
        RegionalBiologicalProfile profile,
        int year,
        int month)
    {
        var regionRoot = region.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? region.Name;
        return new SapientSpeciesDefinition
        {
            Id = $"sapient-emergent-{parent.Id}-{region.Id}-{year:D3}{month:D2}",
            Name = $"{regionRoot} Speakers",
            SpeciesClass = SpeciesClass.Sapient,
            EmergentFromFaunaSpeciesId = parent.Id,
            ParentSpeciesId = parent.Id,
            OriginRegionId = region.Id
        };
    }

    private static FloraSpeciesDefinition CreateProtoFloraSpecies(Region region, ProtoLifeSubstrate substrate, int year, int month)
    {
        var regionRoot = region.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? region.Name;
        var fertility = (float)region.Fertility;
        return new FloraSpeciesDefinition
        {
            Id = $"flora-genesis-{region.Id}-{year:D3}{month:D2}",
            Name = $"{regionRoot} Bloom",
            SpeciesClass = SpeciesClass.Flora,
            CoreBiomes = [region.Biome],
            SupportedWaterAvailabilities = [region.WaterAvailability],
            HabitatFertilityMin = Math.Clamp(fertility - 0.18f, 0.0f, 1.0f),
            HabitatFertilityMax = Math.Clamp(fertility + 0.18f, 0.10f, 1.0f),
            GrowthRate = Math.Clamp(0.32f + (substrate.ProtoFloraPressure * 0.40f), 0.20f, 0.82f),
            RecoveryRate = Math.Clamp(0.36f + (substrate.FloraSupportDeficit * 0.24f), 0.20f, 0.84f),
            UsableBiomass = Math.Clamp(0.22f + (fertility * 0.46f), 0.18f, 0.84f),
            ConsumptionResilience = Math.Clamp(0.30f + (substrate.EcologicalVacancy * 0.30f), 0.20f, 0.78f),
            SpreadTendency = Math.Clamp(0.28f + (substrate.ProtoFloraPressure * 0.34f), 0.20f, 0.82f),
            RegionalAbundance = Math.Clamp(0.16f + (substrate.ProtoFloraCapacity * 0.32f), 0.12f, 0.62f),
            Conspicuousness = Math.Clamp(0.34f + (fertility * 0.26f), 0.28f, 0.78f),
            BaselineTraits = CreateProtoTraitProfile(region, isFauna: false),
            OriginRegionId = region.Id
        };
    }

    private static FaunaSpeciesDefinition CreateProtoFaunaSpecies(
        Region region,
        ProtoLifeSubstrate substrate,
        IReadOnlyList<FaunaDietLink> dietLinks,
        double floraSupport,
        double preySupport,
        int year,
        int month)
    {
        var regionRoot = region.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? region.Name;
        var fertility = (float)region.Fertility;
        var herbivoreLeaning = floraSupport >= preySupport;
        return new FaunaSpeciesDefinition
        {
            Id = $"fauna-genesis-{region.Id}-{year:D3}{month:D2}",
            Name = herbivoreLeaning ? $"{regionRoot} Grazer" : $"{regionRoot} Hunter",
            SpeciesClass = SpeciesClass.Fauna,
            CoreBiomes = [region.Biome],
            SupportedWaterAvailabilities = [region.WaterAvailability],
            HabitatFertilityMin = Math.Clamp(fertility - 0.20f, 0.0f, 1.0f),
            HabitatFertilityMax = Math.Clamp(fertility + 0.20f, 0.12f, 1.0f),
            DietCategory = herbivoreLeaning ? DietCategory.Herbivore : (floraSupport > 0.0 && preySupport > 0.0 ? DietCategory.Omnivore : DietCategory.Carnivore),
            DietLinks = dietLinks.ToArray(),
            RequiredIntake = Math.Clamp(0.24f + (substrate.ProtoFaunaPressure * 0.24f), 0.22f, 0.64f),
            ReproductionRate = Math.Clamp(0.22f + (substrate.ProtoFaunaPressure * 0.18f), 0.18f, 0.56f),
            MortalitySensitivity = Math.Clamp(0.34f + ((1.0f - substrate.ProtoFaunaCapacity) * 0.24f), 0.24f, 0.70f),
            Mobility = Math.Clamp(0.30f + (substrate.EcologicalVacancy * 0.28f), 0.22f, 0.78f),
            FeedingEfficiency = Math.Clamp(0.42f + ((float)Math.Max(floraSupport, preySupport) * 0.20f), 0.34f, 0.82f),
            PredatorVulnerability = herbivoreLeaning ? 0.62f : 0.34f,
            RegionalAbundance = Math.Clamp(0.12f + (substrate.ProtoFaunaCapacity * 0.22f), 0.10f, 0.48f),
            Conspicuousness = herbivoreLeaning ? 0.50f : 0.58f,
            FoodYield = herbivoreLeaning ? 0.28f : 0.34f,
            BaselineTraits = CreateProtoTraitProfile(region, isFauna: true),
            OriginRegionId = region.Id
        };
    }

    private static BiologicalTraitProfile CreateProtoTraitProfile(Region region, bool isFauna)
    {
        var cold = region.Biome is Biome.Tundra or Biome.Highlands ? 62 : 40;
        var heat = region.Biome == Biome.Desert ? 66 : 46;
        var drought = region.WaterAvailability == WaterAvailability.Low ? 62 : 36;
        return new BiologicalTraitProfile
        {
            ColdTolerance = cold,
            HeatTolerance = heat,
            DroughtTolerance = drought,
            Flexibility = isFauna ? 48 : 42,
            BodySize = isFauna ? 42 : 36,
            Reproduction = isFauna ? 44 : 52,
            Mobility = isFauna ? 54 : 18,
            Defense = isFauna ? 40 : 28,
            Resilience = 48
        };
    }

    private void UpdateFloraProfiles(
        Region region,
        FloraSpeciesCatalog floraCatalog,
        IReadOnlyList<FloraPopulationChange> changes,
        IDictionary<string, int> populations,
        IDictionary<string, RegionalBiologicalProfile> profiles,
        IList<FossilRecord> fossils,
        IList<BiologicalHistoryRecord> history,
        ICollection<BiologicalHistoryChange> chronicleChanges,
        ICollection<(string RegionId, string SpeciesId, string SpeciesName, bool IsFlora)> extinctionCandidates,
        int year,
        int month)
    {
        foreach (var change in changes)
        {
            var species = floraCatalog.GetById(change.FloraSpeciesId);
            if (species is null)
            {
                continue;
            }

            var profile = ResolveProfile(profiles, species.Id, region.Id, species.BaselineTraits);
            UpdateProfileFromFloraChange(region, change, profile, species.BaselineTraits);
            MaybeCreateFloraSpeciation(region, floraCatalog, populations, profiles, profile, species, history, chronicleChanges, year, month);
            if (change.NewPopulation == 0)
            {
                HandleRegionalExtinction(region, species.Id, species.Name, true, species.ParentSpeciesId, profile, fossils, history, chronicleChanges, extinctionCandidates, year, month);
            }
        }
    }

    private void UpdateFaunaProfiles(
        Region region,
        FaunaSpeciesCatalog faunaCatalog,
        SapientSpeciesCatalog sapientCatalog,
        IReadOnlyList<FaunaPopulationChange> changes,
        IDictionary<string, int> populations,
        IDictionary<string, RegionalBiologicalProfile> profiles,
        IList<FossilRecord> fossils,
        IList<BiologicalHistoryRecord> history,
        ICollection<BiologicalHistoryChange> chronicleChanges,
        ICollection<(string RegionId, string SpeciesId, string SpeciesName, bool IsFlora)> extinctionCandidates,
        int seed,
        int year,
        int month)
    {
        var carnivorePopulation = region.Ecosystem.FaunaPopulations
            .Where(entry => faunaCatalog.GetById(entry.Key)?.DietCategory == DietCategory.Carnivore)
            .Sum(entry => entry.Value);

        foreach (var change in changes)
        {
            var species = faunaCatalog.GetById(change.FaunaSpeciesId);
            if (species is null)
            {
                continue;
            }

            var profile = ResolveProfile(profiles, species.Id, region.Id, species.BaselineTraits);
            UpdateProfileFromFaunaChange(region, change, profile, species.BaselineTraits, carnivorePopulation);
            MaybeCreateSapientEmergence(region, sapientCatalog, profile, species, history, chronicleChanges, seed, year, month);
            MaybeCreateFaunaSpeciation(region, faunaCatalog, populations, profiles, profile, species, history, chronicleChanges, year, month);
            if (change.NewPopulation == 0)
            {
                HandleRegionalExtinction(region, species.Id, species.Name, false, species.ParentSpeciesId, profile, fossils, history, chronicleChanges, extinctionCandidates, year, month);
            }
        }
    }

    private static RegionalBiologicalProfile ResolveProfile(
        IDictionary<string, RegionalBiologicalProfile> profiles,
        string speciesId,
        string regionId,
        BiologicalTraitProfile baselineTraits)
    {
        if (profiles.TryGetValue(speciesId, out var existing))
        {
            return existing;
        }

        var created = new RegionalBiologicalProfile
        {
            SpeciesId = speciesId,
            RegionId = regionId,
            Traits = baselineTraits.Clone(),
            ViabilityScore = 50
        };
        profiles[speciesId] = created;
        return created;
    }

    private static void UpdateProfileFromFloraChange(Region region, FloraPopulationChange change, RegionalBiologicalProfile profile, BiologicalTraitProfile baseline)
    {
        profile.LastPopulation = change.NewPopulation;
        if (change.NewPopulation > 0)
        {
            profile.IsExtinct = false;
            profile.ContinuityMonths++;
            if (change.NewPopulation >= change.PreviousPopulation || change.TargetPopulation >= change.PreviousPopulation)
            {
                profile.SuccessfulMonths++;
            }
            else
            {
                profile.SuccessfulMonths = Math.Max(0, profile.SuccessfulMonths - 1);
            }
        }

        profile.PressureMemory.ColdStress = DriftPressure(profile.PressureMemory.ColdStress, region.Biome is Biome.Tundra or Biome.Highlands && profile.Traits.ColdTolerance < 60);
        profile.PressureMemory.HeatStress = DriftPressure(profile.PressureMemory.HeatStress, region.Biome == Biome.Desert && profile.Traits.HeatTolerance < 60);
        profile.PressureMemory.DroughtStress = DriftPressure(profile.PressureMemory.DroughtStress, region.WaterAvailability == WaterAvailability.Low && profile.Traits.DroughtTolerance < 60);
        profile.PressureMemory.ScarcityPressure = DriftPressure(profile.PressureMemory.ScarcityPressure, change.NewPopulation < change.PreviousPopulation || change.BiologicalFit < 0.80f);
        profile.PressureMemory.CompetitionPressure = DriftPressure(profile.PressureMemory.CompetitionPressure, change.TargetPopulation < change.PreviousPopulation);
        profile.PressureMemory.TerrainPressure = DriftPressure(profile.PressureMemory.TerrainPressure, !change.CoreBiomeFit);

        ApplyTraitDrift(profile.Traits, baseline, profile.PressureMemory, profile.ContinuityMonths);
        UpdateDivergence(profile, baseline);
    }

    private static void UpdateProfileFromFaunaChange(Region region, FaunaPopulationChange change, RegionalBiologicalProfile profile, BiologicalTraitProfile baseline, int carnivorePopulation)
    {
        profile.LastPopulation = change.NewPopulation;
        if (change.NewPopulation > 0)
        {
            profile.IsExtinct = false;
            profile.ContinuityMonths++;
            if (change.NewPopulation >= change.PreviousPopulation && change.FulfillmentRatio >= 0.70f && change.HabitatSupport >= 0.75f)
            {
                profile.SuccessfulMonths++;
            }
            else
            {
                profile.SuccessfulMonths = Math.Max(0, profile.SuccessfulMonths - 1);
            }
        }

        profile.PressureMemory.ColdStress = DriftPressure(profile.PressureMemory.ColdStress, region.Biome is Biome.Tundra or Biome.Highlands && profile.Traits.ColdTolerance < 60);
        profile.PressureMemory.HeatStress = DriftPressure(profile.PressureMemory.HeatStress, region.Biome == Biome.Desert && profile.Traits.HeatTolerance < 60);
        profile.PressureMemory.DroughtStress = DriftPressure(profile.PressureMemory.DroughtStress, region.WaterAvailability == WaterAvailability.Low && profile.Traits.DroughtTolerance < 60);
        profile.PressureMemory.ScarcityPressure = DriftPressure(profile.PressureMemory.ScarcityPressure, change.FulfillmentRatio < 0.70f || change.NewPopulation < change.PreviousPopulation);
        profile.PressureMemory.CompetitionPressure = DriftPressure(profile.PressureMemory.CompetitionPressure, change.HabitatSupport < 0.85f && change.NewPopulation < change.PreviousPopulation);
        profile.PressureMemory.PredatorPressure = DriftPressure(profile.PressureMemory.PredatorPressure, carnivorePopulation > Math.Max(20, change.PreviousPopulation / 2));
        profile.PressureMemory.TerrainPressure = DriftPressure(profile.PressureMemory.TerrainPressure, change.HabitatSupport < 0.70f);
        profile.StableSupportMonths = change.FoodStressState == nameof(FoodStressState.FedStable) &&
                                      change.FulfillmentRatio >= 0.92f &&
                                      change.HabitatSupport >= 0.78f
            ? profile.StableSupportMonths + 1
            : Math.Max(0, profile.StableSupportMonths - 1);
        profile.SocialCoordinationPressureMonths = ResolveSocialCoordinationPressure(profile, baseline, change)
            ? profile.SocialCoordinationPressureMonths + 1
            : Math.Max(0, profile.SocialCoordinationPressureMonths - 1);
        profile.LearningPressureMonths = ResolveLearningPressure(profile, baseline, change)
            ? profile.LearningPressureMonths + 1
            : Math.Max(0, profile.LearningPressureMonths - 1);
        profile.SapienceProgress = ResolveSapienceProgress(profile, baseline, change);

        ApplyTraitDrift(profile.Traits, baseline, profile.PressureMemory, profile.ContinuityMonths);
        profile.ViabilityScore = Math.Clamp((int)Math.Round((change.FulfillmentRatio * 45.0f) + (change.HabitatSupport * 35.0f) + (change.BiologicalFit * 20.0f), MidpointRounding.AwayFromZero), 0, 100);
        UpdateDivergence(profile, baseline);
    }

    private static bool ResolveSocialCoordinationPressure(RegionalBiologicalProfile profile, BiologicalTraitProfile baseline, FaunaPopulationChange change)
    {
        return change.NewPopulation >= SapienceEmergenceConstants.MinimumPopulation &&
               change.FoodStressState == nameof(FoodStressState.FedStable) &&
               baseline.Mobility >= SapienceEmergenceConstants.MinimumMobility &&
               baseline.Flexibility >= SapienceEmergenceConstants.MinimumFlexibility - 6 &&
               (profile.PressureMemory.PredatorPressure >= 10 || profile.PressureMemory.CompetitionPressure >= 10 || profile.PressureMemory.TerrainPressure >= 10);
    }

    private static bool ResolveLearningPressure(RegionalBiologicalProfile profile, BiologicalTraitProfile baseline, FaunaPopulationChange change)
    {
        return change.FoodStressState == nameof(FoodStressState.FedStable) &&
               change.FulfillmentRatio >= 0.88f &&
               baseline.Flexibility >= SapienceEmergenceConstants.MinimumFlexibility &&
               (profile.PressureMemory.ScarcityPressure >= 10 || profile.PressureMemory.TerrainPressure >= 10 || profile.DivergenceScore >= 42);
    }

    private static int ResolveSapienceProgress(RegionalBiologicalProfile profile, BiologicalTraitProfile baseline, FaunaPopulationChange change)
    {
        if (string.Equals(change.FoodStressState, nameof(FoodStressState.SevereShortage), StringComparison.Ordinal) ||
            string.Equals(change.FoodStressState, nameof(FoodStressState.Starvation), StringComparison.Ordinal) ||
            change.NewPopulation < SapienceEmergenceConstants.MinimumPopulation / 2)
        {
            return Math.Max(0, profile.SapienceProgress - 2);
        }

        var criteriaMet =
            profile.ContinuityMonths >= SapienceEmergenceConstants.MinimumContinuityMonths &&
            profile.SuccessfulMonths >= SapienceEmergenceConstants.MinimumSuccessfulMonths &&
            profile.StableSupportMonths >= SapienceEmergenceConstants.MinimumStableSupportMonths &&
            profile.SocialCoordinationPressureMonths >= SapienceEmergenceConstants.MinimumCoordinationPressureMonths &&
            profile.LearningPressureMonths >= SapienceEmergenceConstants.MinimumLearningPressureMonths &&
            profile.ViabilityScore >= SapienceEmergenceConstants.MinimumViabilityScore &&
            baseline.Flexibility >= SapienceEmergenceConstants.MinimumFlexibility &&
            baseline.Mobility >= SapienceEmergenceConstants.MinimumMobility &&
            !profile.IsExtinct;

        if (!criteriaMet)
        {
            return Math.Max(0, profile.SapienceProgress - 1);
        }

        var gain = Math.Min(
            SapienceEmergenceConstants.MonthlyProgressGainCap,
            1.0f +
            ((profile.StableSupportMonths - SapienceEmergenceConstants.MinimumStableSupportMonths) * 0.05f) +
            ((profile.SocialCoordinationPressureMonths - SapienceEmergenceConstants.MinimumCoordinationPressureMonths) * 0.04f) +
            ((profile.LearningPressureMonths - SapienceEmergenceConstants.MinimumLearningPressureMonths) * 0.04f) +
            (profile.ViabilityScore - SapienceEmergenceConstants.MinimumViabilityScore) * 0.03f);

        return Math.Min(100, profile.SapienceProgress + (int)Math.Round(gain, MidpointRounding.AwayFromZero));
    }

    private void MaybeCreateSapientEmergence(
        Region region,
        SapientSpeciesCatalog sapientCatalog,
        RegionalBiologicalProfile profile,
        FaunaSpeciesDefinition parent,
        IList<BiologicalHistoryRecord> history,
        ICollection<BiologicalHistoryChange> chronicleChanges,
        int seed,
        int year,
        int month)
    {
        if (profile.IsExtinct ||
            profile.SapienceProgress < SapienceEmergenceConstants.SapienceProgressThreshold ||
            profile.ContinuityMonths < SapienceEmergenceConstants.MinimumContinuityMonths ||
            profile.SuccessfulMonths < SapienceEmergenceConstants.MinimumSuccessfulMonths ||
            profile.StableSupportMonths < SapienceEmergenceConstants.MinimumStableSupportMonths ||
            profile.SocialCoordinationPressureMonths < SapienceEmergenceConstants.MinimumCoordinationPressureMonths ||
            profile.LearningPressureMonths < SapienceEmergenceConstants.MinimumLearningPressureMonths ||
            profile.ViabilityScore < SapienceEmergenceConstants.MinimumViabilityScore ||
            profile.LastPopulation < SapienceEmergenceConstants.MinimumPopulation ||
            profile.FoodStressState is FoodStressState.SevereShortage or FoodStressState.Starvation ||
            sapientCatalog.Definitions.Any(definition =>
                string.Equals(definition.EmergentFromFaunaSpeciesId, parent.Id, StringComparison.Ordinal) &&
                string.Equals(definition.OriginRegionId, region.Id, StringComparison.Ordinal)))
        {
            return;
        }

        var triggerChance = Math.Min(
            SapienceEmergenceConstants.MonthlyEmergenceChanceCap,
            0.006f +
            ((profile.SapienceProgress - SapienceEmergenceConstants.SapienceProgressThreshold) * 0.00055f) +
            ((profile.StableSupportMonths - SapienceEmergenceConstants.MinimumStableSupportMonths) * 0.00025f) +
            ((profile.SocialCoordinationPressureMonths - SapienceEmergenceConstants.MinimumCoordinationPressureMonths) * 0.00025f) +
            ((profile.LearningPressureMonths - SapienceEmergenceConstants.MinimumLearningPressureMonths) * 0.00025f));

        if (!PassesTrigger(seed, year, month, region.Id, $"sapience-emergence:{parent.Id}", triggerChance))
        {
            return;
        }

        var sapient = CreateSapientSpecies(parent, region, profile, year, month);
        sapientCatalog.AddOrReplace(sapient);
        profile.SapienceProgress = 0;
        var message = $"{sapient.Name} emerged from the {parent.Name.ToLowerInvariant()} lineage in {region.Name}.";
        history.Add(CreateHistory(region.Id, sapient.Id, "sapience-emergence", year, month, message));
        chronicleChanges.Add(new BiologicalHistoryChange
        {
            RegionId = region.Id,
            RegionName = region.Name,
            SpeciesId = sapient.Id,
            SpeciesName = sapient.Name,
            Kind = "SapienceEmergence",
            Message = $"{sapient.Name} emerged as a sapient lineage from {parent.Name.ToLowerInvariant()} in {region.Name}."
        });
    }

    private static void ApplyTraitDrift(BiologicalTraitProfile traits, BiologicalTraitProfile baseline, BiologicalPressureMemory pressure, int continuityMonths)
    {
        if (continuityMonths < 6)
        {
            return;
        }

        if (pressure.ColdStress >= 16)
        {
            Shift(traits, value => value.ColdTolerance += 1, value => value.HeatTolerance -= 1);
            if (pressure.ColdStress >= 28)
            {
                Shift(traits, value => value.BodySize += 1, value => value.Reproduction -= 1);
            }
        }

        if (pressure.HeatStress >= 16)
        {
            Shift(traits, value => value.HeatTolerance += 1, value => value.ColdTolerance -= 1);
            if (pressure.HeatStress >= 28)
            {
                Shift(traits, value => value.BodySize -= 1, value => value.Resilience -= 1);
            }
        }

        if (pressure.DroughtStress >= 16)
        {
            Shift(traits, value => value.DroughtTolerance += 1, value => value.BodySize -= 1);
            Shift(traits, value => value.Resilience += 1, value => value.Reproduction -= 1);
        }

        if (pressure.ScarcityPressure >= 16)
        {
            Shift(traits, value => value.Flexibility += 1, value => value.BodySize -= 1);
            Shift(traits, value => value.Resilience += 1, value => value.Reproduction -= 1);
        }

        if (pressure.PredatorPressure >= 16)
        {
            Shift(traits, value => value.Defense += 1, value => value.Reproduction -= 1);
            Shift(traits, value => value.Mobility += 1, value => value.BodySize -= 1);
        }

        if (pressure.CompetitionPressure >= 16)
        {
            Shift(traits, value => value.BodySize += 1, value => value.Reproduction -= 1);
        }

        if (pressure.TerrainPressure >= 16)
        {
            Shift(traits, value => value.Mobility += 1, value => value.Defense -= 1);
        }

        DriftTowardBaseline(traits, baseline, pressure);
    }

    private static void DriftTowardBaseline(BiologicalTraitProfile traits, BiologicalTraitProfile baseline, BiologicalPressureMemory pressure)
    {
        if (pressure.ColdStress < 8)
        {
            traits.ColdTolerance = MoveToward(traits.ColdTolerance, baseline.ColdTolerance);
        }

        if (pressure.HeatStress < 8)
        {
            traits.HeatTolerance = MoveToward(traits.HeatTolerance, baseline.HeatTolerance);
        }

        if (pressure.DroughtStress < 8)
        {
            traits.DroughtTolerance = MoveToward(traits.DroughtTolerance, baseline.DroughtTolerance);
        }

        if (pressure.ScarcityPressure < 8)
        {
            traits.Flexibility = MoveToward(traits.Flexibility, baseline.Flexibility);
        }
    }

    private static void UpdateDivergence(RegionalBiologicalProfile profile, BiologicalTraitProfile baseline)
    {
        var distance = profile.Traits.DistanceFrom(baseline);
        var distanceScore = Math.Clamp((int)Math.Round(distance / 4.5, MidpointRounding.AwayFromZero), 0, 100);
        var persistenceScore = Math.Clamp(profile.ContinuityMonths / 4, 0, 25);
        var successScore = Math.Clamp(profile.SuccessfulMonths / 3, 0, 25);
        profile.DivergenceScore = Math.Clamp((int)Math.Round((distanceScore * 0.55) + persistenceScore + successScore, MidpointRounding.AwayFromZero), 0, 100);
        profile.DivergenceStage = profile.DivergenceScore switch
        {
            >= 70 => DivergenceStage.PreSpeciation,
            >= 40 => DivergenceStage.StrongRegionalVariant,
            _ => DivergenceStage.MinorVariation
        };
    }

    private void MaybeCreateFloraSpeciation(
        Region region,
        FloraSpeciesCatalog floraCatalog,
        IDictionary<string, int> populations,
        IDictionary<string, RegionalBiologicalProfile> profiles,
        RegionalBiologicalProfile profile,
        FloraSpeciesDefinition parent,
        IList<BiologicalHistoryRecord> history,
        ICollection<BiologicalHistoryChange> chronicleChanges,
        int year,
        int month)
    {
        if (profile.IsExtinct || profile.DivergenceStage != DivergenceStage.PreSpeciation || profile.ContinuityMonths < 48 || profile.SuccessfulMonths < 18 || profile.SpeciationProgress < 35 && ++profile.SpeciationProgress < 36 || profile.LastPopulation < 20)
        {
            return;
        }

        var child = CreateFloraChildSpecies(parent, region, profile, year, month);
        floraCatalog.AddOrReplace(child);
        var childPopulation = Math.Max(1, (int)Math.Round(profile.LastPopulation * 0.65, MidpointRounding.AwayFromZero));
        populations[parent.Id] = Math.Max(1, profile.LastPopulation - childPopulation);
        populations[child.Id] = childPopulation;
        profiles[child.Id] = new RegionalBiologicalProfile
        {
            SpeciesId = child.Id,
            RegionId = region.Id,
            Traits = profile.Traits.Clone(),
            ContinuityMonths = profile.ContinuityMonths / 2,
            SuccessfulMonths = profile.SuccessfulMonths / 2,
            ViabilityScore = profile.ViabilityScore,
            LastPopulation = childPopulation
        };
        profile.DivergenceScore = 20;
        profile.SpeciationProgress = 0;
        profile.SuccessfulMonths = Math.Max(0, profile.SuccessfulMonths / 2);
        var message = $"{child.Name} emerged as a distinct branch in {region.Name}.";
        history.Add(CreateHistory(region.Id, child.Id, "speciation", year, month, message));
        chronicleChanges.Add(new BiologicalHistoryChange
        {
            RegionId = region.Id,
            RegionName = region.Name,
            SpeciesId = child.Id,
            SpeciesName = child.Name,
            Kind = "Speciation",
            Message = $"A {child.Name.ToLowerInvariant()} branch emerged in {region.Name}."
        });
    }

    private void MaybeCreateFaunaSpeciation(
        Region region,
        FaunaSpeciesCatalog faunaCatalog,
        IDictionary<string, int> populations,
        IDictionary<string, RegionalBiologicalProfile> profiles,
        RegionalBiologicalProfile profile,
        FaunaSpeciesDefinition parent,
        IList<BiologicalHistoryRecord> history,
        ICollection<BiologicalHistoryChange> chronicleChanges,
        int year,
        int month)
    {
        if (profile.IsExtinct || profile.DivergenceStage != DivergenceStage.PreSpeciation || profile.ContinuityMonths < 60 || profile.SuccessfulMonths < 24 || profile.ViabilityScore < 60 || profile.SpeciationProgress < 47 && ++profile.SpeciationProgress < 48 || profile.LastPopulation < 16)
        {
            return;
        }

        var child = CreateFaunaChildSpecies(parent, region, profile, year, month);
        faunaCatalog.AddOrReplace(child);
        var childPopulation = Math.Max(1, (int)Math.Round(profile.LastPopulation * 0.60, MidpointRounding.AwayFromZero));
        populations[parent.Id] = Math.Max(1, profile.LastPopulation - childPopulation);
        populations[child.Id] = childPopulation;
        profiles[child.Id] = new RegionalBiologicalProfile
        {
            SpeciesId = child.Id,
            RegionId = region.Id,
            Traits = profile.Traits.Clone(),
            ContinuityMonths = profile.ContinuityMonths / 2,
            SuccessfulMonths = profile.SuccessfulMonths / 2,
            ViabilityScore = profile.ViabilityScore,
            LastPopulation = childPopulation
        };
        profile.DivergenceScore = 20;
        profile.SpeciationProgress = 0;
        profile.SuccessfulMonths = Math.Max(0, profile.SuccessfulMonths / 2);
        var message = $"{child.Name} emerged as a distinct branch in {region.Name}.";
        history.Add(CreateHistory(region.Id, child.Id, "speciation", year, month, message));
        chronicleChanges.Add(new BiologicalHistoryChange
        {
            RegionId = region.Id,
            RegionName = region.Name,
            SpeciesId = child.Id,
            SpeciesName = child.Name,
            Kind = "Speciation",
            Message = $"A heat-adapted branch of {parent.Name.ToLowerInvariant()} emerged in {region.Name}."
        });
    }

    private static FloraSpeciesDefinition CreateFloraChildSpecies(FloraSpeciesDefinition parent, Region region, RegionalBiologicalProfile profile, int year, int month)
    {
        var id = $"{parent.Id}-branch-{region.Id}-{year:D3}{month:D2}";
        return new FloraSpeciesDefinition
        {
            Id = id,
            Name = $"{region.Name.Split(' ')[0]} {parent.Name}",
            CoreBiomes = [region.Biome],
            SupportedWaterAvailabilities = [region.WaterAvailability],
            HabitatFertilityMin = parent.HabitatFertilityMin,
            HabitatFertilityMax = parent.HabitatFertilityMax,
            GrowthRate = parent.GrowthRate,
            RecoveryRate = parent.RecoveryRate,
            UsableBiomass = parent.UsableBiomass,
            ConsumptionResilience = parent.ConsumptionResilience,
            SpreadTendency = parent.SpreadTendency,
            RegionalAbundance = parent.RegionalAbundance,
            Conspicuousness = parent.Conspicuousness,
            BaselineTraits = profile.Traits.Clone(),
            ParentSpeciesId = parent.Id,
            OriginRegionId = region.Id
        };
    }

    private static FaunaSpeciesDefinition CreateFaunaChildSpecies(FaunaSpeciesDefinition parent, Region region, RegionalBiologicalProfile profile, int year, int month)
    {
        var id = $"{parent.Id}-branch-{region.Id}-{year:D3}{month:D2}";
        return new FaunaSpeciesDefinition
        {
            Id = id,
            Name = $"{region.Name.Split(' ')[0]} {parent.Name}",
            CoreBiomes = [region.Biome],
            SupportedWaterAvailabilities = [region.WaterAvailability],
            HabitatFertilityMin = parent.HabitatFertilityMin,
            HabitatFertilityMax = parent.HabitatFertilityMax,
            DietCategory = parent.DietCategory,
            DietLinks = parent.DietLinks.Select(link => new FaunaDietLink
            {
                TargetKind = link.TargetKind,
                TargetSpeciesId = link.TargetSpeciesId,
                Weight = link.Weight,
                IsFallback = link.IsFallback
            }).ToArray(),
            RequiredIntake = parent.RequiredIntake,
            ReproductionRate = parent.ReproductionRate,
            MortalitySensitivity = parent.MortalitySensitivity,
            Mobility = parent.Mobility,
            FeedingEfficiency = parent.FeedingEfficiency,
            PredatorVulnerability = parent.PredatorVulnerability,
            RegionalAbundance = parent.RegionalAbundance,
            Conspicuousness = parent.Conspicuousness,
            FoodYield = parent.FoodYield,
            BaselineTraits = profile.Traits.Clone(),
            ParentSpeciesId = parent.Id,
            OriginRegionId = region.Id
        };
    }

    private static void HandleRegionalExtinction(
        Region region,
        string speciesId,
        string speciesName,
        bool isFlora,
        string parentSpeciesId,
        RegionalBiologicalProfile profile,
        IList<FossilRecord> fossils,
        IList<BiologicalHistoryRecord> history,
        ICollection<BiologicalHistoryChange> chronicleChanges,
        ICollection<(string RegionId, string SpeciesId, string SpeciesName, bool IsFlora)> extinctionCandidates,
        int year,
        int month)
    {
        if (profile.IsExtinct)
        {
            return;
        }

        profile.IsExtinct = true;
        profile.SpeciationProgress = 0;
        var message = $"{speciesName} vanished from {region.Name}.";
        history.Add(CreateHistory(region.Id, speciesId, "regional-extinction", year, month, message));
        if (profile.DivergenceScore >= 40 || profile.ContinuityMonths >= 24)
        {
            fossils.Add(new FossilRecord
            {
                Id = $"fossil:{region.Id}:{speciesId}:{year:D3}{month:D2}",
                RegionId = region.Id,
                SpeciesId = speciesId,
                ParentSpeciesId = parentSpeciesId,
                FormName = speciesName,
                TraitSummary = profile.Traits.ToSummary(),
                RecordedYear = year,
                RecordedMonth = month,
                CauseSummary = "Regional branch extinction",
                Significance = profile.DivergenceStage.ToString()
            });
        }

        if (!isFlora || profile.DivergenceScore >= 40)
        {
            chronicleChanges.Add(new BiologicalHistoryChange
            {
                RegionId = region.Id,
                RegionName = region.Name,
                SpeciesId = speciesId,
                SpeciesName = speciesName,
                Kind = "Extinction",
                Message = $"The old {speciesName.ToLowerInvariant()} vanished from {region.Name}."
            });
        }

        extinctionCandidates.Add((region.Id, speciesId, speciesName, isFlora));
    }

    private static void MarkGloballyExtinct(
        IReadOnlyList<Region> regions,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        IReadOnlyList<(string RegionId, string SpeciesId, string SpeciesName, bool IsFlora)> extinctionCandidates,
        ICollection<BiologicalHistoryChange> chronicleChanges,
        int year,
        int month)
    {
        var activeFloraIds = regions.SelectMany(region => region.Ecosystem.FloraPopulations.Keys).ToHashSet(StringComparer.Ordinal);
        var activeFaunaIds = regions.SelectMany(region => region.Ecosystem.FaunaPopulations.Keys).ToHashSet(StringComparer.Ordinal);

        foreach (var definition in floraCatalog.Definitions.Where(definition => !definition.IsExtinct && !activeFloraIds.Contains(definition.Id)).ToArray())
        {
            floraCatalog.AddOrReplace(new FloraSpeciesDefinition
            {
                Id = definition.Id,
                Name = definition.Name,
                CoreBiomes = definition.CoreBiomes,
                SupportedWaterAvailabilities = definition.SupportedWaterAvailabilities,
                HabitatFertilityMin = definition.HabitatFertilityMin,
                HabitatFertilityMax = definition.HabitatFertilityMax,
                GrowthRate = definition.GrowthRate,
                RecoveryRate = definition.RecoveryRate,
                UsableBiomass = definition.UsableBiomass,
                ConsumptionResilience = definition.ConsumptionResilience,
                SpreadTendency = definition.SpreadTendency,
                RegionalAbundance = definition.RegionalAbundance,
                Conspicuousness = definition.Conspicuousness,
                BaselineTraits = definition.BaselineTraits.Clone(),
                ParentSpeciesId = definition.ParentSpeciesId,
                OriginRegionId = definition.OriginRegionId,
                IsExtinct = true,
                ExtinctOnYear = year,
                ExtinctOnMonth = month
            });
        }

        foreach (var definition in faunaCatalog.Definitions.Where(definition => !definition.IsExtinct && !activeFaunaIds.Contains(definition.Id)).ToArray())
        {
            faunaCatalog.AddOrReplace(new FaunaSpeciesDefinition
            {
                Id = definition.Id,
                Name = definition.Name,
                CoreBiomes = definition.CoreBiomes,
                SupportedWaterAvailabilities = definition.SupportedWaterAvailabilities,
                HabitatFertilityMin = definition.HabitatFertilityMin,
                HabitatFertilityMax = definition.HabitatFertilityMax,
                DietCategory = definition.DietCategory,
                DietLinks = definition.DietLinks.Select(link => new FaunaDietLink
                {
                    TargetKind = link.TargetKind,
                    TargetSpeciesId = link.TargetSpeciesId,
                    Weight = link.Weight,
                    IsFallback = link.IsFallback
                }).ToArray(),
                RequiredIntake = definition.RequiredIntake,
                ReproductionRate = definition.ReproductionRate,
                MortalitySensitivity = definition.MortalitySensitivity,
                Mobility = definition.Mobility,
                FeedingEfficiency = definition.FeedingEfficiency,
                PredatorVulnerability = definition.PredatorVulnerability,
                RegionalAbundance = definition.RegionalAbundance,
                Conspicuousness = definition.Conspicuousness,
                FoodYield = definition.FoodYield,
                BaselineTraits = definition.BaselineTraits.Clone(),
                ParentSpeciesId = definition.ParentSpeciesId,
                OriginRegionId = definition.OriginRegionId,
                IsExtinct = true,
                ExtinctOnYear = year,
                ExtinctOnMonth = month
            });

            var extinction = extinctionCandidates.LastOrDefault(candidate => !candidate.IsFlora && string.Equals(candidate.SpeciesId, definition.Id, StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(extinction.SpeciesId))
            {
                chronicleChanges.Add(new BiologicalHistoryChange
                {
                    RegionId = extinction.RegionId,
                    RegionName = regions.First(region => string.Equals(region.Id, extinction.RegionId, StringComparison.Ordinal)).Name,
                    SpeciesId = extinction.SpeciesId,
                    SpeciesName = extinction.SpeciesName,
                    Kind = "SpeciesExtinction",
                    Message = $"{extinction.SpeciesName} disappeared entirely after prolonged decline."
                });
            }
        }
    }

    private static int DriftPressure(int current, bool active)
    {
        return active ? Math.Min(100, current + 2) : Math.Max(0, current - 1);
    }

    private static void Shift(BiologicalTraitProfile traits, Action<BiologicalTraitProfile> increase, Action<BiologicalTraitProfile> tradeoff)
    {
        increase(traits);
        tradeoff(traits);
        ClampTraits(traits);
    }

    private static int MoveToward(int current, int baseline)
    {
        if (current == baseline)
        {
            return current;
        }

        return current + (current < baseline ? 1 : -1);
    }

    private static void ClampTraits(BiologicalTraitProfile traits)
    {
        traits.ColdTolerance = Math.Clamp(traits.ColdTolerance, 0, 100);
        traits.HeatTolerance = Math.Clamp(traits.HeatTolerance, 0, 100);
        traits.DroughtTolerance = Math.Clamp(traits.DroughtTolerance, 0, 100);
        traits.Flexibility = Math.Clamp(traits.Flexibility, 0, 100);
        traits.BodySize = Math.Clamp(traits.BodySize, 0, 100);
        traits.Reproduction = Math.Clamp(traits.Reproduction, 0, 100);
        traits.Mobility = Math.Clamp(traits.Mobility, 0, 100);
        traits.Defense = Math.Clamp(traits.Defense, 0, 100);
        traits.Resilience = Math.Clamp(traits.Resilience, 0, 100);
    }

    private static Dictionary<string, RegionalBiologicalProfile> CloneProfiles(IReadOnlyDictionary<string, RegionalBiologicalProfile> profiles)
    {
        return profiles.ToDictionary(entry => entry.Key, entry => entry.Value.Clone(), StringComparer.Ordinal);
    }

    private static BiologicalHistoryRecord CreateHistory(string regionId, string speciesId, string eventKind, int year, int month, string message)
    {
        return new BiologicalHistoryRecord
        {
            Id = $"{eventKind}:{regionId}:{speciesId}:{year:D3}{month:D2}",
            RegionId = regionId,
            SpeciesId = speciesId,
            EventKind = eventKind,
            Year = year,
            Month = month,
            Message = message
        };
    }
}
