using Species.Domain.Catalogs;
using Species.Domain.Enums;
using Species.Domain.Knowledge;
using Species.Domain.Models;
using Species.Client.Models;
using Species.Client.Presentation;
using Species.Client.ViewModels;

namespace Species.Client.ViewModelFactories;

public static class KnownSpeciesViewModelFactory
{
    public static int GetKnownSpeciesCount(World world, FaunaSpeciesCatalog faunaCatalog, string focalPolityId)
    {
        var focusGroup = PlayerFocus.ResolveLeadGroup(world, focalPolityId);
        if (focusGroup is null)
        {
            return 0;
        }

        var focusPolity = world.Polities.FirstOrDefault(polity => string.Equals(polity.Id, focusGroup.PolityId, StringComparison.Ordinal));
        if (focusPolity is null)
        {
            return 1;
        }

        var visibleRegionIds = focusGroup.KnownRegionIds
            .Append(focusGroup.CurrentRegionId)
            .Concat(world.PopulationGroups
                .Where(group => string.Equals(group.PolityId, focusPolity.Id, StringComparison.Ordinal))
                .Select(group => group.CurrentRegionId))
            .ToHashSet(StringComparer.Ordinal);

        var faunaCount = focusPolity.SpeciesAwareness
            .Where(state => state.SpeciesClass == SpeciesClass.Fauna && state.CurrentLevel >= KnowledgeLevel.Encounter)
            .Select(state => state.SpeciesId)
            .Distinct(StringComparer.Ordinal)
            .Count(speciesId =>
                faunaCatalog.GetById(speciesId) is not null &&
                world.Regions.Any(region =>
                    visibleRegionIds.Contains(region.Id) &&
                    region.Ecosystem.FaunaPopulations.GetValueOrDefault(speciesId) > 0));

        return 1 + faunaCount;
    }

    public static KnownSpeciesViewModel Build(World world, FaunaSpeciesCatalog faunaCatalog, string focalPolityId, int selectedIndex, bool isSimulationRunning = false)
    {
        var focusPolity = PlayerFocus.Resolve(world, focalPolityId);
        var focusGroup = PlayerFocus.ResolveLeadGroup(world, focalPolityId);
        var items = BuildSpecies(world, focusGroup, faunaCatalog);
        var clampedIndex = items.Count == 0 ? 0 : Math.Clamp(selectedIndex, 0, items.Count - 1);

        return new KnownSpeciesViewModel(
            focusPolity?.Name ?? "Unknown polity",
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            isSimulationRunning,
            items,
            items.Count == 0 ? null : items[clampedIndex],
            clampedIndex);
    }

    private static IReadOnlyList<KnownSpeciesSummary> BuildSpecies(World world, PopulationGroup? focusGroup, FaunaSpeciesCatalog faunaCatalog)
    {
        if (focusGroup is null)
        {
            return [];
        }

        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var focusPolity = world.Polities.FirstOrDefault(polity => string.Equals(polity.Id, focusGroup.PolityId, StringComparison.Ordinal));
        var summaries = new List<KnownSpeciesSummary>
        {
            BuildOwnSpeciesSummary(world, focusGroup, regionsById)
        };

        if (focusPolity is null)
        {
            return summaries;
        }

        var visibleRegionIds = focusGroup.KnownRegionIds
            .Append(focusGroup.CurrentRegionId)
            .Concat(world.PopulationGroups
                .Where(group => string.Equals(group.PolityId, focusPolity.Id, StringComparison.Ordinal))
                .Select(group => group.CurrentRegionId))
            .ToHashSet(StringComparer.Ordinal);
        var knownFaunaById = new Dictionary<string, List<Region>>(StringComparer.Ordinal);

        foreach (var awareness in focusPolity.SpeciesAwareness
                     .Where(state => state.SpeciesClass == SpeciesClass.Fauna && state.CurrentLevel >= KnowledgeLevel.Encounter)
                     .OrderBy(state => state.SpeciesId, StringComparer.Ordinal))
        {
            foreach (var region in world.Regions
                         .Where(region => visibleRegionIds.Contains(region.Id))
                         .Where(region => region.Ecosystem.FaunaPopulations.GetValueOrDefault(awareness.SpeciesId) > 0))
            {
                if (!knownFaunaById.TryGetValue(awareness.SpeciesId, out var regions))
                {
                    regions = [];
                    knownFaunaById.Add(awareness.SpeciesId, regions);
                }

                regions.Add(region);
            }
        }

        foreach (var faunaEntry in knownFaunaById.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            var fauna = faunaCatalog.GetById(faunaEntry.Key);
            if (fauna is null)
            {
                continue;
            }

            var awareness = focusPolity.SpeciesAwareness.First(state =>
                state.SpeciesClass == SpeciesClass.Fauna &&
                string.Equals(state.SpeciesId, faunaEntry.Key, StringComparison.Ordinal));
            summaries.Add(BuildFaunaSummary(fauna, faunaEntry.Value, awareness.CurrentLevel));
        }

        return summaries;
    }

