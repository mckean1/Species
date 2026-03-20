using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Domain.Simulation;

public static class PolityScreenDataBuilder
{
    private static readonly SocialTraditionCatalog SocialTraditionCatalog = new();

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
                "None",
                Array.Empty<PolityPressureItem>(),
                ["No current alerts."],
                ["No notable strengths yet."],
                ["No acute problems detected."],
                ["No governance data yet."],
                ["No scale data yet."],
                ["No outside relations yet."],
                ["No social identity data yet."],
                ["No enduring traditions yet."],
                ["No regional identity notes yet."],
                ["No material stores yet."],
                ["No notable discoveries yet."],
                ["No active laws yet."],
                ["No regional presence yet."],
                ["No political history yet."],
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
            BuildMaterialSummary(context),
            pressures,
            BuildAlerts(pressures),
            BuildStrengths(context, pressures),
            BuildProblems(context, pressures),
            BuildGovernanceNotes(context),
            BuildScaleNotes(world, focusPolity, context),
            BuildExternalNotes(world, focusPolity, context),
            BuildSocialIdentityNotes(context),
            BuildTraditions(context),
            BuildRegionalIdentityNotes(context, regionsById),
            BuildMaterialNotes(context),
            BuildProgress(context, discoveryCatalog, advancementCatalog),
            BuildActiveLaws(focusPolity),
            BuildRegionalPresence(focusPolity, regionsById),
            BuildPoliticalHistory(focusPolity),
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

        if (context.MaterialProduction.SurplusScore >= 25)
        {
            strengths.Add("Local materials are reinforcing stability");
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

        if (context.MaterialProduction.DeficitScore >= 60 && problems.Count < 3)
        {
            problems.Add("Material shortages are weakening local durability");
        }

        return problems.Count > 0
            ? problems.ToArray()
            : ["No acute problems right now."];
    }

    private static IReadOnlyList<string> BuildGovernanceNotes(PolityContext context)
    {
        return
        [
            $"Legitimacy: {context.Governance.Legitimacy} [{PolityPresentation.DescribeGovernanceBand(context.Governance.Legitimacy)}]",
            $"Cohesion: {context.Governance.Cohesion} [{PolityPresentation.DescribeGovernanceBand(context.Governance.Cohesion)}]",
            $"Authority: {context.Governance.Authority} [{PolityPresentation.DescribeGovernanceBand(context.Governance.Authority)}]",
            $"Governability: {context.Governance.Governability} [{PolityPresentation.DescribeGovernanceBand(context.Governance.Governability)}]",
            $"Peripheral strain: {context.Governance.PeripheralStrain}"
        ];
    }

    private static IReadOnlyList<string> BuildSocialIdentityNotes(PolityContext context)
    {
        return
        [
            $"Rootedness: {context.SocialIdentity.Rootedness}",
            $"Mobility: {context.SocialIdentity.Mobility}",
            $"Communalism: {context.SocialIdentity.Communalism}",
            $"Autonomy: {context.SocialIdentity.AutonomyOrientation}",
            $"Order: {context.SocialIdentity.OrderOrientation}",
            $"Frontier distinction: {context.SocialIdentity.FrontierDistinctiveness}"
        ];
    }

    private static IReadOnlyList<string> BuildScaleNotes(World world, Polity polity, PolityContext context)
    {
        var polityNamesById = world.Polities.ToDictionary(item => item.Id, item => item.Name, StringComparer.Ordinal);
        var notes = new List<string>
        {
            $"State form: {PolityPresentation.DescribePoliticalScaleForm(context.ScaleState.Form)}",
            $"Integration {context.ScaleState.IntegrationDepth} | Centralization {context.ScaleState.Centralization} | Autonomy tolerance {context.ScaleState.AutonomyTolerance}",
            $"Coordination {context.ScaleState.CoordinationStrain} | Distance {context.ScaleState.DistanceStrain} | Overreach {context.ScaleState.OverextensionPressure} | Fragmentation {context.ScaleState.FragmentationRisk}",
            context.ScaleState.Summary
        };

        if (!string.IsNullOrWhiteSpace(context.ParentPolityId))
        {
            notes.Add($"Attached to: {polityNamesById.GetValueOrDefault(context.ParentPolityId, context.ParentPolityId)}");
        }

        foreach (var attachment in polity.PoliticalAttachments.Where(attachment => attachment.IsActive).OrderByDescending(attachment => attachment.Loyalty).Take(2))
        {
            var relatedName = polityNamesById.GetValueOrDefault(attachment.RelatedPolityId, attachment.RelatedPolityId);
            notes.Add($"{relatedName}: {PolityPresentation.DescribePoliticalAttachmentKind(attachment.Kind)} [Integration {attachment.IntegrationDepth} | Loyalty {attachment.Loyalty}]");
        }

        return notes;
    }

