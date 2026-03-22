using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class ChronicleSystem
{
    private static readonly ChroniclePolicy Policy = new();

    public ChronicleUpdateResult Run(
        World world,
        IReadOnlyList<GroupPressureChange> pressureChanges,
        IReadOnlyList<GroupSurvivalChange> survivalChanges,
        IReadOnlyList<MigrationChange> migrationChanges,
        IReadOnlyList<BiologicalHistoryChange> biologicalHistoryChanges,
        IReadOnlyList<DiscoveryChange> discoveryChanges,
        IReadOnlyList<AdvancementChange> advancementChanges,
        IReadOnlyList<SocialIdentityChange> socialIdentityChanges,
        IReadOnlyList<InterPolityChange> interPolityChanges,
        IReadOnlyList<PoliticalScaleChange> politicalScaleChanges,
        IReadOnlyList<LawProposalChange> lawProposalChanges,
        IReadOnlyList<SettlementChange> settlementChanges,
        IReadOnlyList<MaterialEconomyChange> materialEconomyChanges)
    {
        var candidates = BuildCandidates(
            world,
            pressureChanges,
            survivalChanges,
            migrationChanges,
            biologicalHistoryChanges,
            discoveryChanges,
            advancementChanges,
            socialIdentityChanges,
            interPolityChanges,
            politicalScaleChanges,
            lawProposalChanges,
            settlementChanges,
            materialEconomyChanges).ToList();
        foreach (var alertCandidate in BuildAlertCandidates(world))
        {
            candidates.Add(alertCandidate);
        }
        var policyResult = Policy.Evaluate(world, candidates);
        var updatedChronicle = Policy.Apply(world, policyResult);
        var revealedEntries = RevealEntries(updatedChronicle, world.CurrentYear, world.CurrentMonth, out var revealedChronicle);
        return new ChronicleUpdateResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, world.PopulationGroups, revealedChronicle, world.Polities, world.FocalPolityId),
            policyResult.ChronicleEntries,
            revealedEntries,
            policyResult.Alerts);
    }

    private static IReadOnlyList<ChronicleEventCandidate> BuildCandidates(
        World world,
        IReadOnlyList<GroupPressureChange> pressureChanges,
        IReadOnlyList<GroupSurvivalChange> survivalChanges,
        IReadOnlyList<MigrationChange> migrationChanges,
        IReadOnlyList<BiologicalHistoryChange> biologicalHistoryChanges,
        IReadOnlyList<DiscoveryChange> discoveryChanges,
        IReadOnlyList<AdvancementChange> advancementChanges,
        IReadOnlyList<SocialIdentityChange> socialIdentityChanges,
        IReadOnlyList<InterPolityChange> interPolityChanges,
        IReadOnlyList<PoliticalScaleChange> politicalScaleChanges,
        IReadOnlyList<LawProposalChange> lawProposalChanges,
        IReadOnlyList<SettlementChange> settlementChanges,
        IReadOnlyList<MaterialEconomyChange> materialEconomyChanges)
    {
        var candidates = new List<ChronicleEventCandidate>();
        var pressureChangesByGroupId = pressureChanges.ToDictionary(change => change.GroupId, StringComparer.Ordinal);

        foreach (var change in migrationChanges.Where(change => change.Moved))
        {
            candidates.Add(CreateCandidate(
                world,
                change.GroupId,
                change.GroupName,
                "migration",
                ChronicleCandidateCategory.Territory,
                ChronicleEventSeverity.Notable,
                ChronicleTriggerKind.Migrated,
                $"{change.GroupId}:migration:{change.NewRegionId}",
                "migration",
                regionId: change.NewRegionId,
                regionName: change.NewRegionName));
        }

        foreach (var change in survivalChanges)
        {
            pressureChangesByGroupId.TryGetValue(change.GroupId, out var pressureChange);

            if (ShouldRecordHardshipEntry(change, pressureChange))
            {
                candidates.Add(CreateCandidate(
                    world,
                    change.GroupId,
                    change.GroupName,
                    change.StarvationLoss > 0 ? "famine" : "shortage",
                    ChronicleCandidateCategory.Survival,
                    change.StarvationLoss > 0 ? ChronicleEventSeverity.Critical : ChronicleEventSeverity.Major,
                    change.StarvationLoss > 0 ? ChronicleTriggerKind.Escalated : ChronicleTriggerKind.Started,
                    $"{change.GroupId}:shortage:{ResolveTopPressureKey(pressureChange)}:{change.CurrentRegionId}",
                    "shortage",
                    regionId: change.CurrentRegionId));
            }

            if (-change.NetPopulationChange >= ChronicleConstants.MeaningfulDeclineThreshold && change.FinalPopulation > 0)
            {
                candidates.Add(CreateCandidate(
                    world,
                    change.GroupId,
                    change.GroupName,
                    "decline",
                    ChronicleCandidateCategory.Demography,
                    ChronicleEventSeverity.Major,
                    ChronicleTriggerKind.BecameUnstable,
                    $"{change.GroupId}:decline:{world.CurrentYear}:{world.CurrentMonth}",
                    "decline",
                    regionId: change.CurrentRegionId));
            }

            if (change.FinalPopulation == 0)
            {
                candidates.Add(CreateCandidate(
                    world,
                    change.GroupId,
                    change.GroupName,
                    "losses",
                    ChronicleCandidateCategory.Demography,
                    ChronicleEventSeverity.Critical,
                    ChronicleTriggerKind.Collapsed,
                    $"{change.GroupId}:extinction",
                    "extinction",
                    regionId: change.CurrentRegionId));
            }
        }

        foreach (var change in discoveryChanges.Where(change => change.UnlockedEntries.Count > 0))
        {
            foreach (var unlocked in change.UnlockedEntries)
            {
                candidates.Add(CreateCandidate(
                    world,
                    change.GroupId,
                    change.GroupName,
                    "discovery",
                    ChronicleCandidateCategory.Discovery,
                    ChronicleEventSeverity.Notable,
                    unlocked.IsEncounter ? ChronicleTriggerKind.Encountered : ChronicleTriggerKind.Discovered,
                    $"{change.GroupId}:discovery:{Normalize(unlocked.Name)}:{(unlocked.IsEncounter ? "encounter" : "discover")}:{Normalize(unlocked.RegionName ?? string.Empty)}:{unlocked.IsScoutSourced}",
                    "discovery",
                    regionName: unlocked.RegionName,
                    discoveryName: unlocked.IsEncounter ? null : unlocked.Name,
                    otherPartyName: unlocked.IsEncounter ? unlocked.Name : null,
                    isScoutSourced: unlocked.IsScoutSourced));
            }
        }

        foreach (var change in advancementChanges.Where(change => change.UnlockedAdvancementNames.Count > 0))
        {
            foreach (var advancementName in change.UnlockedAdvancementNames)
            {
                candidates.Add(CreateCandidate(
                    world,
                    change.GroupId,
                    change.GroupName,
                    "advancement",
                    ChronicleCandidateCategory.Advancement,
                    ChronicleEventSeverity.Major,
                    ChronicleTriggerKind.Learned,
                    $"{change.GroupId}:advancement:{Normalize(advancementName)}",
                    "advancement",
                    advancementName: advancementName));
            }
        }

        foreach (var change in interPolityChanges)
        {
            candidates.Add(CreateCandidate(
                world,
                change.PrimaryPolityId,
                change.PrimaryPolityName,
                change.EventType,
                ChronicleCandidateCategory.Conflict,
                ResolveExternalSeverity(change.EventType),
                ResolveConflictTrigger(change.EventType),
                $"{change.PrimaryPolityId}:external:{change.Kind}:{Normalize(change.EventType)}:{change.OtherPolityId}",
                $"external:{change.Kind}",
                otherPartyName: change.OtherPolityName));
        }

        foreach (var change in politicalScaleChanges)
        {
            candidates.Add(CreateCandidate(
                world,
                change.PolityId,
                change.PolityName,
                "political",
                ChronicleCandidateCategory.Polity,
                ResolvePoliticalSeverity(change.TriggerKind),
                change.TriggerKind,
                $"{change.PolityId}:political:{change.Kind}:{Normalize(change.GovernmentFormName)}:{change.OtherPolityId}",
                $"political:{change.Kind}",
                governmentFormName: string.IsNullOrWhiteSpace(change.GovernmentFormName) ? null : change.GovernmentFormName));
        }

        foreach (var change in lawProposalChanges)
        {
            candidates.Add(CreateCandidate(
                world,
                change.GroupId,
                change.GroupName,
                "law",
                ChronicleCandidateCategory.Politics,
                ChronicleEventSeverity.Notable,
                change.Status == Species.Domain.Enums.LawProposalStatus.Passed ? ChronicleTriggerKind.Secured : ChronicleTriggerKind.Lost,
                $"{change.GroupId}:law:{Normalize(change.ProposalTitle)}:{change.Status}",
                "law",
                lawName: change.ProposalTitle));
        }

        foreach (var change in settlementChanges)
        {
            if (change.Kind is not (SettlementChangeKind.Founded or SettlementChangeKind.Abandoned))
            {
                continue;
            }

            candidates.Add(CreateCandidate(
                world,
                change.PolityId,
                change.PolityName,
                "settlement",
                ChronicleCandidateCategory.Settlement,
                ResolveSettlementSeverity(change.Kind),
                ResolveSettlementTrigger(change.Kind),
                $"{change.PolityId}:settlement:{change.RegionId}:{change.Kind}:{Normalize(change.SettlementName)}",
                "settlement",
                regionId: change.RegionId,
                regionName: change.RegionName,
                settlementName: change.SettlementName));
        }

        return candidates;
    }

    private static IReadOnlyList<ChronicleEventCandidate> BuildAlertCandidates(World world)
    {
        var candidates = new List<ChronicleEventCandidate>();

        foreach (var polity in world.Polities.OrderBy(polity => polity.Id, StringComparer.Ordinal))
        {
            var context = PolityData.BuildContext(world, polity);
            if (context is null)
            {
                continue;
            }

            var monthlyDemand = Math.Max(1, context.FoodAccounting.MonthlyDemand);
            if (context.FoodAccounting.EndingTotalStores <= monthlyDemand ||
                context.Pressures.Food.DisplayValue >= ChronicleConstants.HardshipWarningDisplayThreshold)
            {
                candidates.Add(CreateCandidate(
                    world,
                    polity.Id,
                    polity.Name,
                    "low-food-stores",
                    ChronicleCandidateCategory.Survival,
                    ChronicleEventSeverity.Major,
                    ChronicleTriggerKind.Started,
                    $"{polity.Id}:active:food-stores",
                    "active-food-stores",
                    preferredOutputTarget: ChronicleOutputTarget.Alert));
            }

            if (context.FoodAccounting.ShortageMonths > 0)
            {
                candidates.Add(CreateCandidate(
                    world,
                    polity.Id,
                    polity.Name,
                    "shortage-active",
                    ChronicleCandidateCategory.Survival,
                    ChronicleEventSeverity.Major,
                    ChronicleTriggerKind.Started,
                    $"{polity.Id}:active:shortage",
                    "active-shortage",
                    preferredOutputTarget: ChronicleOutputTarget.Alert));
            }

            if (context.FoodAccounting.UnresolvedDeficit > 0 ||
                context.FoodAccounting.FoodStressState == FoodStressState.Starvation)
            {
                candidates.Add(CreateCandidate(
                    world,
                    polity.Id,
                    polity.Name,
                    "famine-active",
                    ChronicleCandidateCategory.Survival,
                    ChronicleEventSeverity.Critical,
                    ChronicleTriggerKind.Escalated,
                    $"{polity.Id}:active:famine",
                    "active-famine",
                    preferredOutputTarget: ChronicleOutputTarget.Alert));
            }

            if (context.Pressures.Migration.DisplayValue >= ChronicleConstants.PressureConcernDisplayThreshold)
            {
                candidates.Add(CreateCandidate(
                    world,
                    polity.Id,
                    polity.Name,
                    "migration-pressure",
                    ChronicleCandidateCategory.Territory,
                    ChronicleEventSeverity.Notable,
                    ChronicleTriggerKind.Escalated,
                    $"{polity.Id}:active:migration",
                    "active-migration",
                    preferredOutputTarget: ChronicleOutputTarget.Alert));
            }

            if (context.ExternalPressure.RaidPressure >= 55 || context.ExternalPressure.Threat >= 70)
            {
                candidates.Add(CreateCandidate(
                    world,
                    polity.Id,
                    polity.Name,
                    "conflict-active",
                    ChronicleCandidateCategory.Conflict,
                    ChronicleEventSeverity.Major,
                    ChronicleTriggerKind.Escalated,
                    $"{polity.Id}:active:conflict",
                    "active-conflict",
                    preferredOutputTarget: ChronicleOutputTarget.Alert));
            }

            var primarySettlement = context.PrimarySettlement;
            if (primarySettlement is not null &&
                (primarySettlement.MaterialSupport < 40 || primarySettlement.StoredFood <= 0))
            {
                candidates.Add(CreateCandidate(
                    world,
                    polity.Id,
                    polity.Name,
                    "settlement-vulnerable",
                    ChronicleCandidateCategory.Settlement,
                    ChronicleEventSeverity.Notable,
                    ChronicleTriggerKind.BecameUnstable,
                    $"{polity.Id}:active:settlement:{primarySettlement.Id}",
                    "active-settlement-vulnerable",
                    preferredOutputTarget: ChronicleOutputTarget.Alert,
                    settlementName: primarySettlement.Name,
                    regionId: primarySettlement.RegionId));
            }
        }

        return candidates;
    }

    public World RecordLawDecision(World world, LawProposalChange change)
    {
        var candidate = CreateCandidate(
            world,
            change.GroupId,
            change.GroupName,
            "law",
            ChronicleCandidateCategory.Politics,
            ChronicleEventSeverity.Notable,
            change.Status == Species.Domain.Enums.LawProposalStatus.Passed ? ChronicleTriggerKind.Secured : ChronicleTriggerKind.Lost,
            $"{change.GroupId}:law:{Normalize(change.ProposalTitle)}:{change.Status}",
            "law",
            lawName: change.ProposalTitle);
        var policyResult = Policy.Evaluate(world, [candidate]);
        var chronicle = Policy.Apply(world, policyResult);
        return new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, world.PopulationGroups, chronicle, world.Polities, world.FocalPolityId);
    }

    private static ChronicleEventCandidate CreateCandidate(
        World world,
        string polityId,
        string polityName,
        string eventType,
        ChronicleCandidateCategory category,
        ChronicleEventSeverity severity,
        ChronicleTriggerKind triggerKind,
        string dedupeKey,
        string cooldownFamily,
        ChronicleOutputTarget preferredOutputTarget = ChronicleOutputTarget.Chronicle,
        string? regionId = null,
        string? regionName = null,
        string? settlementName = null,
        string? discoveryName = null,
        string? advancementName = null,
        string? lawName = null,
        string? otherPartyName = null,
        string? governmentFormName = null,
        bool isScoutSourced = false)
    {
        return new ChronicleEventCandidate
        {
            EventYear = world.CurrentYear,
            EventMonth = world.CurrentMonth,
            PolityId = polityId,
            PolityName = polityName,
            EventType = eventType,
            Category = category,
            Severity = severity,
            TriggerKind = triggerKind,
            RegionId = regionId,
            RegionName = regionName,
            SettlementName = settlementName,
            DiscoveryName = discoveryName,
            AdvancementName = advancementName,
            LawName = lawName,
            OtherPartyName = otherPartyName,
            GovernmentFormName = governmentFormName,
            IsScoutSourced = isScoutSourced,
            DedupeKey = dedupeKey,
            CooldownFamily = cooldownFamily,
            PreferredOutputTarget = preferredOutputTarget,
            Tags = [eventType]
        };
    }

    private static IReadOnlyList<ChronicleEntry> RevealEntries(Chronicle chronicle, int currentYear, int currentMonth, out Chronicle updatedChronicle)
    {
        var revealableEntries = chronicle.GetPendingEntries()
            .Where(entry => IsDue(entry, currentYear, currentMonth))
            .Take(ChronicleConstants.MaxRevealPerTick)
            .ToArray();

        if (revealableEntries.Length == 0)
        {
            updatedChronicle = chronicle;
            return Array.Empty<ChronicleEntry>();
        }

        var revealedEntriesBySequence = new Dictionary<int, ChronicleEntry>();
        var nextRevealSequence = chronicle.NextRevealSequence;
        foreach (var entry in revealableEntries)
        {
            revealedEntriesBySequence[entry.RecordSequence] = entry.Reveal(currentYear, currentMonth, nextRevealSequence++);
        }

        var mergedEntries = chronicle.Entries
            .Select(entry => revealedEntriesBySequence.TryGetValue(entry.RecordSequence, out var revealedEntry) ? revealedEntry : entry)
            .OrderBy(entry => entry.RecordSequence)
            .ToArray();

        updatedChronicle = new Chronicle(
            mergedEntries,
            chronicle.Alerts,
            chronicle.EventMemory,
            chronicle.NextRecordSequence,
            nextRevealSequence,
            chronicle.NextAlertSequence);
        return revealedEntriesBySequence.Values
            .OrderByDescending(entry => entry.RevealSequence)
            .ToArray();
    }

    private static bool IsDue(ChronicleEntry entry, int currentYear, int currentMonth)
    {
        if (entry.RevealAfterYear < currentYear)
        {
            return true;
        }

        return entry.RevealAfterYear == currentYear && entry.RevealAfterMonth <= currentMonth;
    }

    private static bool ShouldRecordHardshipEntry(GroupSurvivalChange survivalChange, GroupPressureChange? pressureChange)
    {
        if (survivalChange.StarvationLoss > 0)
        {
            return true;
        }

        if (survivalChange.Shortage < ChronicleConstants.MeaningfulShortageThreshold)
        {
            return false;
        }

        if (survivalChange.Shortage >= ChronicleConstants.MeaningfulShortageThreshold * 2)
        {
            return true;
        }

        if (pressureChange is null)
        {
            return true;
        }

        var previousTop = ResolveTopPressure(pressureChange, useCurrent: false);
        var currentTop = ResolveTopPressure(pressureChange, useCurrent: true);
        if (!string.Equals(previousTop.Category, currentTop.Category, StringComparison.Ordinal) &&
            currentTop.Display >= ChronicleConstants.PressureTransitionDisplayThreshold)
        {
            return true;
        }

        if (!string.Equals(previousTop.SeverityLabel, currentTop.SeverityLabel, StringComparison.Ordinal))
        {
            return true;
        }

        return HasMeaningfulPressureWorsening(pressureChange.Food, PressureDefinitions.Food) ||
               HasMeaningfulPressureWorsening(pressureChange.Water, PressureDefinitions.Water) ||
               HasMeaningfulPressureWorsening(pressureChange.Migration, PressureDefinitions.Migration);
    }

    private static bool HasMeaningfulPressureWorsening(PressureChangeDetail detail, PressureDefinition definition)
    {
        var previousDisplay = PressureMath.ComputeDisplayValue(PressureMath.ComputeEffectiveValue(definition, detail.PriorRaw));
        var previousSeverity = PressureMath.ComputeSeverityLabel(previousDisplay);
        return detail.Display >= ChronicleConstants.PressureTransitionDisplayThreshold &&
               (detail.Display - previousDisplay >= ChronicleConstants.MeaningfulPressureDisplayDelta ||
                !string.Equals(detail.SeverityLabel, previousSeverity, StringComparison.Ordinal));
    }

    private static PressureStatus ResolveTopPressure(GroupPressureChange pressureChange, bool useCurrent)
    {
        var pressures = new[]
        {
            BuildPressureStatus("Food", pressureChange.Food, PressureDefinitions.Food, useCurrent),
            BuildPressureStatus("Water", pressureChange.Water, PressureDefinitions.Water, useCurrent),
            BuildPressureStatus("Migration", pressureChange.Migration, PressureDefinitions.Migration, useCurrent)
        };

        return pressures
            .OrderByDescending(item => item.Display)
            .ThenBy(item => item.Category, StringComparer.Ordinal)
            .First();
    }

    private static PressureStatus BuildPressureStatus(string category, PressureChangeDetail detail, PressureDefinition definition, bool useCurrent)
    {
        if (useCurrent)
        {
            return new PressureStatus(category, detail.Display, detail.SeverityLabel);
        }

        var previousEffective = PressureMath.ComputeEffectiveValue(definition, detail.PriorRaw);
        var previousDisplay = PressureMath.ComputeDisplayValue(previousEffective);
        return new PressureStatus(category, previousDisplay, PressureMath.ComputeSeverityLabel(previousDisplay));
    }

    private static string ResolveTopPressureKey(GroupPressureChange? pressureChange)
    {
        if (pressureChange is null)
        {
            return "unknown";
        }

        return ResolveTopPressure(pressureChange, useCurrent: true).Category.ToLowerInvariant();
    }

    private static ChronicleEventSeverity ResolveExternalSeverity(string eventType)
    {
        if (string.Equals(eventType, "war", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(eventType, "raid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(eventType, "hostility", StringComparison.OrdinalIgnoreCase))
        {
            return ChronicleEventSeverity.Major;
        }

        return ChronicleEventSeverity.Notable;
    }

    private static ChronicleEventSeverity ResolvePoliticalSeverity(ChronicleTriggerKind triggerKind)
    {
        return triggerKind switch
        {
            ChronicleTriggerKind.Collapsed or ChronicleTriggerKind.Fractured => ChronicleEventSeverity.Critical,
            ChronicleTriggerKind.Unified or ChronicleTriggerKind.Split => ChronicleEventSeverity.Major,
            _ => ChronicleEventSeverity.Notable
        };
    }

    private static ChronicleEventSeverity ResolveSettlementSeverity(SettlementChangeKind kind)
    {
        if (kind == SettlementChangeKind.Abandoned)
        {
            return ChronicleEventSeverity.Major;
        }

        return ChronicleEventSeverity.Notable;
    }

    private static ChronicleTriggerKind ResolveSettlementTrigger(SettlementChangeKind kind)
    {
        return kind == SettlementChangeKind.Abandoned
            ? ChronicleTriggerKind.Abandoned
            : ChronicleTriggerKind.Founded;
    }

    private static ChronicleTriggerKind ResolveConflictTrigger(string eventType)
    {
        return eventType switch
        {
            "peace" => ChronicleTriggerKind.Recovered,
            "alliance" => ChronicleTriggerKind.Unified,
            "war" => ChronicleTriggerKind.Escalated,
            "raid" => ChronicleTriggerKind.Escalated,
            _ => ChronicleTriggerKind.Encountered
        };
    }

    private static string Normalize(string text)
    {
        return new string(text
            .ToLowerInvariant()
            .Where(character => !char.IsWhiteSpace(character) && !char.IsPunctuation(character))
            .ToArray());
    }

    private sealed record PressureStatus(string Category, int Display, string SeverityLabel);
}
