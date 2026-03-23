using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;
using Species.Domain.Catalogs;
using Species.Domain.Discovery;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Client.ViewModelFactories;

public static class KnownSpeciesViewModelFactory
{
    private static readonly string[] FloraColumns = ["Name", "Food Role", "Known Uses", "Outputs", "Habitat", "Seasonality"];
    private static readonly string[] FaunaColumns = ["Name", "Role", "Food Role", "Outputs", "Danger", "Habitat"];
    private static readonly string[] SapientColumns = ["Name", "Threat", "Location", "Known Traits"];

    public static int GetKnownSpeciesCount(
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        string focalPolityId)
    {
        return BuildSelectableSpecies(world, floraCatalog, faunaCatalog, focalPolityId).Count;
    }

    public static KnownSpeciesViewModel Build(
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        string focalPolityId,
        int selectedIndex,
        bool isSimulationRunning = false)
    {
        var isPrimitiveWorldMode = world.Polities.Count == 0;
        var focusPolity = PlayerFocus.Resolve(world, focalPolityId);
        
        // In primitive-world mode, show all seeded species
        var selectableSpecies = isPrimitiveWorldMode
            ? BuildAllSeededSpecies(world, floraCatalog, faunaCatalog)
            : BuildSelectableSpecies(world, floraCatalog, faunaCatalog, focalPolityId);
            
        var sections = BuildSections(selectableSpecies);
        var clampedIndex = selectableSpecies.Count == 0 ? 0 : Math.Clamp(selectedIndex, 0, selectableSpecies.Count - 1);

        var polityName = isPrimitiveWorldMode ? "Primitive World" : (focusPolity?.Name ?? "Unknown polity");

        return new KnownSpeciesViewModel(
            polityName,
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            isSimulationRunning,
            sections,
            selectableSpecies,
            selectableSpecies.Count == 0 ? null : selectableSpecies[clampedIndex],
            clampedIndex);
    }

    private static IReadOnlyList<KnownSpeciesSectionSummary> BuildSections(IReadOnlyList<KnownSpeciesSummary> species)
    {
        var flora = species.Where(item => item.SpeciesClass == SpeciesClass.Flora).ToArray();
        var fauna = species.Where(item => item.SpeciesClass == SpeciesClass.Fauna).ToArray();
        var sapients = species.Where(item => item.SpeciesClass == SpeciesClass.Sapient).ToArray();

        return
        [
            new KnownSpeciesSectionSummary("Flora", "No known flora", FloraColumns, flora),
            new KnownSpeciesSectionSummary("Fauna", "No known fauna", FaunaColumns, fauna),
            new KnownSpeciesSectionSummary("Sapients", "No known sapients", SapientColumns, sapients)
        ];
    }