    private static IReadOnlyList<string> BuildExternalNotes(World world, Polity polity, PolityContext context)
    {
        var polityNamesById = world.Polities.ToDictionary(item => item.Id, item => item.Name, StringComparer.Ordinal);
        var notes = new List<string>
        {
            $"Outside pressure: {context.ExternalPressure.Summary}",
            $"Threat {context.ExternalPressure.Threat} | Cooperation {context.ExternalPressure.Cooperation} | Friction {context.ExternalPressure.FrontierFriction} | Raids {context.ExternalPressure.RaidPressure}",
            $"Hostile neighbors: {context.ExternalPressure.HostileNeighborCount}"
        };

        var topRelations = polity.InterPolityRelations
            .OrderByDescending(relation => relation.Escalation + relation.Hostility + relation.ContactIntensity)
            .ThenBy(relation => relation.OtherPolityId, StringComparer.Ordinal)
            .Take(2)
            .ToArray();
        foreach (var relation in topRelations)
        {
            var otherName = polityNamesById.GetValueOrDefault(relation.OtherPolityId, relation.OtherPolityId);
            notes.Add($"{otherName}: {relation.Stance} [{relation.RecentSummary}]");
        }

        return notes;
    }

    private static IReadOnlyList<string> BuildTraditions(PolityContext context)
    {
        var traditions = context.SocialIdentity.TraditionIds
            .Select(SocialTraditionCatalog.GetById)
            .Where(definition => definition is not null)
            .Select(definition => $"{definition!.Name}: {definition.Summary}")
            .ToArray();

        return traditions.Length > 0 ? traditions : ["No enduring traditions yet."];
    }

    private static IReadOnlyList<string> BuildRegionalIdentityNotes(PolityContext context, IReadOnlyDictionary<string, Region> regionsById)
    {
        var notes = new List<string>();
        var coreRegion = regionsById.GetValueOrDefault(context.CoreRegionId);

        if (context.SocialIdentity.Rootedness >= 60)
        {
            notes.Add(coreRegion?.WaterAvailability == Species.Domain.Enums.WaterAvailability.High
                ? "The river heartland is becoming a rooted social core."
                : "The core region is becoming a rooted social heartland.");
        }

        if (context.SocialIdentity.FrontierDistinctiveness >= 55)
        {
            notes.Add("Peripheral regions are developing stronger distinct expectations.");
        }

        if (context.SocialIdentity.Mobility >= 55)
        {
            notes.Add("Movement and return cycles remain part of the polity's social character.");
        }

        return notes.Count > 0 ? notes : ["No strong regional distinctions yet."];
    }

    private static string BuildMaterialSummary(PolityContext context)
    {
        var stores = context.TotalMaterialStores;
        if (stores.Total <= 0)
        {
            return "None";
        }

        return $"Timber {stores.Timber} | Stone {stores.Stone} | Fiber {stores.Fiber} | Clay {stores.Clay} | Hides {stores.Hides}";
    }

    private static IReadOnlyList<string> BuildMaterialNotes(PolityContext context)
    {
        var notes = new List<string>
        {
            $"Condition: {context.MaterialProduction.ConditionSummary}",
            $"Support [Shelter {context.MaterialProduction.ShelterSupport} | Storage {context.MaterialProduction.StorageSupport} | Tools {context.MaterialProduction.ToolSupport} | Goods {context.MaterialProduction.TextileSupport}]"
        };

        if (context.MaterialSurplusMonths > 0)
        {
            notes.Add($"Material surplus sustained for {context.MaterialSurplusMonths} month(s)");
        }

        if (context.MaterialShortageMonths > 0)
        {
            notes.Add($"Material shortages sustained for {context.MaterialShortageMonths} month(s)");
        }

        return notes;
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

    private static IReadOnlyList<string> BuildPoliticalHistory(Polity polity)
    {
        var history = polity.PoliticalHistory
            .OrderByDescending(record => record.Year)
            .ThenByDescending(record => record.Month)
            .Select(record => record.Summary)
            .Take(4)
            .ToArray();

        return history.Length > 0 ? history : ["No political history yet."];
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
    string MaterialStores,
    IReadOnlyList<PolityPressureItem> Pressures,
    IReadOnlyList<string> Alerts,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Problems,
    IReadOnlyList<string> GovernanceNotes,
    IReadOnlyList<string> ScaleNotes,
    IReadOnlyList<string> ExternalNotes,
    IReadOnlyList<string> SocialIdentityNotes,
    IReadOnlyList<string> Traditions,
    IReadOnlyList<string> RegionalIdentityNotes,
    IReadOnlyList<string> MaterialNotes,
    IReadOnlyList<string> ProgressItems,
    IReadOnlyList<string> ActiveLaws,
    IReadOnlyList<string> RegionalPresence,
    IReadOnlyList<string> PoliticalHistory,
    IReadOnlyList<PoliticalBlocScreenItem> PoliticalBlocs);

public sealed record PolityPressureItem(string Label, int Value);

public sealed record PoliticalBlocScreenItem(string Name, int Influence, int Satisfaction);
