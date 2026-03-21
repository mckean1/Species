using Species.Domain.Enums;
using Species.Domain.Models;
using Species.Domain.Simulation;
using Species.Client.Presentation;

namespace Species.Client.DataBuilders;

public static class GovernmentScreenDataBuilder
{
    private static readonly PolityConditionEvaluator PolityConditionEvaluator = new();

    public static GovernmentScreenData Build(World world, string focalPolityId)
    {
        var focusPolity = PlayerFocus.Resolve(world, focalPolityId);
        var context = PlayerFocus.ResolveContext(world, focalPolityId);
        if (focusPolity is null || context?.LeadGroup is null)
        {
            return new GovernmentScreenData(
                "Unknown polity",
                FormatMonthYear(world.CurrentMonth, world.CurrentYear),
                "Unknown",
                "Not Established",
                "Not Recorded");
        }

        var snapshot = PolityConditionEvaluator.Evaluate(world, focusPolity);
        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);

        return new GovernmentScreenData(
            focusPolity.Name,
            FormatMonthYear(world.CurrentMonth, world.CurrentYear),
            PolityPresentation.DescribeGovernmentForm(snapshot.GovernmentForm),
            ResolveCapital(focusPolity, context, regionsById),
            ResolveFounded(focusPolity));
    }

    private static string ResolveCapital(
        Polity polity,
        PolityContext context,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        var activeSettlements = polity.Settlements
            .Where(settlement =>
                settlement.IsActive &&
                string.Equals(settlement.PolityId, polity.Id, StringComparison.Ordinal))
            .Where(settlement => ResolveSettlementPopulation(context, settlement) > 0)
            .ToArray();

        if (activeSettlements.Length == 0 || polity.AnchoringKind == PolityAnchoringKind.Mobile)
        {
            return "Not Established";
        }

        var explicitCapital = activeSettlements.FirstOrDefault(settlement =>
                                  settlement.IsPrimary &&
                                  string.Equals(settlement.Id, polity.PrimarySettlementId, StringComparison.Ordinal))
                              ?? activeSettlements.FirstOrDefault(settlement =>
                                  string.Equals(settlement.Id, polity.PrimarySettlementId, StringComparison.Ordinal))
                              ?? activeSettlements.FirstOrDefault(settlement => settlement.IsPrimary);
        if (explicitCapital is not null)
        {
            return FormatCapital(explicitCapital, regionsById);
        }

        var stableSettlements = activeSettlements
            .Where(settlement => settlement.Type == SettlementType.Village)
            .ToArray();
        if (stableSettlements.Length == 0)
        {
            return "Not Established";
        }

        var foundingSettlement = stableSettlements
            .Where(settlement => string.Equals(settlement.RegionId, polity.HomeRegionId, StringComparison.Ordinal))
            .OrderBy(settlement => settlement.FoundedYear)
            .ThenBy(settlement => settlement.FoundedMonth)
            .ThenBy(settlement => settlement.Name, StringComparer.Ordinal)
            .ThenBy(settlement => settlement.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        if (foundingSettlement is not null)
        {
            return FormatCapital(foundingSettlement, regionsById);
        }

        var largestSettlement = stableSettlements
            .OrderByDescending(settlement => ResolveSettlementPopulation(context, settlement))
            .ThenBy(settlement => settlement.FoundedYear)
            .ThenBy(settlement => settlement.FoundedMonth)
            .ThenBy(settlement => settlement.Name, StringComparer.Ordinal)
            .ThenBy(settlement => settlement.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        if (largestSettlement is not null && ResolveSettlementPopulation(context, largestSettlement) > 0)
        {
            return FormatCapital(largestSettlement, regionsById);
        }

        var oldestContinuousSettlement = stableSettlements
            .OrderBy(settlement => settlement.FoundedYear)
            .ThenBy(settlement => settlement.FoundedMonth)
            .ThenBy(settlement => settlement.Name, StringComparer.Ordinal)
            .ThenBy(settlement => settlement.Id, StringComparer.Ordinal)
            .First();
        return FormatCapital(oldestContinuousSettlement, regionsById);
    }

    private static int ResolveSettlementPopulation(PolityContext context, Settlement settlement)
    {
        return context.MemberGroups
            .Where(group => string.Equals(group.CurrentRegionId, settlement.RegionId, StringComparison.Ordinal))
            .Sum(group => group.Population);
    }

    private static string FormatCapital(Settlement settlement, IReadOnlyDictionary<string, Region> regionsById)
    {
        var regionName = regionsById.GetValueOrDefault(settlement.RegionId)?.Name;
        return string.IsNullOrWhiteSpace(regionName)
            ? settlement.Name
            : $"{settlement.Name} ({regionName})";
    }

    private static string ResolveFounded(Polity polity)
    {
        var foundedRecord = polity.PoliticalHistory
            .Where(record => record.Kind is PoliticalHistoryKind.Consolidation or PoliticalHistoryKind.Breakaway or PoliticalHistoryKind.Successor or PoliticalHistoryKind.Independence)
            .OrderBy(record => record.Year)
            .ThenBy(record => record.Month)
            .FirstOrDefault();

        return foundedRecord is null
            ? "Not Recorded"
            : FormatMonthYear(foundedRecord.Month, foundedRecord.Year);
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

public sealed record GovernmentScreenData(
    string PolityName,
    string CurrentDate,
    string GovernmentForm,
    string Capital,
    string Founded);