    private static IReadOnlyList<KnownSpeciesSummary> BuildSelectableSpecies(
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        string focalPolityId)
    {
        var focusGroup = PlayerFocus.ResolveLeadGroup(world, focalPolityId);
        if (focusGroup is null)
        {
            return [];
        }

        var focusPolity = world.Polities.FirstOrDefault(polity => string.Equals(polity.Id, focusGroup.PolityId, StringComparison.Ordinal));
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var visibleRegionIds = ResolveVisibleRegionIds(world, focusGroup, focusPolity);
        var visibleRegions = visibleRegionIds
            .Select(regionId => regionsById.GetValueOrDefault(regionId))
            .Where(region => region is not null)
            .Cast<Region>()
            .ToArray();
        var summaries = new List<KnownSpeciesSummary>();

        summaries.AddRange(BuildFloraSummaries(visibleRegions, floraCatalog, focusPolity));
        summaries.AddRange(BuildFaunaSummaries(visibleRegions, faunaCatalog, focusPolity));
        summaries.AddRange(BuildSapientSummaries(world, focusGroup, focusPolity, regionsById, visibleRegionIds));

        return summaries
            .OrderBy(summary => summary.SpeciesClass)
            .ThenBy(summary => summary.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static HashSet<string> ResolveVisibleRegionIds(World world, PopulationGroup focusGroup, Polity? focusPolity)
    {
        return focusGroup.KnownRegionIds
            .Append(focusGroup.CurrentRegionId)
            .Concat(world.PopulationGroups
                .Where(group => focusPolity is not null && string.Equals(group.PolityId, focusPolity.Id, StringComparison.Ordinal))
                .Select(group => group.CurrentRegionId))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IReadOnlyList<KnownSpeciesSummary> BuildFloraSummaries(
        IReadOnlyList<Region> visibleRegions,
        FloraSpeciesCatalog floraCatalog,
        Polity? focusPolity)
    {
        if (focusPolity is null)
        {
            return [];
        }

        var floraById = new Dictionary<string, List<Region>>(StringComparer.Ordinal);
        foreach (var awareness in focusPolity.SpeciesAwareness
                     .Where(state => state.SpeciesClass == SpeciesClass.Flora && state.CurrentStage == DiscoveryStage.Discovered)
                     .OrderBy(state => state.SpeciesId, StringComparer.Ordinal))
        {
            foreach (var region in visibleRegions.Where(region => region.Ecosystem.FloraPopulations.GetValueOrDefault(awareness.SpeciesId) > 0))
            {
                if (!floraById.TryGetValue(awareness.SpeciesId, out var regions))
                {
                    regions = [];
                    floraById.Add(awareness.SpeciesId, regions);
                }

                regions.Add(region);
            }
        }

        return floraById
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => floraCatalog.GetById(entry.Key) is { } flora ? BuildFloraSummary(flora, entry.Value) : null)
            .Where(summary => summary is not null)
            .Cast<KnownSpeciesSummary>()
            .ToArray();
    }

    private static IReadOnlyList<KnownSpeciesSummary> BuildFaunaSummaries(
        IReadOnlyList<Region> visibleRegions,
        FaunaSpeciesCatalog faunaCatalog,
        Polity? focusPolity)
    {
        if (focusPolity is null)
        {
            return [];
        }

        var faunaById = new Dictionary<string, List<Region>>(StringComparer.Ordinal);
        foreach (var awareness in focusPolity.SpeciesAwareness
                     .Where(state => state.SpeciesClass == SpeciesClass.Fauna && state.CurrentStage == DiscoveryStage.Discovered)
                     .OrderBy(state => state.SpeciesId, StringComparer.Ordinal))
        {
            foreach (var region in visibleRegions.Where(region => region.Ecosystem.FaunaPopulations.GetValueOrDefault(awareness.SpeciesId) > 0))
            {
                if (!faunaById.TryGetValue(awareness.SpeciesId, out var regions))
                {
                    regions = [];
                    faunaById.Add(awareness.SpeciesId, regions);
                }

                regions.Add(region);
            }
        }

        return faunaById
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => faunaCatalog.GetById(entry.Key) is { } fauna ? BuildFaunaSummary(fauna, entry.Value) : null)
            .Where(summary => summary is not null)
            .Cast<KnownSpeciesSummary>()
            .ToArray();
    }

    private static IReadOnlyList<KnownSpeciesSummary> BuildSapientSummaries(
        World world,
        PopulationGroup focusGroup,
        Polity? focusPolity,
        IReadOnlyDictionary<string, Region> regionsById,
        IReadOnlySet<string> visibleRegionIds)
    {
        var encounterRegionIds = new HashSet<string>(visibleRegionIds, StringComparer.Ordinal);
        foreach (var regionId in visibleRegionIds)
        {
            if (!regionsById.TryGetValue(regionId, out var region))
            {
                continue;
            }

            foreach (var neighborId in region.NeighborIds)
            {
                encounterRegionIds.Add(neighborId);
            }
        }

        var sapientGroups = world.PopulationGroups
            .Where(group => group.SpeciesClass == SpeciesClass.Sapient)
            .Where(group =>
                string.Equals(group.PolityId, focusGroup.PolityId, StringComparison.Ordinal) ||
                encounterRegionIds.Contains(group.CurrentRegionId) ||
                focusPolity?.InterPolityRelations.Any(relation =>
                    string.Equals(relation.OtherPolityId, group.PolityId, StringComparison.Ordinal) &&
                    relation.ContactIntensity > 0) == true)
            .GroupBy(group => group.SpeciesId, StringComparer.Ordinal)
            .OrderBy(grouping => grouping.Key, StringComparer.Ordinal);

        var summaries = new List<KnownSpeciesSummary>();
        foreach (var speciesGroup in sapientGroups)
        {
            summaries.Add(BuildSapientSummary(speciesGroup.Key, speciesGroup.ToArray(), focusGroup, focusPolity, regionsById));
        }

        return summaries;
    }

    private static KnownSpeciesSummary BuildFloraSummary(FloraSpeciesDefinition flora, IReadOnlyList<Region> regions)
    {
        var distinctRegions = regions.DistinctBy(region => region.Id).ToArray();
        var habitat = BuildHabitat(distinctRegions);
        var foodRole = flora.UsableBiomass >= 0.65f
            ? "major forage"
            : flora.UsableBiomass >= 0.35f
                ? "supplemental forage"
                : "marginal forage";
        var uses = BuildFloraUses(flora);
        var outputs = BuildFloraOutputs(flora);
        var seasonality = BuildFloraSeasonality(flora);

        return new KnownSpeciesSummary(
            flora.Id,
            flora.Name,
            SpeciesClass.Flora,
            false,
            "Discovered",
            [flora.Name, foodRole, uses, outputs, habitat, seasonality],
            $"A discovered plant species known from {BuildRegionNames(distinctRegions)}.",
            [
                $"Known habitat: {habitat}",
                $"Water range: {BuildWaterSummary(flora.SupportedWaterAvailabilities)}",
                $"Observed range: {BuildRegionNames(distinctRegions)}"
            ],
            [
                $"Food role: {foodRole}",
                $"Known uses: {uses}",
                $"Outputs: {outputs}"
            ],
            [
                "Appears on the Known Species screen only after full discovery.",
                $"Seasonality is currently inferred as {seasonality} from growth and recovery."
            ]);
    }

    private static KnownSpeciesSummary BuildFaunaSummary(FaunaSpeciesDefinition fauna, IReadOnlyList<Region> regions)
    {
        var distinctRegions = regions.DistinctBy(region => region.Id).ToArray();
        var habitat = BuildHabitat(distinctRegions);
        var role = fauna.DietCategory switch
        {
            DietCategory.Carnivore => "predator",
            DietCategory.Herbivore => "grazer",
            _ => "omnivore"
        };
        var foodRole = fauna.DietCategory switch
        {
            DietCategory.Carnivore => fauna.FoodYield >= 0.45f ? "risky meat" : "dangerous prey",
            DietCategory.Herbivore => fauna.FoodYield >= 0.55f ? "major prey" : "common prey",
            _ => "opportunistic prey"
        };
        var outputs = BuildFaunaOutputs(fauna);
        var danger = BuildFaunaDanger(fauna);

        return new KnownSpeciesSummary(
            fauna.Id,
            fauna.Name,
            SpeciesClass.Fauna,
            false,
            "Discovered",
            [fauna.Name, role, foodRole, outputs, danger, habitat],
            $"A discovered animal species known from {BuildRegionNames(distinctRegions)}.",
            [
                $"Known habitat: {habitat}",
                $"Water range: {BuildWaterSummary(fauna.SupportedWaterAvailabilities)}",
                $"Observed range: {BuildRegionNames(distinctRegions)}"
            ],
            [
                $"Role: {role}",
                $"Food role: {foodRole}",
                $"Outputs: {outputs}"
            ],
            [
                "Appears on the Known Species screen only after full discovery.",
                $"{danger} danger is inferred from diet, mobility, and size."
            ]);
    }

    private static KnownSpeciesSummary BuildSapientSummary(
        string speciesId,
        IReadOnlyList<PopulationGroup> groups,
        PopulationGroup focusGroup,
        Polity? focusPolity,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        var isPlayerSpecies = string.Equals(speciesId, focusGroup.SpeciesId, StringComparison.Ordinal);
        var locations = groups
            .Select(group => regionsById.GetValueOrDefault(group.CurrentRegionId)?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .Take(3)
            .ToArray();
        var locationText = locations.Length == 0 ? "uncertain" : string.Join(", ", locations);
        var threat = isPlayerSpecies
            ? "ourselves"
            : BuildSapientThreat(groups, focusPolity);
        var knownTraits = BuildSapientKnownTraits(groups, isPlayerSpecies);
        var displayName = BuildPlayerSpeciesName(speciesId);

        return new KnownSpeciesSummary(
            speciesId,
            displayName,
            SpeciesClass.Sapient,
            isPlayerSpecies,
            isPlayerSpecies ? "Known" : "Encountered",
            [displayName, threat, locationText, knownTraits],
            isPlayerSpecies
                ? "Your polity's own sapient species."
                : $"An encountered sapient species known from {locationText}.",
            [
                $"Known location: {locationText}",
                $"Observed groups: {groups.Count}",
                $"Known population: {groups.Sum(group => group.Population):N0}"
            ],
            BuildSapientTraitLines(groups, isPlayerSpecies),
            [
                isPlayerSpecies
                    ? "Always present because it is your own species."
                    : "Sapients appear here after encounter or live contact, even before deeper social understanding.",
                focusPolity is null || isPlayerSpecies
                    ? "Diplomacy depth is intentionally deferred on this screen."
                    : BuildSapientContactContext(groups, focusPolity)
            ]);
    }

    private static IReadOnlyList<KnownSpeciesSummary> BuildAllSeededSpecies(
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var species = new List<KnownSpeciesSummary>();
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);

        // Gather all seeded flora species
        var seededFloraIds = world.Regions
            .SelectMany(r => r.Ecosystem.FloraPopulations.Keys)
            .Distinct()
            .ToArray();

        foreach (var speciesId in seededFloraIds)
        {
            var definition = floraCatalog.GetById(speciesId);
            if (definition is null) continue;

            var regionsWithSpecies = world.Regions
                .Where(r => r.Ecosystem.FloraPopulations.ContainsKey(speciesId))
                .ToArray();

            species.Add(new KnownSpeciesSummary(
                speciesId,
                definition.Name,
                SpeciesClass.Flora,
                ResolveFoodRole(definition.Tags),
                ResolveKnownUses([]),
                ResolveFloraOutputs(definition.Tags),
                ResolveFloraHabitat(definition),
                ResolveFloraDanger([]),
                ResolveFloraSeasonality(definition),
                null,
                null,
                null,
                regionsWithSpecies.Length,
                string.Join(", ", regionsWithSpecies.Take(3).Select(r => r.Name))));
        }

        // Gather all seeded fauna species
        var seededFaunaIds = world.Regions
            .SelectMany(r => r.Ecosystem.FaunaPopulations.Keys)
            .Distinct()
            .ToArray();

        foreach (var speciesId in seededFaunaIds)
        {
            var definition = faunaCatalog.GetById(speciesId);
            if (definition is null) continue;

            var regionsWithSpecies = world.Regions
                .Where(r => r.Ecosystem.FaunaPopulations.ContainsKey(speciesId))
                .ToArray();

            species.Add(new KnownSpeciesSummary(
                speciesId,
                definition.Name,
                SpeciesClass.Fauna,
                null,
                null,
                ResolveFaunaOutputs(definition.Tags),
                ResolveFaunaHabitat(definition),
                ResolveFaunaDanger(definition.Tags),
                null,
                ResolveFaunaRole(definition.DietCategory),
                ResolveFaunaFoodRole(definition.DietCategory),
                null,
                regionsWithSpecies.Length,
                string.Join(", ", regionsWithSpecies.Take(3).Select(r => r.Name))));
        }

        return species
            .OrderBy(s => s.SpeciesClass)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildFloraUses(FloraSpeciesDefinition flora)
    {
        var uses = new List<string>();
        if (flora.UsableBiomass >= 0.25f)
        {
            uses.Add("forage");
        }

        if (flora.SpreadTendency >= 0.60f || flora.ConsumptionResilience >= 0.55f)
        {
            uses.Add("fodder");
        }

        if (flora.BaselineTraits.BodySize >= 44 || flora.BaselineTraits.Defense >= 36)
        {
            uses.Add("cover");
        }

        return uses.Count == 0 ? "limited use" : string.Join(", ", uses.Take(2));
    }

    private static string BuildFloraOutputs(FloraSpeciesDefinition flora)
    {
        var outputs = new List<string>();
        if (flora.UsableBiomass >= 0.55f)
        {
            outputs.Add("food");
        }

        if (flora.BaselineTraits.BodySize >= 45)
        {
            outputs.Add("fiber");
        }

        if (flora.RecoveryRate >= 0.60f)
        {
            outputs.Add("fresh growth");
        }

        return outputs.Count == 0 ? "minor biomass" : string.Join(", ", outputs.Take(2));
    }

    private static string BuildFloraSeasonality(FloraSpeciesDefinition flora)
    {
        if (flora.GrowthRate >= 0.65f && flora.RecoveryRate >= 0.65f)
        {
            return "steady";
        }

        if (flora.GrowthRate >= 0.55f)
        {
            return "strong season";
        }

        if (flora.RecoveryRate <= 0.40f)
        {
            return "slow renewal";
        }

        return "moderate";
    }

    private static string BuildFaunaOutputs(FaunaSpeciesDefinition fauna)
    {
        var outputs = new List<string>();
        if (fauna.FoodYield >= 0.25f)
        {
            outputs.Add("meat");
        }

        if (fauna.BaselineTraits.BodySize >= 40 || fauna.FoodYield >= 0.45f)
        {
            outputs.Add("hide");
        }

        if (fauna.BaselineTraits.Defense >= 50)
        {
            outputs.Add("bone");
        }

        return outputs.Count == 0 ? "minor yield" : string.Join(", ", outputs.Take(2));
    }

    private static string BuildFaunaDanger(FaunaSpeciesDefinition fauna)
    {
        var dangerScore = 0;
        if (fauna.DietCategory == DietCategory.Carnivore)
        {
            dangerScore += 2;
        }

        if (fauna.Mobility >= 0.60f)
        {
            dangerScore++;
        }

        if (fauna.BaselineTraits.BodySize >= 55)
        {
            dangerScore++;
        }

        return dangerScore switch
        {
            >= 3 => "high",
            2 => "moderate",
            _ => "low"
        };
    }

    private static string BuildSapientThreat(IReadOnlyList<PopulationGroup> groups, Polity? focusPolity)
    {
        var maxThreat = groups.Max(group => group.Pressures.Threat.DisplayValue);
        var relatedPolityIds = groups.Select(group => group.PolityId).Distinct(StringComparer.Ordinal).ToArray();
        var hostility = focusPolity?.InterPolityRelations
            .Where(relation => relatedPolityIds.Contains(relation.OtherPolityId, StringComparer.Ordinal))
            .Select(relation => relation.Hostility)
            .DefaultIfEmpty(0)
            .Max() ?? 0;
        var score = Math.Max(maxThreat, hostility);

        return score switch
        {
            >= 60 => "high",
            >= 30 => "moderate",
            _ => "low"
        };
    }

    private static string BuildSapientKnownTraits(IReadOnlyList<PopulationGroup> groups, bool isPlayerSpecies)
    {
        if (isPlayerSpecies)
        {
            return "our people";
        }

        var dominantMode = groups
            .GroupBy(group => group.SubsistenceMode)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();
        var mobility = groups.Average(group => group.MonthsSinceLastMove);
        var mobilityText = mobility <= 2 ? "mobile" : "settled";

        return $"{FormatSubsistence(dominantMode)}, {mobilityText}";
    }

    private static IReadOnlyList<string> BuildSapientTraitLines(IReadOnlyList<PopulationGroup> groups, bool isPlayerSpecies)
    {
        var dominantMode = groups
            .GroupBy(group => group.SubsistenceMode)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();

        return
        [
            $"Subsistence pattern: {FormatSubsistence(dominantMode)}",
            groups.Average(group => group.MonthsSinceLastMove) <= 2 ? "Mobility: currently mobile" : "Mobility: relatively settled",
            isPlayerSpecies
                ? "Known traits reflect direct familiarity with your own species."
                : "Known traits are still first-pass encounter impressions."
        ];
    }

    private static string BuildSapientContactContext(IReadOnlyList<PopulationGroup> groups, Polity focusPolity)
    {
        var relatedPolityIds = groups.Select(group => group.PolityId).Distinct(StringComparer.Ordinal).ToArray();
        var relation = focusPolity.InterPolityRelations
            .Where(item => relatedPolityIds.Contains(item.OtherPolityId, StringComparer.Ordinal))
            .OrderByDescending(item => item.ContactIntensity)
            .FirstOrDefault();

        if (relation is null || relation.ContactIntensity <= 0)
        {
            return "The encounter is geographic and local rather than deeply socialized.";
        }

        return relation.Hostility >= 45
            ? "Encounter is reinforced by tense live contact."
            : relation.Cooperation >= 45
                ? "Encounter is reinforced by recurring live contact."
                : "Encounter is reinforced by ongoing contact, but deep diplomacy detail is deferred.";
    }

    private static string BuildHabitat(IReadOnlyList<Region> regions)
    {
        if (regions.Count == 0)
        {
            return "unknown";
        }

        var biome = regions
            .Select(region => FormatBiome(region.Biome))
            .Distinct(StringComparer.Ordinal)
            .Take(2);
        var water = regions
            .Select(region => FormatWater(region.WaterAvailability))
            .Distinct(StringComparer.Ordinal)
            .Take(2);
        return $"{string.Join("/", biome)}; {string.Join("/", water)} water";
    }

    private static string BuildWaterSummary(IReadOnlyList<WaterAvailability> waters)
    {
        return waters.Count == 0
            ? "unknown"
            : string.Join(", ", waters.Select(FormatWater).Distinct(StringComparer.Ordinal));
    }

    private static string BuildRegionNames(IReadOnlyList<Region> regions)
    {
        var names = regions.Select(region => region.Name).Distinct(StringComparer.Ordinal).Take(3).ToArray();
        return names.Length == 0 ? "unknown regions" : string.Join(", ", names);
    }

    private static string BuildPlayerSpeciesName(string speciesId)
    {
        return speciesId switch
        {
            "species-human" => "Human",
            _ => string.Join(' ', speciesId
                .Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries)
                .Where(part => !string.Equals(part, "species", StringComparison.OrdinalIgnoreCase))
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]))
        };
    }

    private static string FormatSubsistence(SubsistenceMode mode)
    {
        return mode switch
        {
            SubsistenceMode.Gatherer => "gathering",
            SubsistenceMode.Hunter => "hunting",
            _ => "mixed foraging"
        };
    }

    private static string FormatBiome(Biome biome)
    {
        return biome switch
        {
            Biome.Highlands => "highlands",
            Biome.Wetlands => "wetlands",
            _ => biome.ToString().ToLowerInvariant()
        };
    }

    private static string FormatWater(WaterAvailability water)
    {
        return water.ToString().ToLowerInvariant();
    }

    private static string FormatMonthYear(int month, int year)
    {
        var monthText = month switch
        {
            1 => "Jan",
            2 => "Feb",
            3 => "Mar",
            4 => "Apr",
            5 => "May",
            6 => "Jun",
            7 => "Jul",
            8 => "Aug",
            9 => "Sep",
            10 => "Oct",
            11 => "Nov",
            12 => "Dec",
            _ => "Jan"
        };

        return $"{monthText} {year:D3}";
    }
}
