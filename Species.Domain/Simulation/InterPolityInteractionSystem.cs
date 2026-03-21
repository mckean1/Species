using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class InterPolityInteractionSystem
{
    public InterPolityResult Run(World world)
    {
        if (world.Polities.Count <= 1)
        {
            return new InterPolityResult(world, Array.Empty<InterPolityChange>());
        }

        var regionsById = world.Regions.ToDictionary(region => region.Id, StringComparer.Ordinal);
        var updatedPolities = world.Polities
            .Select(polity =>
            {
                var clone = polity.Clone();
                clone.ExternalPressure = new ExternalPressureState();
                return clone;
            })
            .ToDictionary(polity => polity.Id, StringComparer.Ordinal);
        var updatedGroups = world.PopulationGroups
            .Select(CloneGroup)
            .ToList();
        var groupsByPolityId = updatedGroups
            .GroupBy(group => group.PolityId, StringComparer.Ordinal)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray(), StringComparer.Ordinal);
        var changes = new List<InterPolityChange>();

        var polityList = updatedPolities.Values
            .OrderBy(polity => polity.Id, StringComparer.Ordinal)
            .ToArray();

        for (var index = 0; index < polityList.Length; index++)
        {
            for (var otherIndex = index + 1; otherIndex < polityList.Length; otherIndex++)
            {
                var primary = polityList[index];
                var other = polityList[otherIndex];
                var primaryGroups = groupsByPolityId.GetValueOrDefault(primary.Id) ?? Array.Empty<PopulationGroup>();
                var otherGroups = groupsByPolityId.GetValueOrDefault(other.Id) ?? Array.Empty<PopulationGroup>();

                if (primaryGroups.Length == 0 || otherGroups.Length == 0)
                {
                    continue;
                }

                var primaryContext = PolityData.BuildContext(
                    new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, updatedPolities.Values.ToArray(), world.FocalPolityId),
                    primary);
                var otherContext = PolityData.BuildContext(
                    new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, updatedPolities.Values.ToArray(), world.FocalPolityId),
                    other);
                if (primaryContext is null || otherContext is null)
                {
                    continue;
                }

                var primaryRelation = GetOrCreateRelation(primary, other.Id);
                var otherRelation = GetOrCreateRelation(other, primary.Id);
                var priorPrimaryStance = primaryRelation.Stance;
                var priorOtherStance = otherRelation.Stance;
                var contact = AnalyzeContact(primary, other, regionsById);
                UpdateRelation(primaryRelation, primaryContext, otherContext, contact, primaryIsInitiator: true);
                UpdateRelation(otherRelation, otherContext, primaryContext, contact, primaryIsInitiator: false);

                var interactionOutcome = ResolveInteraction(
                    primary,
                    other,
                    primaryContext,
                    otherContext,
                    primaryRelation,
                    otherRelation,
                    primaryGroups,
                    otherGroups,
                    regionsById);

                if (interactionOutcome.Change is not null)
                {
                    changes.Add(interactionOutcome.Change);
                }

                ApplyPressure(primary, primaryRelation);
                ApplyPressure(other, otherRelation);

                RecordStanceShift(changes, primary, other, priorPrimaryStance, primaryRelation.Stance, primaryRelation);
                RecordStanceShift(changes, other, primary, priorOtherStance, otherRelation.Stance, otherRelation);
            }
        }

        foreach (var polity in updatedPolities.Values)
        {
            polity.ExternalPressure.Summary = BuildExternalSummary(polity);
        }

        return new InterPolityResult(
            new World(world.Seed, world.CurrentYear, world.CurrentMonth, world.Regions, updatedGroups, world.Chronicle, updatedPolities.Values.OrderBy(polity => polity.Id, StringComparer.Ordinal).ToArray(), world.FocalPolityId),
            changes);
    }

    private static ContactAnalysis AnalyzeContact(
        Polity primary,
        Polity other,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        var primaryCurrent = primary.RegionalPresences
            .Where(presence => presence.IsCurrent || presence.MonthsSinceLastPresence <= 2)
            .ToArray();
        var otherCurrent = other.RegionalPresences
            .Where(presence => presence.IsCurrent || presence.MonthsSinceLastPresence <= 2)
            .ToArray();
        var primaryCurrentRegionIds = primaryCurrent.Select(presence => presence.RegionId).ToHashSet(StringComparer.Ordinal);
        var otherCurrentRegionIds = otherCurrent.Select(presence => presence.RegionId).ToHashSet(StringComparer.Ordinal);
        var sharedRegionIds = primaryCurrentRegionIds.Intersect(otherCurrentRegionIds, StringComparer.Ordinal).ToArray();

        var adjacentContacts = primaryCurrent
            .Count(presence =>
                regionsById.TryGetValue(presence.RegionId, out var region) &&
                region.NeighborIds.Any(otherCurrentRegionIds.Contains));

        var contestedRegions = sharedRegionIds.Count(regionId =>
        {
            var primaryPresence = primaryCurrent.FirstOrDefault(presence => string.Equals(presence.RegionId, regionId, StringComparison.Ordinal));
            var otherPresence = otherCurrent.FirstOrDefault(presence => string.Equals(presence.RegionId, regionId, StringComparison.Ordinal));
            return (primaryPresence?.Kind ?? PolityPresenceKind.PassingThrough) >= PolityPresenceKind.Habitation ||
                   (otherPresence?.Kind ?? PolityPresenceKind.PassingThrough) >= PolityPresenceKind.Habitation;
        });

        var primarySettlementExposure = primary.Settlements
            .Count(settlement => settlement.IsActive &&
                                 (otherCurrentRegionIds.Contains(settlement.RegionId) ||
                                  IsAdjacentRegion(settlement.RegionId, otherCurrentRegionIds, regionsById)));
        var otherSettlementExposure = other.Settlements
            .Count(settlement => settlement.IsActive &&
                                 (primaryCurrentRegionIds.Contains(settlement.RegionId) ||
                                  IsAdjacentRegion(settlement.RegionId, primaryCurrentRegionIds, regionsById)));

        return new ContactAnalysis(
            sharedRegionIds,
            sharedRegionIds.Length,
            adjacentContacts,
            contestedRegions,
            primarySettlementExposure,
            otherSettlementExposure);
    }

    private static void UpdateRelation(
        InterPolityRelation relation,
        PolityContext polity,
        PolityContext other,
        ContactAnalysis contact,
        bool primaryIsInitiator)
    {
        var attached = string.Equals(polity.ParentPolityId, other.Polity.Id, StringComparison.Ordinal) ||
                       string.Equals(other.ParentPolityId, polity.Polity.Id, StringComparison.Ordinal) ||
                       polity.Polity.PoliticalAttachments.Any(attachment => attachment.IsActive && string.Equals(attachment.RelatedPolityId, other.Polity.Id, StringComparison.Ordinal));
        var peacefulContact = contact.SharedRegions > 0 && contact.ContestedRegions == 0 && relation.Hostility < 45;
        var cooperationTarget = Math.Clamp(
            (contact.SharedRegions * 16) +
            (contact.AdjacentContacts * 8) +
            (peacefulContact ? 12 : 0) +
            (attached ? 20 : 0) +
            (Math.Min(polity.ExternalPressure.Threat, other.ExternalPressure.Threat) / 6) +
            (relation.Trust / 5) -
            (contact.ContestedRegions * 10) -
            (polity.Pressures.Food.EffectiveValue / 8),
            0,
            100);
        var hostilityTarget = Math.Clamp(
            (contact.ContestedRegions * 18) +
            (contact.SharedRegions * 8) +
            (primaryIsInitiator ? contact.OtherSettlementExposure : contact.PrimarySettlementExposure) * 7 +
            (polity.Pressures.Food.EffectiveValue / 8) +
            (polity.MaterialProduction.DeficitScore / 9) +
            (attached ? -30 : 0) +
            (relation.RaidsSuffered * 4) +
            (relation.RaidsInflicted * 2) -
            (relation.PeaceMonths / 4),
            0,
            100);
        var frontierFrictionTarget = Math.Clamp(
            (contact.ContestedRegions * 22) +
            (contact.AdjacentContacts * 10) +
            ((primaryIsInitiator ? contact.OtherSettlementExposure : contact.PrimarySettlementExposure) * 8) -
            (attached ? 18 : 0) -
            (cooperationTarget / 5),
            0,
            100);
        var trustTarget = Math.Clamp(
            45 +
            (cooperationTarget / 3) +
            (relation.CooperationMonths / 4) +
            (relation.PeaceMonths / 5) -
            (hostilityTarget / 2) -
            (relation.RaidsSuffered * 5),
            0,
            100);
        var escalationTarget = Math.Clamp(
            (hostilityTarget / 2) +
            (frontierFrictionTarget / 3) +
            (relation.RaidPressure / 2) +
            (((primaryIsInitiator ? contact.OtherSettlementExposure : contact.PrimarySettlementExposure) > 0) ? 8 : 0) -
            (attached ? 25 : 0) -
            (cooperationTarget / 6) -
            (relation.PeaceMonths / 5),
            0,
            100);

        relation.ContactIntensity = DriftToward(relation.ContactIntensity, Math.Clamp((contact.SharedRegions * 20) + (contact.AdjacentContacts * 12), 0, 100));
        relation.Cooperation = DriftToward(relation.Cooperation, cooperationTarget);
        relation.Hostility = DriftToward(relation.Hostility, hostilityTarget);
        relation.FrontierFriction = DriftToward(relation.FrontierFriction, frontierFrictionTarget);
        relation.Trust = DriftToward(relation.Trust, trustTarget);
        relation.Escalation = DriftToward(relation.Escalation, escalationTarget);
        relation.RaidPressure = Math.Max(0, relation.RaidPressure - 4);

        if (peacefulContact && relation.Hostility <= 40)
        {
            relation.CooperationMonths++;
            relation.PeaceMonths++;
        }
        else if (contact.SharedRegions + contact.AdjacentContacts > 0 && relation.Hostility <= 55)
        {
            relation.PeaceMonths++;
            relation.CooperationMonths = Math.Max(0, relation.CooperationMonths - 1);
        }
        else
        {
            relation.PeaceMonths = Math.Max(0, relation.PeaceMonths - 1);
            relation.CooperationMonths = Math.Max(0, relation.CooperationMonths - 1);
        }

        if (polity.ExternalPressure.Threat >= 50 &&
            other.ExternalPressure.Threat >= 50 &&
            relation.Hostility <= 45 &&
            relation.ContactIntensity >= 20)
        {
            relation.SharedThreatMonths++;
            relation.Cooperation = Math.Clamp(relation.Cooperation + 4, 0, 100);
            relation.Trust = Math.Clamp(relation.Trust + 2, 0, 100);
        }
        else
        {
            relation.SharedThreatMonths = Math.Max(0, relation.SharedThreatMonths - 1);
        }

        relation.Stance = ResolveStance(relation);
        relation.RecentSummary = BuildRelationSummary(relation);
    }

    private static InteractionOutcome ResolveInteraction(
        Polity primary,
        Polity other,
        PolityContext primaryContext,
        PolityContext otherContext,
        InterPolityRelation primaryRelation,
        InterPolityRelation otherRelation,
        IReadOnlyList<PopulationGroup> primaryGroups,
        IReadOnlyList<PopulationGroup> otherGroups,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        var primaryStrength = ResolvePolityStrength(primaryContext);
        var otherStrength = ResolvePolityStrength(otherContext);
        var strengthGap = Math.Abs(primaryStrength - otherStrength);
        var likelyOpenConflict = primaryRelation.Escalation >= 82 && primaryRelation.Hostility >= 68 && primaryRelation.ContactIntensity >= 20;
        var likelyRaid = !likelyOpenConflict && primaryRelation.Escalation >= 60 && primaryRelation.Hostility >= 52 && primaryRelation.ContactIntensity >= 16;
        var attached = string.Equals(primary.ParentPolityId, other.Id, StringComparison.Ordinal) ||
                       string.Equals(other.ParentPolityId, primary.Id, StringComparison.Ordinal) ||
                       primary.PoliticalAttachments.Any(attachment => attachment.IsActive && string.Equals(attachment.RelatedPolityId, other.Id, StringComparison.Ordinal)) ||
                       other.PoliticalAttachments.Any(attachment => attachment.IsActive && string.Equals(attachment.RelatedPolityId, primary.Id, StringComparison.Ordinal));

        if ((!likelyOpenConflict && !likelyRaid) || attached)
        {
            return new InteractionOutcome();
        }

        var aggressor = primaryStrength >= otherStrength ? primary : other;
        var defender = aggressor == primary ? other : primary;
        var aggressorContext = aggressor == primary ? primaryContext : otherContext;
        var defenderContext = aggressor == primary ? otherContext : primaryContext;
        var aggressorRelation = aggressor == primary ? primaryRelation : otherRelation;
        var defenderRelation = aggressor == primary ? otherRelation : primaryRelation;
        var defenderGroups = aggressor == primary ? otherGroups : primaryGroups;
        var severity = likelyOpenConflict ? 18 + (strengthGap / 8) : 10 + (strengthGap / 12);
        var defenderSettlement = ResolveFrontierSettlement(defender, aggressor, regionsById);
        var targetRegionId = defenderSettlement?.RegionId ??
                             defender.RegionalPresences.FirstOrDefault(presence => presence.IsCurrent)?.RegionId ??
                             defender.CoreRegionId;
        var affectedGroups = defenderGroups
            .Where(group => string.Equals(group.CurrentRegionId, targetRegionId, StringComparison.Ordinal))
            .ToArray();

        if (defenderSettlement is not null)
        {
            defenderSettlement.StoredFood = Math.Max(0, defenderSettlement.StoredFood - severity);
            DrainStockpile(defenderSettlement.MaterialStores, severity / 2);
            defenderSettlement.MaterialSupport = Math.Max(0, defenderSettlement.MaterialSupport - (likelyOpenConflict ? 12 : 7));
        }
        else
        {
            aggressorRelation.FrontierFriction = Math.Clamp(aggressorRelation.FrontierFriction + 4, 0, 100);
            defenderRelation.FrontierFriction = Math.Clamp(defenderRelation.FrontierFriction + 6, 0, 100);
        }

        foreach (var group in affectedGroups)
        {
            group.Pressures = group.Pressures.Clone();
            group.Pressures.Threat = PressureMath.ApplyRawAdjustment(PressureDefinitions.Threat, group.Pressures.Threat, likelyOpenConflict ? 18 : 12);
            group.Pressures.Migration = PressureMath.ApplyRawAdjustment(PressureDefinitions.Migration, group.Pressures.Migration, likelyOpenConflict ? 12 : 8);
        }

        aggressorRelation.RaidsInflicted++;
        aggressorRelation.RaidPressure = Math.Clamp(aggressorRelation.RaidPressure + (likelyOpenConflict ? 22 : 16), 0, 100);
        aggressorRelation.PeaceMonths = 0;
        aggressorRelation.Hostility = Math.Clamp(aggressorRelation.Hostility + 8, 0, 100);
        aggressorRelation.Escalation = Math.Clamp(aggressorRelation.Escalation + (likelyOpenConflict ? 10 : 6), 0, 100);

        defenderRelation.RaidsSuffered++;
        defenderRelation.RaidPressure = Math.Clamp(defenderRelation.RaidPressure + (likelyOpenConflict ? 24 : 18), 0, 100);
        defenderRelation.PeaceMonths = 0;
        defenderRelation.Hostility = Math.Clamp(defenderRelation.Hostility + 10, 0, 100);
        defenderRelation.Escalation = Math.Clamp(defenderRelation.Escalation + (likelyOpenConflict ? 12 : 8), 0, 100);

        aggressorRelation.Stance = ResolveStance(aggressorRelation);
        defenderRelation.Stance = ResolveStance(defenderRelation);
        aggressorRelation.RecentSummary = BuildRelationSummary(aggressorRelation);
        defenderRelation.RecentSummary = BuildRelationSummary(defenderRelation);

        var message = likelyOpenConflict
            ? $"{aggressor.Name} and {defender.Name} fell into open conflict along the frontier."
            : $"{aggressor.Name} raided {defender.Name} along the frontier.";

        return new InteractionOutcome
        {
            Change = new InterPolityChange
            {
                PrimaryPolityId = aggressor.Id,
                PrimaryPolityName = aggressor.Name,
                OtherPolityId = defender.Id,
                OtherPolityName = defender.Name,
                Kind = likelyOpenConflict ? "open-conflict" : "raid",
                Message = message
            }
        };
    }

    private static void ApplyPressure(Polity polity, InterPolityRelation relation)
    {
        polity.ExternalPressure.Threat = Math.Clamp(
            polity.ExternalPressure.Threat + ResolveThreatContribution(relation),
            0,
            100);
        polity.ExternalPressure.Cooperation = Math.Clamp(
            polity.ExternalPressure.Cooperation + Math.Max(0, (relation.Cooperation + relation.Trust - relation.Hostility) / 8),
            0,
            100);
        polity.ExternalPressure.FrontierFriction = Math.Clamp(
            polity.ExternalPressure.FrontierFriction + Math.Max(0, relation.FrontierFriction / 6),
            0,
            100);
        polity.ExternalPressure.RaidPressure = Math.Clamp(
            polity.ExternalPressure.RaidPressure + Math.Max(0, relation.RaidPressure / 5),
            0,
            100);

        if (relation.Stance is InterPolityStance.Hostile or InterPolityStance.RaidingConflict or InterPolityStance.OpenConflict or InterPolityStance.Rival)
        {
            polity.ExternalPressure.HostileNeighborCount++;
        }
    }

    private static int ResolveThreatContribution(InterPolityRelation relation)
    {
        var threat = (relation.Hostility / 5) + (relation.Escalation / 6) + (relation.RaidPressure / 6);
        if (relation.Stance is InterPolityStance.RaidingConflict or InterPolityStance.OpenConflict)
        {
            threat += 8;
        }

        return Math.Clamp(threat, 0, 35);
    }

    private static string BuildExternalSummary(Polity polity)
    {
        if (polity.ExternalPressure.HostileNeighborCount >= 2 || polity.ExternalPressure.RaidPressure >= 35)
        {
            return "Multiple outside pressures are destabilizing the frontier.";
        }

        if (polity.ExternalPressure.Threat >= 40)
        {
            return "Hostile neighbors are weighing on polity stability.";
        }

        if (polity.ExternalPressure.Cooperation >= 30)
        {
            return "Nearby cooperation is easing outside pressure.";
        }

        if (polity.ExternalPressure.FrontierFriction >= 20)
        {
            return "Friction is building along nearby frontiers.";
        }

        return "No major outside pressure.";
    }

    private static void RecordStanceShift(
        ICollection<InterPolityChange> changes,
        Polity polity,
        Polity other,
        InterPolityStance previous,
        InterPolityStance current,
        InterPolityRelation relation)
    {
        if (previous == current)
        {
            return;
        }

        var message = current switch
        {
            InterPolityStance.Cooperative => $"{polity.Name} and {other.Name} settled into practical cooperation.",
            InterPolityStance.Rival => $"{polity.Name} and {other.Name} grew wary along the frontier.",
            InterPolityStance.Hostile => $"{polity.Name} and {other.Name} turned openly hostile.",
            InterPolityStance.UneasyPeace => $"{polity.Name} and {other.Name} cooled into an uneasy peace.",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        changes.Add(new InterPolityChange
        {
            PrimaryPolityId = polity.Id,
            PrimaryPolityName = polity.Name,
            OtherPolityId = other.Id,
            OtherPolityName = other.Name,
            Kind = "stance-shift",
            Message = message
        });
    }

    private static Settlement? ResolveFrontierSettlement(
        Polity defender,
        Polity aggressor,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        var aggressorRegions = aggressor.RegionalPresences
            .Where(presence => presence.IsCurrent || presence.MonthsSinceLastPresence <= 2)
            .Select(presence => presence.RegionId)
            .ToHashSet(StringComparer.Ordinal);

        return defender.Settlements
            .Where(settlement => settlement.IsActive &&
                                 (aggressorRegions.Contains(settlement.RegionId) ||
                                  IsAdjacentRegion(settlement.RegionId, aggressorRegions, regionsById)))
            .OrderByDescending(settlement => !settlement.IsPrimary)
            .ThenByDescending(settlement => settlement.MaterialStores.Total + settlement.StoredFood)
            .ThenBy(settlement => settlement.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static bool IsAdjacentRegion(
        string regionId,
        IReadOnlySet<string> otherRegionIds,
        IReadOnlyDictionary<string, Region> regionsById)
    {
        return regionsById.TryGetValue(regionId, out var region) &&
               region.NeighborIds.Any(otherRegionIds.Contains);
    }

    private static InterPolityRelation GetOrCreateRelation(Polity polity, string otherPolityId)
    {
        var relation = polity.InterPolityRelations
            .FirstOrDefault(item => string.Equals(item.OtherPolityId, otherPolityId, StringComparison.Ordinal));
        if (relation is not null)
        {
            return relation;
        }

        relation = new InterPolityRelation
        {
            OtherPolityId = otherPolityId,
            Stance = InterPolityStance.Unknown
        };
        polity.InterPolityRelations.Add(relation);
        return relation;
    }

    private static InterPolityStance ResolveStance(InterPolityRelation relation)
    {
        if (relation.Escalation >= 85)
        {
            return InterPolityStance.OpenConflict;
        }

        if (relation.Escalation >= 65 || relation.RaidPressure >= 40)
        {
            return InterPolityStance.RaidingConflict;
        }

        if (relation.Hostility >= 68)
        {
            return InterPolityStance.Hostile;
        }

        if (relation.Hostility >= 52 || relation.FrontierFriction >= 50)
        {
            return InterPolityStance.Rival;
        }

        if ((relation.RaidsInflicted > 0 || relation.RaidsSuffered > 0) &&
            relation.PeaceMonths >= 6 &&
            relation.Hostility <= 45)
        {
            return InterPolityStance.UneasyPeace;
        }

        if (relation.Cooperation >= 58 && relation.Trust >= 54)
        {
            return InterPolityStance.Cooperative;
        }

        if (relation.ContactIntensity >= 20 && (relation.FrontierFriction >= 28 || relation.Hostility >= 30))
        {
            return InterPolityStance.Wary;
        }

        if (relation.ContactIntensity >= 15)
        {
            return InterPolityStance.Neutral;
        }

        return InterPolityStance.Unknown;
    }

    private static string BuildRelationSummary(InterPolityRelation relation)
    {
        return relation.Stance switch
        {
            InterPolityStance.Cooperative => "Repeated peaceful contact is supporting cooperation.",
            InterPolityStance.Rival => "Contested frontier use is hardening rivalry.",
            InterPolityStance.Hostile => "Hostility is rising through repeated friction.",
            InterPolityStance.RaidingConflict => "Raids are driving the relationship.",
            InterPolityStance.OpenConflict => "Open conflict is disrupting the frontier.",
            InterPolityStance.UneasyPeace => "Old violence lingers beneath a brittle peace.",
            InterPolityStance.Wary => "Frontier contact remains uneasy.",
            InterPolityStance.Neutral => "Contact remains limited but non-hostile.",
            _ => "No meaningful outside relationship is established yet."
        };
    }

    private static int ResolvePolityStrength(PolityContext context)
    {
        return Math.Max(
            1,
            context.TotalPopulation +
            (context.MaterialProduction.ToolSupport * 4) +
            (context.MaterialProduction.ShelterSupport * 2) +
            (context.Governance.Authority * 3) +
            (context.ScaleState.Centralization * 2) +
            (context.ScaleState.IntegrationDepth * 2) +
            (context.ExternalPressure.Threat / 2));
    }

    private static void DrainStockpile(MaterialStockpile stockpile, int amount)
    {
        if (amount <= 0 || stockpile.Total <= 0)
        {
            return;
        }

        var perType = Math.Max(1, amount / 5);
        stockpile.Timber = Math.Max(0, stockpile.Timber - perType);
        stockpile.Stone = Math.Max(0, stockpile.Stone - perType);
        stockpile.Fiber = Math.Max(0, stockpile.Fiber - perType);
        stockpile.Clay = Math.Max(0, stockpile.Clay - perType);
        stockpile.Hides = Math.Max(0, stockpile.Hides - perType);
    }

    private static PopulationGroup CloneGroup(PopulationGroup group)
    {
        return new PopulationGroup
        {
            Id = group.Id,
            Name = group.Name,
            SpeciesId = group.SpeciesId,
            PolityId = group.PolityId,
            CurrentRegionId = group.CurrentRegionId,
            OriginRegionId = group.OriginRegionId,
            Population = group.Population,
            StoredFood = group.StoredFood,
            FoodAccounting = group.FoodAccounting.Clone(),
            HungerPressure = group.HungerPressure,
            ShortageMonths = group.ShortageMonths,
            FoodStressState = group.FoodStressState,
            SubsistencePreference = group.SubsistencePreference,
            SubsistenceMode = group.SubsistenceMode,
            Pressures = group.Pressures.Clone(),
            LastRegionId = group.LastRegionId,
            MonthsSinceLastMove = group.MonthsSinceLastMove,
            KnownRegionIds = new HashSet<string>(group.KnownRegionIds, StringComparer.Ordinal),
            KnownDiscoveryIds = new HashSet<string>(group.KnownDiscoveryIds, StringComparer.Ordinal),
            DiscoveryEvidence = group.DiscoveryEvidence.Clone(),
            LearnedAdvancementIds = new HashSet<string>(group.LearnedAdvancementIds, StringComparer.Ordinal),
            AdvancementEvidence = group.AdvancementEvidence.Clone()
        };
    }

    private static int DriftToward(int current, int target)
    {
        return Math.Clamp((current * 2 + target) / 3, 0, 100);
    }

    private sealed record ContactAnalysis(
        IReadOnlyList<string> SharedRegionIds,
        int SharedRegions,
        int AdjacentContacts,
        int ContestedRegions,
        int PrimarySettlementExposure,
        int OtherSettlementExposure);

    private sealed class InteractionOutcome
    {
        public InterPolityChange? Change { get; init; }
    }
}