    private static KnownSpeciesSummary BuildOwnSpeciesSummary(
        World world,
        PopulationGroup focusGroup,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        var speciesGroups = world.PopulationGroups
            .Where(group => string.Equals(group.SpeciesId, focusGroup.SpeciesId, StringComparison.Ordinal))
            .Where(group =>
                string.Equals(group.Id, focusGroup.Id, StringComparison.Ordinal) ||
                focusGroup.KnownRegionIds.Contains(group.CurrentRegionId) ||
                focusGroup.KnownRegionIds.Contains(group.OriginRegionId))
            .ToArray();
        var regions = speciesGroups
            .Select(group => regionsById.GetValueOrDefault(group.CurrentRegionId))
            .Where(region => region is not null)
            .Cast<Region>()
            .DistinctBy(region => region.Id)
            .ToArray();
        var avgPressure = speciesGroups.Length == 0
            ? 0
            : (int)Math.Round(speciesGroups.Average(group => Math.Max(group.Pressures.Food.DisplayValue, group.Pressures.Water.DisplayValue)), MidpointRounding.AwayFromZero);

        return new KnownSpeciesSummary(
            focusGroup.SpeciesId,
            BuildPlayerSpeciesName(focusGroup.SpeciesId),
            "Sapient species",
            true,
            $"{speciesGroups.Sum(group => group.Population):N0}",
            avgPressure >= 50 ? "familiar, pressured" : "familiar, common nearby",
            "The sapient species your polity belongs to.",
            [
                $"Known population: {speciesGroups.Sum(group => group.Population):N0}",
                $"Known range: {regions.Length} region{(regions.Length == 1 ? string.Empty : "s")}",
                $"Dominant way of living: {FormatSubsistence(speciesGroups.GroupBy(group => group.SubsistenceMode).OrderByDescending(group => group.Count()).First().Key)}"
            ],
            BuildOwnTraits(speciesGroups, regions),
            BuildOwnContext(focusGroup, regions));
    }

    private static KnownSpeciesSummary BuildFaunaSummary(FaunaSpeciesDefinition fauna, IReadOnlyList<Region> regions, KnowledgeLevel knowledgeLevel)
    {
        var totalPopulation = regions.Sum(region => region.Ecosystem.FaunaPopulations.GetValueOrDefault(fauna.Id));
        var isDangerous = fauna.DietCategory == DietCategory.Carnivore;
        var prevalence = totalPopulation switch
        {
            >= 900 => "common nearby",
            >= 350 => "present nearby",
            >= 1 => "rare",
            _ => "sparsely confirmed"
        };
        var knowledgeStatus = knowledgeLevel switch
        {
            KnowledgeLevel.Knowledge => "reliably understood",
            KnowledgeLevel.Discovery => "recognized resource",
            _ => "encountered"
        };
        var status = isDangerous ? $"{prevalence}, {knowledgeStatus}, dangerous" : $"{prevalence}, {knowledgeStatus}";
        var habitat = string.Join(", ", regions.Select(region => region.Name).Distinct(StringComparer.Ordinal).Take(3));

        var overview = isDangerous
            ? $"A known predator species encountered in {habitat}."
            : fauna.DietCategory == DietCategory.Herbivore
                ? $"A known prey and grazing species encountered in {habitat}."
                : $"A known omnivorous species encountered in {habitat}.";

        var observedBiomes = regions
            .Select(region => FormatBiome(region.Biome))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var observedWater = regions
            .Select(region => FormatWater(region.WaterAvailability))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new KnownSpeciesSummary(
            fauna.Id,
            fauna.Name,
            "Known fauna",
            false,
            ApproximatePopulation(totalPopulation),
            status,
            overview,
            [
                $"Observed habitat: {string.Join(", ", observedBiomes)}",
                $"Observed water conditions: {string.Join(", ", observedWater)}",
                $"Known range: {habitat}"
            ],
            BuildFaunaTraits(fauna),
            BuildFaunaContext(fauna, regions, totalPopulation));
    }

