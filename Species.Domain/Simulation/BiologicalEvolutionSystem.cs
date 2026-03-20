using Species.Domain.Catalogs;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class BiologicalEvolutionSystem
{
    public BiologicalEvolutionResult Run(
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
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
            UpdateFaunaProfiles(region, faunaCatalog, faunaChangesByRegion.GetValueOrDefault(region.Id, Array.Empty<FaunaPopulationChange>()), mutableFauna, mutableFaunaProfiles, fossils, history, chronicleChanges, extinctionCandidates, world.CurrentYear, world.CurrentMonth);

            updatedRegions.Add(new Region(
                region.Id,
                region.Name,
                region.Fertility,
                region.Biome,
                region.WaterAvailability,
                region.NeighborIds,
                new RegionEcosystem(mutableFlora, mutableFauna, mutableFloraProfiles, mutableFaunaProfiles, fossils, history),
                region.MaterialProfile.Clone()));
        }

        MarkGloballyExtinct(updatedRegions, floraCatalog, faunaCatalog, extinctionCandidates, chronicleChanges, world.CurrentYear, world.CurrentMonth);

        return new BiologicalEvolutionResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, updatedRegions, world.PopulationGroups, world.Chronicle, world.Polities, world.FocalPolityId),
            chronicleChanges);
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
        IReadOnlyList<FaunaPopulationChange> changes,
        IDictionary<string, int> populations,
        IDictionary<string, RegionalBiologicalProfile> profiles,
        IList<FossilRecord> fossils,
        IList<BiologicalHistoryRecord> history,
        ICollection<BiologicalHistoryChange> chronicleChanges,
        ICollection<(string RegionId, string SpeciesId, string SpeciesName, bool IsFlora)> extinctionCandidates,
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

        ApplyTraitDrift(profile.Traits, baseline, profile.PressureMemory, profile.ContinuityMonths);
        profile.ViabilityScore = Math.Clamp((int)Math.Round((change.FulfillmentRatio * 45.0f) + (change.HabitatSupport * 35.0f) + (change.BiologicalFit * 20.0f), MidpointRounding.AwayFromZero), 0, 100);
        UpdateDivergence(profile, baseline);
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
            PreferredFertilityMin = parent.PreferredFertilityMin,
            PreferredFertilityMax = parent.PreferredFertilityMax,
            GrowthRate = parent.GrowthRate,
            FoodValue = parent.FoodValue,
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
            DietCategory = parent.DietCategory,
            FoodRequirement = parent.FoodRequirement,
            ReproductionRate = parent.ReproductionRate,
            MigrationTendency = parent.MigrationTendency,
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
                PreferredFertilityMin = definition.PreferredFertilityMin,
                PreferredFertilityMax = definition.PreferredFertilityMax,
                GrowthRate = definition.GrowthRate,
                FoodValue = definition.FoodValue,
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
                DietCategory = definition.DietCategory,
                FoodRequirement = definition.FoodRequirement,
                ReproductionRate = definition.ReproductionRate,
                MigrationTendency = definition.MigrationTendency,
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
