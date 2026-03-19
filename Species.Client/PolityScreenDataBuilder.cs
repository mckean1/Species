using Species.Domain.Catalogs;
using Species.Domain.Models;

public static class PolityScreenDataBuilder
{
    public static PolityScreenData Build(
        World world,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var focusGroup = SelectFocusGroup(world);
        if (focusGroup is null)
        {
            return new PolityScreenData(
                "Unknown",
                FormatMonthYear(world.CurrentMonth, world.CurrentYear),
                "Unknown",
                "Unknown",
                "Unknown",
                "Unknown",
                Array.Empty<PolityPressureItem>(),
                ["No current alerts."],
                ["No notable strengths yet."],
                ["No acute problems detected."],
                ["No notable discoveries yet."],
                ["No active laws yet."]);
        }

        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var currentRegionName = regionsById.GetValueOrDefault(focusGroup.CurrentRegionId)?.Name ?? "Unknown";
        var coreRegionName = regionsById.GetValueOrDefault(focusGroup.OriginRegionId)?.Name ?? currentRegionName;
        var pressures = BuildPressureItems(focusGroup.Pressures);

        return new PolityScreenData(
            focusGroup.Name,
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            "Unknown",
            BuildSpeciesLabel(focusGroup.SpeciesId),
            coreRegionName,
            focusGroup.Population.ToString("N0"),
            pressures,
            BuildAlerts(pressures),
            BuildStrengths(focusGroup, pressures),
            BuildProblems(focusGroup, pressures),
            BuildProgress(focusGroup, discoveryCatalog, advancementCatalog),
            ["No active laws yet."]);
    }

    private static string BuildSpeciesLabel(string speciesId)
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

    private static PopulationGroup? SelectFocusGroup(World world)
    {
        var latestEntry = world.Chronicle.GetVisibleFeedEntries().FirstOrDefault();
        if (latestEntry is not null)
        {
            var matchingGroup = world.PopulationGroups.FirstOrDefault(group =>
                string.Equals(group.Id, latestEntry.GroupId, StringComparison.Ordinal));

            if (matchingGroup is not null)
            {
                return matchingGroup;
            }
        }

        return world.PopulationGroups
            .OrderByDescending(group => group.Population)
            .ThenBy(group => group.Name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static IReadOnlyList<PolityPressureItem> BuildPressureItems(PressureState pressures)
    {
        return
        [
            new PolityPressureItem("Food", pressures.FoodPressure),
            new PolityPressureItem("Water", pressures.WaterPressure),
            new PolityPressureItem("Threat", pressures.ThreatPressure),
            new PolityPressureItem("Crowding", pressures.OvercrowdingPressure),
            new PolityPressureItem("Migration", pressures.MigrationPressure)
        ];
    }

    private static IReadOnlyList<string> BuildAlerts(IReadOnlyList<PolityPressureItem> pressures)
    {
        var alerts = pressures
            .Where(item => item.Value >= 40)
            .OrderByDescending(item => item.Value)
            .Select(item => item.Label switch
            {
                "Food" => "Food stores are under strain",
                "Water" => "Water access is tightening",
                "Threat" => "Threat pressure is rising nearby",
                "Crowding" => "Living conditions are becoming crowded",
                "Migration" => "Migration pressure is increasing",
                _ => $"{item.Label} pressure is building"
            })
            .Take(4)
            .ToArray();

        return alerts.Length > 0 ? alerts : ["No urgent current issues."];
    }

    private static IReadOnlyList<string> BuildStrengths(PopulationGroup group, IReadOnlyList<PolityPressureItem> pressures)
    {
        var strengths = new List<string>();

        if (pressures.First(item => item.Label == "Food").Value < 40)
        {
            strengths.Add("Food stores remain manageable");
        }

        if (pressures.First(item => item.Label == "Water").Value < 40)
        {
            strengths.Add("Water access is holding steady");
        }

        if (pressures.First(item => item.Label == "Threat").Value < 40)
        {
            strengths.Add("Immediate threats are limited");
        }

        if (group.KnownDiscoveryIds.Count + group.LearnedAdvancementIds.Count >= 3)
        {
            strengths.Add("Practical knowledge is accumulating");
        }

        if (group.KnownRegionIds.Count > 1)
        {
            strengths.Add("Nearby routes are known");
        }

        return strengths.Count > 0
            ? strengths.Take(3).ToArray()
            : ["No standout strengths yet."];
    }

    private static IReadOnlyList<string> BuildProblems(PopulationGroup group, IReadOnlyList<PolityPressureItem> pressures)
    {
        var problems = pressures
            .Where(item => item.Value >= 60)
            .OrderByDescending(item => item.Value)
            .Select(item => item.Label switch
            {
                "Food" => "Food stress is shaping daily life",
                "Water" => "Reliable water is becoming harder to secure",
                "Threat" => "Threat pressure is weighing on safety",
                "Crowding" => "Crowding is tightening local conditions",
                "Migration" => "Migration pressure is pulling at stability",
                _ => $"{item.Label} pressure is elevated"
            })
            .Take(3)
            .ToList();

        if (group.StoredFood <= Math.Max(1, group.Population / 2) && problems.Count < 3)
        {
            problems.Add("Stored food is thin for the current population");
        }

        return problems.Count > 0
            ? problems.ToArray()
            : ["No acute problems right now."];
    }

    private static IReadOnlyList<string> BuildProgress(
        PopulationGroup group,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var progress = new List<string>();

        progress.AddRange(group.LearnedAdvancementIds
            .OrderBy(id => id, StringComparer.Ordinal)
            .Select(id => advancementCatalog.GetById(id)?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>());

        progress.AddRange(group.KnownDiscoveryIds
            .OrderBy(id => id, StringComparer.Ordinal)
            .Select(id => discoveryCatalog.GetById(id)?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>());

        return progress.Count > 0
            ? progress.Distinct(StringComparer.Ordinal).Take(4).ToArray()
            : ["No notable discoveries yet."];
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

public sealed record PolityScreenData(
    string PolityName,
    string CurrentDate,
    string GovernmentForm,
    string Species,
    string CoreRegion,
    string Population,
    IReadOnlyList<PolityPressureItem> Pressures,
    IReadOnlyList<string> Alerts,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Problems,
    IReadOnlyList<string> ProgressItems,
    IReadOnlyList<string> ActiveLaws);

public sealed record PolityPressureItem(string Label, int Value);