    private static IReadOnlyList<string> BuildOwnTraits(IReadOnlyList<PopulationGroup> groups, IReadOnlyList<Region> regions)
    {
        var traits = new List<string>
        {
            "Omnivorous subsistence inferred from mixed foraging and hunting systems."
        };

        if (groups.Average(group => group.Pressures.Migration.DisplayValue) >= 45)
        {
            traits.Add("Mobility is moderate to high when pressures rise.");
        }

        if (regions.Any(region => region.WaterAvailability == WaterAvailability.High))
        {
            traits.Add("Performs best in well-watered known regions.");
        }

        if (groups.Any(group => group.LearnedAdvancementIds.Count > 0))
        {
            traits.Add("Cultural learning is visible through active advancements.");
        }

        return traits;
    }

    private static IReadOnlyList<string> BuildOwnContext(PopulationGroup focusGroup, IReadOnlyList<Region> regions)
    {
        var context = new List<string>
        {
            "This is your own sapient species.",
            focusGroup.Pressures.Water.DisplayValue >= 40
                ? $"Water access is currently the main species-level concern for your polity ({focusGroup.Pressures.Water.SeverityLabel.ToLowerInvariant()})."
                : "No single species-level pressure dominates your polity right now."
        };

        if (regions.Count > 1)
        {
            context.Add("Known neighboring regions give this species room to adapt.");
        }

        return context;
    }

    private static IReadOnlyList<string> BuildFaunaTraits(FaunaSpeciesDefinition fauna)
    {
        var traits = new List<string>
        {
            $"Diet: {fauna.DietCategory}",
            fauna.Mobility >= 0.65f ? "Mobility: highly mobile" : fauna.Mobility >= 0.45f ? "Mobility: moderately mobile" : "Mobility: relatively settled",
            $"Yield / usefulness: {(fauna.FoodYield >= 0.60f ? "high" : fauna.FoodYield >= 0.35f ? "moderate" : "limited")}"
        };

        if (fauna.DietCategory == DietCategory.Carnivore)
        {
            traits.Add("Behavior: threatening where encountered.");
        }
        else if (fauna.DietCategory == DietCategory.Herbivore)
        {
            traits.Add("Behavior: likely useful as prey.");
        }
        else
        {
            traits.Add("Behavior: opportunistic omnivore.");
        }

        return traits;
    }

    private static IReadOnlyList<string> BuildFaunaContext(FaunaSpeciesDefinition fauna, IReadOnlyList<Region> regions, int totalPopulation)
    {
        var context = new List<string>();

        if (fauna.DietCategory == DietCategory.Carnivore)
        {
            context.Add("Matters as a local threat when your polity moves or settles nearby.");
        }
        else
        {
            context.Add("Matters as a known food or environmental species in nearby regions.");
        }

        if (totalPopulation >= 900)
        {
            context.Add("Appears widespread in the regions your polity knows.");
        }
        else if (regions.Count <= 1)
        {
            context.Add("Known from only a narrow part of the player polity's world.");
        }

        return context;
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

    private static string ApproximatePopulation(int value)
    {
        return value switch
        {
            >= 1000 => "many",
            >= 400 => "several known groups",
            >= 1 => "few known sightings",
            _ => "unknown"
        };
    }

    private static string FormatSubsistence(SubsistenceMode mode)
    {
        return mode switch
        {
            SubsistenceMode.Gatherer => "Gathering",
            SubsistenceMode.Hunter => "Hunting",
            _ => "Mixed foraging"
        };
    }

    private static string FormatBiome(Biome biome)
    {
        return biome switch
        {
            Biome.Highlands => "Highlands",
            Biome.Wetlands => "Wetlands",
            _ => biome.ToString()
        };
    }

    private static string FormatWater(WaterAvailability water)
    {
        return water.ToString();
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
