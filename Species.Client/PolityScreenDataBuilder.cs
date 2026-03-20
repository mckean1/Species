using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Domain.Simulation;

public static class PolityScreenDataBuilder
{
    public static PolityScreenData Build(
        World world,
        string focalPolityId,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var focusPolity = PlayerFocus.Resolve(world, focalPolityId);
        var context = PlayerFocus.ResolveContext(world, focalPolityId);
        if (focusPolity is null || context?.LeadGroup is null)
        {
            return new PolityScreenData(
                "Unknown",
                FormatMonthYear(world.CurrentMonth, world.CurrentYear),
                "Unknown",
                "Unknown",
                "Unknown",
                "Unknown",
                "Unknown",
                "Unknown",
                "None",
                Array.Empty<PolityPressureItem>(),
                ["No current alerts."],
                ["No notable strengths yet."],
                ["No acute problems detected."],
                ["No notable discoveries yet."],
                ["No active laws yet."],
                ["No regional presence yet."],
                Array.Empty<PoliticalBlocScreenItem>());
        }

        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var currentRegionName = regionsById.GetValueOrDefault(context.CurrentRegionId)?.Name ?? "Unknown";
        var homeRegionName = regionsById.GetValueOrDefault(context.HomeRegionId)?.Name ?? currentRegionName;
        var coreRegionName = regionsById.GetValueOrDefault(context.CoreRegionId)?.Name ?? homeRegionName;
        var pressures = BuildPressureItems(context.Pressures);
        var primarySite = context.PrimarySettlement is null
            ? "None"
            : $"{context.PrimarySettlement.Name} ({PolityPresentation.DescribeSettlementType(context.PrimarySettlement.Type)})";

        return new PolityScreenData(
            focusPolity.Name,
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            PolityPresentation.DescribeGovernmentForm(focusPolity.GovernmentForm),
            BuildSpeciesLabel(context.SpeciesId),
            PolityPresentation.DescribeAnchoringKind(context.AnchoringKind),
            homeRegionName,
            coreRegionName,
            primarySite,
            context.TotalPopulation.ToString("N0"),
            pressures,
            BuildAlerts(pressures),
            BuildStrengths(context, pressures),
            BuildProblems(context, pressures),
            BuildProgress(context, discoveryCatalog, advancementCatalog),
            BuildActiveLaws(focusPolity),
            BuildRegionalPresence(focusPolity, regionsById),
            BuildPoliticalBlocs(focusPolity));
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

    private static IReadOnlyList<PolityPressureItem> BuildPressureItems(PressureState pressures)
    {
        return
        [
            new PolityPressureItem("Food Stores", pressures.FoodPressure),
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
                "Food Stores" => "Food stores are under strain",
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

    private static IReadOnlyList<string> BuildStrengths(PolityContext context, IReadOnlyList<PolityPressureItem> pressures)
    {
        var strengths = new List<string>();

        if (pressures.First(item => item.Label == "Food Stores").Value < 40)
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

        if (context.KnownDiscoveryIds.Count + context.LearnedAdvancementIds.Count >= 3)
        {
            strengths.Add("Practical knowledge is accumulating");
        }

        if (context.KnownRegionIds.Count > 1)
        {
            strengths.Add("Nearby routes are known");
        }

        return strengths.Count > 0
            ? strengths.Take(3).ToArray()
            : ["No standout strengths yet."];
    }

    private static IReadOnlyList<string> BuildProblems(PolityContext context, IReadOnlyList<PolityPressureItem> pressures)
    {
        var problems = pressures
            .Where(item => item.Value >= 60)
            .OrderByDescending(item => item.Value)
            .Select(item => item.Label switch
            {
                "Food Stores" => "Food stress is shaping daily life",
                "Water" => "Reliable water is becoming harder to secure",
                "Threat" => "Threat pressure is weighing on safety",
                "Crowding" => "Crowding is tightening local conditions",
                "Migration" => "Migration pressure is pulling at stability",
                _ => $"{item.Label} pressure is elevated"
            })
            .Take(3)
            .ToList();

        if (context.TotalStoredFood <= Math.Max(1, context.TotalPopulation / 2) && problems.Count < 3)
        {
            problems.Add("Stored food is thin for the current population");
        }

        return problems.Count > 0
            ? problems.ToArray()
            : ["No acute problems right now."];
    }

    private static IReadOnlyList<string> BuildProgress(
        PolityContext context,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var progress = new List<string>();

        progress.AddRange(context.LearnedAdvancementIds
            .OrderBy(id => id, StringComparer.Ordinal)
            .Select(id => advancementCatalog.GetById(id)?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>());

        progress.AddRange(context.KnownDiscoveryIds
            .OrderBy(id => id, StringComparer.Ordinal)
            .Select(id => discoveryCatalog.GetById(id)?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>());

        return progress.Count > 0
            ? progress.Distinct(StringComparer.Ordinal).Take(4).ToArray()
            : ["No notable discoveries yet."];
    }

    private static IReadOnlyList<string> BuildActiveLaws(Polity polity)
    {
        var enacted = polity.EnactedLaws
            .Where(law => law.IsActive)
            .OrderByDescending(law => law.EnactedOnYear)
            .ThenByDescending(law => law.EnactedOnMonth)
            .ThenBy(law => law.Title, StringComparer.Ordinal)
            .Select(law => $"{law.Title} [{PolityPresentation.DescribeLawCategory(law.Category)} | E {PolityPresentation.DescribeLawStrengthBand(law.EnforcementStrength)} | C {PolityPresentation.DescribeLawStrengthBand(law.ComplianceLevel)}]")
            .ToArray();

        return enacted.Length > 0 ? enacted : ["No enacted laws yet."];
    }

    private static IReadOnlyList<string> BuildRegionalPresence(Polity polity, IReadOnlyDictionary<string, Region> regionsById)
    {
        var activePresence = polity.RegionalPresences
            .Where(presence => presence.IsCurrent || presence.MonthsSinceLastPresence <= 6)
            .OrderByDescending(presence => presence.Kind)
            .ThenBy(presence => presence.MonthsSinceLastPresence)
            .ThenBy(presence => presence.RegionId, StringComparer.Ordinal)
            .Take(4)
            .Select(presence =>
            {
                var regionName = regionsById.TryGetValue(presence.RegionId, out var region) ? region.Name : presence.RegionId;
                var recency = presence.IsCurrent ? "current" : $"{presence.MonthsSinceLastPresence}m ago";
                return $"{regionName} [{PolityPresentation.DescribePresenceKind(presence.Kind)} | {recency}]";
            })
            .ToArray();

        return activePresence.Length > 0 ? activePresence : ["No notable regional footholds yet."];
    }

    private static IReadOnlyList<PoliticalBlocScreenItem> BuildPoliticalBlocs(Polity polity)
    {
        return polity.PoliticalBlocs
            .OrderByDescending(bloc => bloc.Influence)
            .ThenByDescending(bloc => bloc.Satisfaction)
            .ThenBy(bloc => PolityPresentation.DescribeBackingSource(bloc.Source), StringComparer.Ordinal)
            .Take(6)
            .Select(bloc => new PoliticalBlocScreenItem(
                PolityPresentation.DescribeBackingSource(bloc.Source),
                bloc.Influence,
                bloc.Satisfaction))
            .ToArray();
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
    string Anchoring,
    string HomeRegion,
    string CoreRegion,
    string PrimarySite,
    string Population,
    IReadOnlyList<PolityPressureItem> Pressures,
    IReadOnlyList<string> Alerts,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Problems,
    IReadOnlyList<string> ProgressItems,
    IReadOnlyList<string> ActiveLaws,
    IReadOnlyList<string> RegionalPresence,
    IReadOnlyList<PoliticalBlocScreenItem> PoliticalBlocs);

public sealed record PolityPressureItem(string Label, int Value);

public sealed record PoliticalBlocScreenItem(string Name, int Influence, int Satisfaction);
