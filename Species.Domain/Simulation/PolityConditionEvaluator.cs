using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public sealed class PolityConditionEvaluator
{
    private static readonly GovernmentForm[] DowngradeOrder =
    [
        GovernmentForm.ImperialBureaucracy,
        GovernmentForm.AbsoluteRule,
        GovernmentForm.Theocracy,
        GovernmentForm.FeudalRule,
        GovernmentForm.Republic,
        GovernmentForm.MerchantRule,
        GovernmentForm.Confederation,
        GovernmentForm.CouncilRule,
        GovernmentForm.DespoticRule,
        GovernmentForm.TribalClanRule
    ];

    public World FinalizePolities(World world)
    {
        if (world.Polities.Count == 0)
        {
            return world;
        }

        var updatedPolities = world.Polities
            .Select(polity => polity.Clone())
            .ToArray();
        var finalizedWorld = new World(
            world.Seed,
            world.CurrentYear,
            world.CurrentMonth,
            world.Regions,
            world.PopulationGroups,
            world.Chronicle,
            updatedPolities,
            world.FocalPolityId);

        foreach (var polity in updatedPolities)
        {
            var snapshot = Evaluate(finalizedWorld, polity);
            ApplySnapshot(polity, snapshot);
        }

        return finalizedWorld;
    }

    public PolityConditionSnapshot Evaluate(World world, Polity polity)
    {
        var context = PolityData.BuildContext(world, polity);
        if (context?.LeadGroup is null)
        {
            var defaultMaterial = new MaterialSurvivalAssessment(
                PolityConditionSeverity.Stable,
                "No food accounting is available.",
                PolityConditionSeverity.Stable,
                PolityConditionSeverity.Stable,
                PolityConditionSeverity.Stable,
                PolityConditionSeverity.Stable,
                PolityConditionSeverity.Stable,
                "No living conditions strain is visible.",
                PolityConditionSeverity.Stable,
                false,
                false,
                false,
                false);
            var defaultSpatial = new SpatialStabilityAssessment(
                PolityAnchoringKind.Mobile,
                false,
                false,
                false,
                false,
                PolityConditionSeverity.Collapse,
                "This polity has no viable stable base.");
            var defaultIntegrity = new PolityIntegrityAssessment(0, PolityIntegrityBand.NearCollapse, PoliticalScaleForm.LocalPolity, "This polity is effectively collapsing.");
            var defaultGovernance = new GovernanceConditionAssessment(
                new GovernanceState(),
                GovernanceConditionBand.Collapsing,
                "Governance has broken down.",
                0,
                0,
                0,
                0,
                Array.Empty<string>());
            return new PolityConditionSnapshot(
                polity.Id,
                GovernmentForm.TribalClanRule,
                PoliticalScaleForm.LocalPolity,
                PolityAnchoringKind.Mobile,
                defaultMaterial,
                defaultSpatial,
                defaultIntegrity,
                defaultGovernance,
                ["No viable governing base remains."],
                Array.Empty<string>(),
                ["This polity no longer holds together as a stable political unit."],
                ["Governance has collapsed."],
                ["Political scale has fallen back to a local footing."],
                "This polity is near collapse.");
        }

        var material = AssessMaterialSurvival(context);
        var spatial = AssessSpatialStability(context, material);
        var integrity = AssessIntegrity(context, material, spatial);
        var governance = AssessGovernance(context, material, spatial, integrity);
        var scaleForm = ValidateScaleForm(context, integrity, governance);
        var governmentForm = ValidateGovernmentForm(context, material, spatial, integrity, governance, scaleForm);
        var currentIssues = BuildCurrentIssues(context, material, spatial, integrity, governance);
        var strengths = BuildStrengths(context, material, spatial, integrity, governance);
        var problems = BuildProblems(context, material, spatial, integrity, governance);
        var governanceNotes = BuildGovernanceNotes(governance, material, spatial, integrity);
        var scaleNotes = BuildScaleNotes(context, integrity, spatial, scaleForm);
        var summary = BuildSummary(material, spatial, integrity, governance, governmentForm, scaleForm);

        return new PolityConditionSnapshot(
            polity.Id,
            governmentForm,
            scaleForm,
            spatial.AnchoringKind,
            material,
            spatial,
            integrity,
            governance,
            currentIssues,
            strengths,
            problems,
            governanceNotes,
            scaleNotes,
            summary);
    }

    private static MaterialSurvivalAssessment AssessMaterialSurvival(PolityContext context)
    {
        // Food condition is food-only. It is derived from finalized food accounting and food pressure,
        // while broader non-food material weakness is tracked separately as living conditions strain.
        var food = ClassifyFoodCondition(context, out var foodReason);
        var waterPressure = context.Pressures.Water.EffectiveValue;
        var threatPressure = Math.Max(context.Pressures.Threat.EffectiveValue, context.ExternalPressure.Threat);
        var crowdingPressure = context.Pressures.Overcrowding.EffectiveValue;
        var migrationPressure = context.Pressures.Migration.EffectiveValue + Math.Max(0, context.SocialIdentity.Mobility - 55) / 3;
        var materialFragility = ClassifyMaterialFragility(context, out var materialFragilityReason);

        var water = ClassifyPressure(waterPressure, context.Pressures.Water.DisplayValue);
        var threat = ClassifyPressure(threatPressure, context.Pressures.Threat.DisplayValue);
        var crowding = ClassifyPressure(crowdingPressure, context.Pressures.Overcrowding.DisplayValue);
        var migration = ClassifyPressure(migrationPressure, context.Pressures.Migration.DisplayValue);
        var sustainedShortage = context.MaterialShortageMonths >= PolityConditionConstants.SustainedShortageMonthsThreshold;

        var hasCriticalFoodWater = food >= PolityConditionSeverity.Critical || water >= PolityConditionSeverity.Critical;
        var hasExtremeMigration =
            context.Pressures.Migration.DisplayValue >= PolityConditionConstants.ExtremeMigrationPressureThreshold ||
            (context.Pressures.Migration.DisplayValue >= PolityConditionConstants.ElevatedMigrationPressureThreshold &&
             context.MaterialShortageMonths >= PolityConditionConstants.SustainedShortageMonthsThreshold);
        var hasMaterialShortage = sustainedShortage || context.MaterialProduction.DeficitScore >= PolityConditionConstants.StrainedPressureThreshold;
        var overallSeverity = MaxSeverity(food, water, threat, crowding, migration, materialFragility);

        return new MaterialSurvivalAssessment(
            food,
            foodReason,
            water,
            threat,
            crowding,
            migration,
            materialFragility,
            materialFragilityReason,
            overallSeverity,
            hasCriticalFoodWater,
            hasExtremeMigration,
            hasMaterialShortage,
            materialFragility > PolityConditionSeverity.Stable);
    }

    private static SpatialStabilityAssessment AssessSpatialStability(PolityContext context, MaterialSurvivalAssessment material)
    {
        var activeSettlements = context.Polity.Settlements.Where(settlement => settlement.IsActive).ToArray();
        var primarySite = activeSettlements.FirstOrDefault(settlement => settlement.IsPrimary)
            ?? activeSettlements.FirstOrDefault(settlement => string.Equals(settlement.Id, context.Polity.PrimarySettlementId, StringComparison.Ordinal));
        var hasValidSeat = primarySite is not null;
        var corePresence = context.Polity.RegionalPresences
            .FirstOrDefault(presence =>
            presence.IsCurrent &&
            string.Equals(presence.RegionId, context.CoreRegionId, StringComparison.Ordinal));
        var hasCorePresence = corePresence is not null;
        var hasRecentCorePresence = hasCorePresence || context.Polity.RegionalPresences.Any(presence =>
            string.Equals(presence.RegionId, context.CoreRegionId, StringComparison.Ordinal) &&
            presence.MonthsSinceLastPresence <= PolityConditionConstants.RecentCorePresenceMonthsThreshold);
        if (!hasRecentCorePresence)
        {
            hasRecentCorePresence = context.MemberGroups.Any(group => string.Equals(group.CurrentRegionId, context.CoreRegionId, StringComparison.Ordinal));
        }

        var seatLossIsTemporary = !hasValidSeat &&
                                  context.Polity.Settlements.Any(settlement =>
                                      settlement.IsActive &&
                                      settlement.MaterialShortageMonths < PolityConditionConstants.SeatLossGraceMonthsThreshold);
        var unresolvedDisplacement = material.HasExtremeMigration &&
                                     (!hasRecentCorePresence || (!hasValidSeat && !seatLossIsTemporary)) &&
                                     context.MaterialShortageMonths >= PolityConditionConstants.SustainedShortageMonthsThreshold;
        var stableBase = hasRecentCorePresence && (!material.HasExtremeMigration || hasValidSeat || seatLossIsTemporary);
        var anchoring = context.AnchoringKind;
        if (unresolvedDisplacement)
        {
            anchoring = hasRecentCorePresence ? PolityAnchoringKind.Seasonal : PolityAnchoringKind.Mobile;
        }
        else if (!stableBase)
        {
            anchoring = hasRecentCorePresence
                ? PolityAnchoringKind.SemiRooted
                : context.AnchoringKind == PolityAnchoringKind.Anchored
                    ? PolityAnchoringKind.Seasonal
                    : PolityAnchoringKind.Mobile;
        }
        else if (!hasValidSeat && anchoring == PolityAnchoringKind.Anchored)
        {
            anchoring = PolityAnchoringKind.SemiRooted;
        }

        var severity = unresolvedDisplacement
            ? PolityConditionSeverity.Critical
            : stableBase
                ? PolityConditionSeverity.Stable
                : PolityConditionSeverity.Strained;
        var summary = anchoring switch
        {
            PolityAnchoringKind.Anchored when hasValidSeat => "The polity still holds an anchored core with a functioning primary site.",
            PolityAnchoringKind.SemiRooted => "The polity keeps a partial base, but its anchoring is weakening.",
            PolityAnchoringKind.Seasonal => "The polity relies on a weak seasonal base rather than a durable seat.",
            _ => "The polity is operating without a reliable anchored center."
        };

        return new SpatialStabilityAssessment(
            anchoring,
            hasValidSeat,
            hasRecentCorePresence,
            stableBase,
            unresolvedDisplacement,
            severity,
            summary);
    }

    private static PolityIntegrityAssessment AssessIntegrity(
        PolityContext context,
        MaterialSurvivalAssessment material,
        SpatialStabilityAssessment spatial)
    {
        var score = 100;
        score -= context.ScaleState.FragmentationRisk / 2;
        score -= context.ScaleState.OverextensionPressure / 3;
        score -= context.Governance.PeripheralStrain / 3;
        score -= context.ScaleState.DistanceStrain / 4;
        score -= material.OverallSeverity switch
        {
            PolityConditionSeverity.Collapse => 40,
            PolityConditionSeverity.Critical => 26,
            PolityConditionSeverity.Strained => 12,
            _ => 0
        };
        score -= spatial.IsDisplaced ? 22 : 0;
        score -= spatial.HasStableBase ? 0 : 12;
        score += context.TotalPopulation >= PolityConditionConstants.SeatRequiredPopulationThreshold ? 6 : 0;
        score += context.ScaleState.IntegrationDepth / 5;
        score += context.ScaleState.Centralization / 8;
        score += context.ScaleState.ScaleContinuityMonths >= PolityConditionConstants.FormRetentionScaleMonths ? 4 : 0;
        score += context.MaterialSurplusMonths >= PolityConditionConstants.SustainedShortageMonthsThreshold ? 4 : 0;
        score = Math.Clamp(score, 0, 100);

        var band = ResolveIntegrityBand(score, context.ScaleState.Form);
        var form = band switch
        {
            PolityIntegrityBand.Coherent => context.ScaleState.Form,
            PolityIntegrityBand.Strained => context.ScaleState.Form is PoliticalScaleForm.EmpireLike
                ? PoliticalScaleForm.CompositeRealm
                : context.ScaleState.Form,
            PolityIntegrityBand.Unstable => context.ScaleState.Form is PoliticalScaleForm.LocalPolity
                ? PoliticalScaleForm.LocalPolity
                : PoliticalScaleForm.Fragmenting,
            _ => PoliticalScaleForm.LocalPolity
        };
        var summary = band switch
        {
            PolityIntegrityBand.Coherent => "The polity still functions as a coherent political whole.",
            PolityIntegrityBand.Strained => "The polity remains intact, but strain is visibly accumulating.",
            PolityIntegrityBand.Unstable => "The polity is unstable and struggling to maintain coherent rule.",
            _ => "The polity is close to collapse."
        };

        return new PolityIntegrityAssessment(score, band, form, summary);
    }

    private static GovernanceConditionAssessment AssessGovernance(
        PolityContext context,
        MaterialSurvivalAssessment material,
        SpatialStabilityAssessment spatial,
        PolityIntegrityAssessment integrity)
    {
        var legitimacy = context.Governance.Legitimacy
            - ResolveSeverityPenalty(material.FoodCondition, 5, 11, 18)
            - ResolveSeverityPenalty(material.WaterCondition, 4, 9, 15)
            - ResolveSeverityPenalty(material.MigrationCondition, 2, 6, 10)
            - (context.MaterialShortageMonths * 2)
            - (context.ScaleState.FragmentationRisk / 8)
            + (context.SocialIdentity.Communalism / 12);
        var cohesion = context.Governance.Cohesion
            - ResolveSeverityPenalty(material.MigrationCondition, 4, 10, 16)
            - (spatial.IsDisplaced ? 14 : 0)
            - (context.ScaleState.FragmentationRisk / 7)
            - (context.ExternalPressure.RaidPressure / 8)
            + (spatial.HasStableBase ? 6 : 0);
        var authority = context.Governance.Authority
            - (integrity.Band == PolityIntegrityBand.NearCollapse ? 20 : 0)
            - (integrity.Band == PolityIntegrityBand.Unstable ? 10 : 0)
            - (context.Governance.PeripheralStrain / 7)
            - (context.ScaleState.FragmentationRisk / 8)
            - ResolveSeverityPenalty(material.OverallSeverity, 0, 5, 10)
            + (context.SocialIdentity.OrderOrientation / 14);
        var governability = (int)Math.Round(
            (legitimacy * 0.34) +
            (cohesion * 0.28) +
            (authority * 0.24) +
            (integrity.IntegrityScore * 0.14) -
            (context.Governance.PeripheralStrain * 0.22),
            MidpointRounding.AwayFromZero);

        var governance = new GovernanceState
        {
            Legitimacy = Math.Clamp(legitimacy, 0, 100),
            Cohesion = Math.Clamp(cohesion, 0, 100),
            Authority = Math.Clamp(authority, 0, 100),
            Governability = Math.Clamp(governability, 0, 100),
            PeripheralStrain = Math.Clamp(Math.Max(context.Governance.PeripheralStrain, context.ScaleState.OverextensionPressure), 0, 100)
        };
        var aggregate = (int)Math.Round(
            (governance.Governability * 0.40) +
            (governance.Legitimacy * 0.20) +
            (governance.Cohesion * 0.20) +
            (governance.Authority * 0.20),
            MidpointRounding.AwayFromZero);
        var isGovernanceCollapse =
            governance.Governability < 20 &&
            (governance.Legitimacy < 25 || governance.Cohesion < 25);
        var band = isGovernanceCollapse
            ? GovernanceConditionBand.Collapsing
            : ResolveGovernanceBand(aggregate, context.Governance.Governability);
        var summary = band switch
        {
            GovernanceConditionBand.Functional => "Governance remains functionally intact.",
            GovernanceConditionBand.Strained => "Governance is under growing strain.",
            GovernanceConditionBand.Failing => "Governance is failing as living conditions worsen.",
            _ => "Governance is collapsing as living conditions and political integrity break down."
        };

        return new GovernanceConditionAssessment(
            governance,
            band,
            summary,
            governance.Legitimacy - context.Governance.Legitimacy,
            governance.Cohesion - context.Governance.Cohesion,
            governance.Authority - context.Governance.Authority,
            governance.Governability - context.Governance.Governability,
            Array.Empty<string>());
    }

    private static PoliticalScaleForm ValidateScaleForm(
        PolityContext context,
        PolityIntegrityAssessment integrity,
        GovernanceConditionAssessment governance)
    {
        if (integrity.Band == PolityIntegrityBand.NearCollapse &&
            (context.MaterialShortageMonths >= PolityConditionConstants.SustainedShortageMonthsThreshold ||
             governance.Band == GovernanceConditionBand.Collapsing))
        {
            return PoliticalScaleForm.LocalPolity;
        }

        if (integrity.Band == PolityIntegrityBand.Unstable || governance.Band == GovernanceConditionBand.Collapsing)
        {
            return context.ScaleState.Form is PoliticalScaleForm.LocalPolity
                ? PoliticalScaleForm.LocalPolity
                : PoliticalScaleForm.Fragmenting;
        }

        if (context.ScaleState.Form == PoliticalScaleForm.EmpireLike &&
            (integrity.Band != PolityIntegrityBand.Coherent || governance.Governance.Governability < 60))
        {
            return PoliticalScaleForm.CompositeRealm;
        }

        return integrity.ScaleForm;
    }

    private static GovernmentForm ValidateGovernmentForm(
        PolityContext context,
        MaterialSurvivalAssessment material,
        SpatialStabilityAssessment spatial,
        PolityIntegrityAssessment integrity,
        GovernanceConditionAssessment governance,
        PoliticalScaleForm scaleForm)
    {
        if (QualifiesFor(context.Polity.GovernmentForm, context, material, spatial, integrity, governance, scaleForm, retainingCurrentForm: true))
        {
            return context.Polity.GovernmentForm;
        }

        return DowngradeOrder
            .SkipWhile(candidate => candidate != context.Polity.GovernmentForm)
            .First(candidate => QualifiesFor(candidate, context, material, spatial, integrity, governance, scaleForm, retainingCurrentForm: false));
    }

    private static bool QualifiesFor(
        GovernmentForm form,
        PolityContext context,
        MaterialSurvivalAssessment material,
        SpatialStabilityAssessment spatial,
        PolityIntegrityAssessment integrity,
        GovernanceConditionAssessment governance,
        PoliticalScaleForm scaleForm,
        bool retainingCurrentForm)
    {
        var populationBuffer = retainingCurrentForm ? PolityConditionConstants.FormRetentionPopulationBuffer : 0;
        var governanceBuffer = retainingCurrentForm ? PolityConditionConstants.FormRetentionGovernanceBuffer : 0;
        var integrityBuffer = retainingCurrentForm ? PolityConditionConstants.FormRetentionIntegrityBuffer : 0;

        if (material.HasCriticalFoodWater && form is GovernmentForm.ImperialBureaucracy or GovernmentForm.AbsoluteRule or GovernmentForm.Republic)
        {
            return false;
        }

        if (!spatial.HasValidSeat && form is GovernmentForm.ImperialBureaucracy or GovernmentForm.FeudalRule or GovernmentForm.Republic or GovernmentForm.AbsoluteRule)
        {
            return false;
        }

        if (integrity.Band == PolityIntegrityBand.NearCollapse &&
            form is not GovernmentForm.TribalClanRule and not GovernmentForm.DespoticRule)
        {
            return false;
        }

        return form switch
        {
            GovernmentForm.ImperialBureaucracy =>
                context.TotalPopulation >= 180 - populationBuffer &&
                scaleForm == PoliticalScaleForm.EmpireLike &&
                integrity.IntegrityScore >= PolityConditionConstants.CoherentIntegrityThreshold - integrityBuffer &&
                governance.Governance.Legitimacy >= 55 - governanceBuffer &&
                governance.Governance.Cohesion >= 55 - governanceBuffer &&
                governance.Governance.Governability >= 60 - governanceBuffer &&
                spatial.HasValidSeat,
            GovernmentForm.AbsoluteRule =>
                context.TotalPopulation >= 120 - populationBuffer &&
                scaleForm is PoliticalScaleForm.KingdomRealm or PoliticalScaleForm.CompositeRealm or PoliticalScaleForm.EmpireLike &&
                integrity.IntegrityScore >= PolityConditionConstants.StrainedIntegrityThreshold - integrityBuffer &&
                governance.Governance.Authority >= 58 - governanceBuffer &&
                governance.Governance.Governability >= 52 - governanceBuffer &&
                spatial.HasValidSeat,
            GovernmentForm.Theocracy =>
                context.TotalPopulation >= 90 - populationBuffer &&
                governance.Governance.Legitimacy >= 50 - governanceBuffer &&
                governance.Governance.Cohesion >= 48 - governanceBuffer &&
                integrity.Band is not PolityIntegrityBand.NearCollapse,
            GovernmentForm.FeudalRule =>
                context.TotalPopulation >= 100 - populationBuffer &&
                scaleForm is PoliticalScaleForm.RegionalState or PoliticalScaleForm.KingdomRealm or PoliticalScaleForm.CompositeRealm &&
                governance.Governance.Authority >= 45 - governanceBuffer &&
                governance.Governance.Governability >= 42 - governanceBuffer &&
                spatial.HasValidSeat,
            GovernmentForm.Republic =>
                context.TotalPopulation >= 90 - populationBuffer &&
                scaleForm is not PoliticalScaleForm.LocalPolity &&
                governance.Governance.Legitimacy >= 54 - governanceBuffer &&
                governance.Governance.Cohesion >= 50 - governanceBuffer &&
                governance.Governance.Governability >= 50 - governanceBuffer &&
                integrity.IntegrityScore >= PolityConditionConstants.StrainedIntegrityThreshold - integrityBuffer &&
                spatial.HasValidSeat,
            GovernmentForm.MerchantRule =>
                context.TotalPopulation >= 65 - populationBuffer &&
                governance.Governance.Governability >= 42 - governanceBuffer &&
                context.MaterialProduction.StorageSupport >= 35 &&
                material.FoodCondition < PolityConditionSeverity.Collapse,
            GovernmentForm.Confederation =>
                governance.Governance.Cohesion >= 34 - governanceBuffer &&
                integrity.IntegrityScore >= PolityConditionConstants.UnstableIntegrityThreshold - integrityBuffer,
            GovernmentForm.CouncilRule =>
                governance.Governance.Legitimacy >= 34 - governanceBuffer &&
                governance.Governance.Cohesion >= 34 - governanceBuffer,
            GovernmentForm.DespoticRule =>
                governance.Governance.Authority >= 28 - governanceBuffer,
            _ => true
        };
    }

    private static IReadOnlyList<string> BuildCurrentIssues(
        PolityContext context,
        MaterialSurvivalAssessment material,
        SpatialStabilityAssessment spatial,
        PolityIntegrityAssessment integrity,
        GovernanceConditionAssessment governance)
    {
        var issues = new List<(int Priority, string Text)>();
        if (material.FoodCondition >= PolityConditionSeverity.Critical)
        {
            issues.Add((100, DescribeFoodIssue(context.FoodAccounting, material.FoodCondition)));
        }

        if (material.WaterCondition >= PolityConditionSeverity.Critical)
        {
            issues.Add((96, "Water access is in a critical state."));
        }

        if (material.MigrationCondition >= PolityConditionSeverity.Critical)
        {
            issues.Add((92, "Migration pressure is forcing instability."));
        }

        if (spatial.IsDisplaced || !spatial.HasStableBase)
        {
            issues.Add((88, "The polity no longer holds a reliably stable base."));
        }

        if (integrity.Band >= PolityIntegrityBand.Unstable)
        {
            issues.Add((80, "Political coherence is breaking down."));
        }

        if (governance.Band >= GovernanceConditionBand.Failing)
        {
            issues.Add((72, "Governance is failing under current conditions."));
        }

        if (material.MaterialFragilityCondition >= PolityConditionSeverity.Critical)
        {
            issues.Add((68, DescribeMaterialFragilityIssue(material, context)));
        }

        if (material.OverallSeverity == PolityConditionSeverity.Strained && issues.Count == 0)
        {
            issues.Add((40, material.MaterialFragilityCondition >= PolityConditionSeverity.Strained
                ? DescribeMaterialFragilityIssue(material, context)
                : "Living conditions are under growing strain."));
        }

        return issues.Count > 0
            ? issues.OrderByDescending(item => item.Priority).Select(item => item.Text).Take(PolityConditionConstants.MaxDisplayedIssues).ToArray()
            : ["No urgent current issues."];
    }

    private static IReadOnlyList<string> BuildStrengths(
        PolityContext context,
        MaterialSurvivalAssessment material,
        SpatialStabilityAssessment spatial,
        PolityIntegrityAssessment integrity,
        GovernanceConditionAssessment governance)
    {
        if (material.HasCriticalFoodWater || material.MigrationCondition >= PolityConditionSeverity.Critical || integrity.Band >= PolityIntegrityBand.Unstable)
        {
            return Array.Empty<string>();
        }

        var strengths = new List<string>();
        if (material.FoodCondition == PolityConditionSeverity.Stable)
        {
            strengths.Add("Food stores and intake remain stable.");
        }

        if (material.WaterCondition == PolityConditionSeverity.Stable)
        {
            strengths.Add("Water access remains stable.");
        }

        if (spatial.HasStableBase && spatial.HasValidSeat)
        {
            strengths.Add("The polity still holds a functioning core seat.");
        }

        if (governance.Band == GovernanceConditionBand.Functional)
        {
            strengths.Add("Governance remains functionally coordinated.");
        }

        if (context.MaterialProduction.SurplusScore >= 25)
        {
            strengths.Add("Material production is reinforcing resilience.");
        }

        if (material.OverallSeverity == PolityConditionSeverity.Strained && strengths.Count > 1)
        {
            strengths = strengths.Take(1).ToList();
        }

        return strengths.Count > 0
            ? strengths.Take(PolityConditionConstants.MaxDisplayedStrengths).ToArray()
            : ["No standout strengths yet."];
    }

    private static IReadOnlyList<string> BuildProblems(
        PolityContext context,
        MaterialSurvivalAssessment material,
        SpatialStabilityAssessment spatial,
        PolityIntegrityAssessment integrity,
        GovernanceConditionAssessment governance)
    {
        var problems = new List<string>();
        if (material.FoodCondition >= PolityConditionSeverity.Strained)
        {
            problems.Add(DescribeFoodProblem(material, context.FoodAccounting));
        }

        if (material.WaterCondition >= PolityConditionSeverity.Strained)
        {
            problems.Add($"Water conditions are {material.WaterCondition.ToString().ToLowerInvariant()}.");
        }

        if (material.MigrationCondition >= PolityConditionSeverity.Strained)
        {
            problems.Add("Migration pressure is destabilizing the polity.");
        }

        if (!spatial.HasValidSeat && spatial.AnchoringKind == PolityAnchoringKind.Mobile)
        {
            problems.Add("No viable administrative seat remains.");
        }

        if (integrity.Band >= PolityIntegrityBand.Unstable)
        {
            problems.Add("Fragmentation risk is now structurally serious.");
        }

        if (governance.Band >= GovernanceConditionBand.Failing)
        {
            problems.Add("Governance is no longer operating effectively.");
        }

        if (material.MaterialFragilityCondition >= PolityConditionSeverity.Strained)
        {
            problems.Add(DescribeMaterialFragilityProblem(material, context));
        }

        return problems.Count > 0
            ? problems.Distinct(StringComparer.Ordinal).Take(PolityConditionConstants.MaxDisplayedProblems).ToArray()
            : ["No acute problems right now."];
    }

    private static IReadOnlyList<string> BuildGovernanceNotes(
        GovernanceConditionAssessment governance,
        MaterialSurvivalAssessment material,
        SpatialStabilityAssessment spatial,
        PolityIntegrityAssessment integrity)
    {
        return
        [
            $"Legitimacy: {governance.Governance.Legitimacy}",
            $"Cohesion: {governance.Governance.Cohesion}",
            $"Authority: {governance.Governance.Authority}",
            $"Governability: {governance.Governance.Governability}",
            $"Condition: {governance.Summary}",
            spatial.IsDisplaced || material.HasCriticalFoodWater || integrity.Band >= PolityIntegrityBand.Unstable
                ? "Worsening living conditions and integrity failures are directly reducing governance performance."
                : "Governance is still supported by workable living conditions and a stable footing."
        ];
    }

    private static IReadOnlyList<string> BuildScaleNotes(
        PolityContext context,
        PolityIntegrityAssessment integrity,
        SpatialStabilityAssessment spatial,
        PoliticalScaleForm scaleForm)
    {
        return
        [
            $"State form: {DescribeScaleForm(scaleForm)}",
            $"Integrity: {integrity.IntegrityScore} [{integrity.Band}]",
            $"Coordination {context.ScaleState.CoordinationStrain} | Distance {context.ScaleState.DistanceStrain} | Overreach {context.ScaleState.OverextensionPressure} | Fragmentation {context.ScaleState.FragmentationRisk}",
            integrity.Summary,
            spatial.Summary
        ];
    }

    private static string BuildSummary(
        MaterialSurvivalAssessment material,
        SpatialStabilityAssessment spatial,
        PolityIntegrityAssessment integrity,
        GovernanceConditionAssessment governance,
        GovernmentForm governmentForm,
        PoliticalScaleForm scaleForm)
    {
        var governmentLabel = DescribeGovernmentForm(governmentForm);
        var scaleLabel = DescribeScaleForm(scaleForm);
        if (material.HasCriticalFoodWater || governance.Band == GovernanceConditionBand.Collapsing || integrity.Band == PolityIntegrityBand.NearCollapse)
        {
            return $"This {governmentLabel} is in severe crisis as its {scaleLabel} base fails to hold.";
        }

        if (spatial.IsDisplaced || material.HasExtremeMigration || integrity.Band == PolityIntegrityBand.Unstable)
        {
            return $"This {governmentLabel} is still functioning, but its political footing is unstable.";
        }

        return $"This {governmentLabel} remains a {scaleLabel} with a presently workable governing base.";
    }

    private static void ApplySnapshot(Polity polity, PolityConditionSnapshot snapshot)
    {
        polity.GovernmentForm = snapshot.GovernmentForm;
        polity.AnchoringKind = snapshot.AnchoringKind;
        polity.Governance = snapshot.Governance.Governance.Clone();
        polity.ScaleState.Form = snapshot.ScaleForm;
        polity.ScaleState.Summary = snapshot.Integrity.Summary;
        polity.ExternalPressure.Summary = snapshot.Summary;

        if (!snapshot.SpatialStability.HasValidSeat)
        {
            polity.PrimarySettlementId = string.Empty;
            foreach (var settlement in polity.Settlements)
            {
                settlement.IsPrimary = false;
            }

            return;
        }

        var primary = polity.Settlements.FirstOrDefault(settlement =>
            settlement.IsActive &&
            string.Equals(settlement.Id, polity.PrimarySettlementId, StringComparison.Ordinal))
            ?? polity.Settlements.FirstOrDefault(settlement => settlement.IsActive);
        polity.PrimarySettlementId = primary?.Id ?? string.Empty;
        foreach (var settlement in polity.Settlements)
        {
            settlement.IsPrimary = primary is not null && string.Equals(settlement.Id, primary.Id, StringComparison.Ordinal);
        }
    }

    private static PolityIntegrityBand ResolveIntegrityBand(int score, PoliticalScaleForm currentForm)
    {
        if (score >= PolityConditionConstants.CoherentIntegrityThreshold)
        {
            return PolityIntegrityBand.Coherent;
        }

        if (score >= PolityConditionConstants.StrainedIntegrityThreshold)
        {
            return PolityIntegrityBand.Strained;
        }

        if (score >= PolityConditionConstants.UnstableIntegrityThreshold)
        {
            return PolityIntegrityBand.Unstable;
        }

        if (currentForm is PoliticalScaleForm.EmpireLike or PoliticalScaleForm.CompositeRealm or PoliticalScaleForm.KingdomRealm &&
            score >= PolityConditionConstants.UnstableIntegrityThreshold - PolityConditionConstants.IntegrityRetentionBuffer)
        {
            return PolityIntegrityBand.Unstable;
        }

        return PolityIntegrityBand.NearCollapse;
    }

    private static GovernanceConditionBand ResolveGovernanceBand(int score, int previousGovernability)
    {
        if (score >= PolityConditionConstants.FunctionalGovernanceThreshold)
        {
            return GovernanceConditionBand.Functional;
        }

        if (score >= PolityConditionConstants.StrainedGovernanceThreshold)
        {
            return GovernanceConditionBand.Strained;
        }

        if (score >= PolityConditionConstants.FailingGovernanceThreshold)
        {
            return GovernanceConditionBand.Failing;
        }

        if (previousGovernability >= PolityConditionConstants.FailingGovernanceThreshold &&
            score >= PolityConditionConstants.FailingGovernanceThreshold - PolityConditionConstants.GovernanceRetentionBuffer)
        {
            return GovernanceConditionBand.Failing;
        }

        return GovernanceConditionBand.Collapsing;
    }

    private static int ResolveSeverityPenalty(PolityConditionSeverity severity, int strained, int critical, int collapse)
    {
        return severity switch
        {
            PolityConditionSeverity.Collapse => collapse,
            PolityConditionSeverity.Critical => critical,
            PolityConditionSeverity.Strained => strained,
            _ => 0
        };
    }

    private static PolityConditionSeverity ClassifyPressure(int effectiveValue, int displayValue)
    {
        var value = Math.Max(Math.Abs(effectiveValue), displayValue);
        return value switch
        {
            >= PolityConditionConstants.CollapsePressureThreshold => PolityConditionSeverity.Collapse,
            >= PolityConditionConstants.CriticalPressureThreshold => PolityConditionSeverity.Critical,
            >= PolityConditionConstants.StrainedPressureThreshold => PolityConditionSeverity.Strained,
            >= PolityConditionConstants.StablePressureThreshold + PolityConditionConstants.SeverityRetentionBuffer when displayValue >= PolityConditionConstants.StablePressureThreshold => PolityConditionSeverity.Strained,
            _ => PolityConditionSeverity.Stable
        };
    }

    private static PolityConditionSeverity MaxSeverity(params PolityConditionSeverity[] values)
    {
        return values.Max();
    }

    private static PolityConditionSeverity ClassifyFoodCondition(PolityContext context, out string reason)
    {
        var accounting = context.FoodAccounting;
        var demand = Math.Max(1, accounting.MonthlyDemand);
        var endingStores = accounting.EndingTotalStores;
        var reserveMonths = endingStores / (float)demand;
        var deficitRatio = accounting.UnresolvedDeficit / (float)demand;
        var pressure = Math.Max(context.Pressures.Food.EffectiveValue, context.Pressures.Food.DisplayValue);

        if (accounting.UnresolvedDeficit > 0 &&
            (accounting.FoodStressState == FoodStressState.Starvation ||
             accounting.ShortageMonths >= PolityConditionConstants.SustainedCollapseMonthsThreshold ||
             endingStores <= 0 ||
             pressure >= PolityConditionConstants.CollapsePressureThreshold))
        {
            reason = $"Food collapse is driven by unresolved deficit {accounting.UnresolvedDeficit}, empty or exhausted cover, and sustained food stress ({accounting.FoodStressState}).";
            return PolityConditionSeverity.Collapse;
        }

        if (accounting.UnresolvedDeficit > 0 ||
            accounting.FoodStressState == FoodStressState.SevereShortage ||
            accounting.ShortageMonths >= PolityConditionConstants.SustainedShortageMonthsThreshold ||
            pressure >= PolityConditionConstants.CriticalPressureThreshold)
        {
            reason = accounting.UnresolvedDeficit > 0
                ? $"Food remains critical because deficit {accounting.UnresolvedDeficit} is unresolved this month."
                : $"Food remains critical because repeated shortage and strain have pushed the polity into {accounting.FoodStressState}.";
            return PolityConditionSeverity.Critical;
        }

        if (pressure >= PolityConditionConstants.StrainedPressureThreshold ||
            reserveMonths < 1.0f ||
            accounting.ShortageMonths > 0 ||
            accounting.HungerPressure >= 0.25f ||
            accounting.NetFoodChange < 0 ||
            deficitRatio > 0.0f)
        {
            reason = accounting.NetFoodChange < 0 && reserveMonths >= 1.0f
                ? $"Food is strained because intake fell behind use this month despite stores still covering demand."
                : $"Food is strained because stores cover only {reserveMonths:0.0} month(s) and recent food stress has not fully cleared.";
            return PolityConditionSeverity.Strained;
        }

        reason = $"Food is stable because stores cover about {reserveMonths:0.0} month(s), net food changed by {accounting.NetFoodChange:+#;-#;0}, and no deficit remains.";
        return PolityConditionSeverity.Stable;
    }

    private static PolityConditionSeverity ClassifyMaterialFragility(PolityContext context, out string reason)
    {
        var materialSignal = context.MaterialProduction.DeficitScore + (context.MaterialShortageMonths * 8);
        var severity = ClassifyPressure(materialSignal, Math.Min(100, materialSignal));

        reason = severity switch
        {
            PolityConditionSeverity.Collapse => $"Living conditions are collapsing: deficit score {context.MaterialProduction.DeficitScore} with shortages sustained for {context.MaterialShortageMonths} month(s).",
            PolityConditionSeverity.Critical => $"Living conditions are failing: deficit score {context.MaterialProduction.DeficitScore} with shortages sustained for {context.MaterialShortageMonths} month(s).",
            PolityConditionSeverity.Strained => $"Living conditions are strained: deficit score {context.MaterialProduction.DeficitScore} and shelter, storage, and tool support are weakening.",
            _ => "Living conditions remain stable."
        };

        return severity;
    }

    private static string DescribeFoodIssue(FoodAccountingSnapshot accounting, PolityConditionSeverity severity)
    {
        if (accounting.UnresolvedDeficit > 0 && accounting.EndingTotalStores <= 0)
        {
            return "Food deficit is unresolved and stores are exhausted.";
        }

        if (accounting.UnresolvedDeficit > 0)
        {
            return accounting.EndingTotalStores > 0
                ? "Food intake is failing, though stored food is still covering immediate demand."
                : "Food deficit is unresolved this month.";
        }

        if (accounting.ShortageMonths >= PolityConditionConstants.SustainedShortageMonthsThreshold)
        {
            return "Repeated food shortage is keeping food conditions unstable.";
        }

        if (severity >= PolityConditionSeverity.Critical)
        {
            return "Food stores are low and shortage risk is rising.";
        }

        return "Food strain is rising.";
    }

    private static string DescribeFoodProblem(MaterialSurvivalAssessment material, FoodAccountingSnapshot accounting)
    {
        if (accounting.UnresolvedDeficit > 0)
        {
            return accounting.EndingTotalStores > 0
                ? "Food deficit remains unresolved despite stored cover."
                : "Food deficit is unresolved.";
        }

        if (accounting.ShortageMonths >= PolityConditionConstants.SustainedShortageMonthsThreshold)
        {
            return "Repeated food shortage is eroding food stability.";
        }

        return $"Food state is {material.FoodCondition.ToString().ToLowerInvariant()}.";
    }

    private static string DescribeMaterialFragilityIssue(MaterialSurvivalAssessment material, PolityContext context)
    {
        return context.MaterialShortageMonths >= PolityConditionConstants.SustainedShortageMonthsThreshold
            ? "Living conditions are worsening and pulling wider stability down."
            : "Shelter, storage, and tool weakness are dragging down living conditions.";
    }

    private static string DescribeMaterialFragilityProblem(MaterialSurvivalAssessment material, PolityContext? context)
    {
        if (context is not null && context.MaterialShortageMonths >= PolityConditionConstants.SustainedShortageMonthsThreshold)
        {
            return "Persistent material shortages are worsening living conditions.";
        }

        return $"Living conditions are {DescribeLivingConditions(material.MaterialFragilityCondition)}.";
    }

    private static string DescribeLivingConditions(PolityConditionSeverity severity)
    {
        return severity switch
        {
            PolityConditionSeverity.Stable => "stable",
            PolityConditionSeverity.Strained => "strained",
            PolityConditionSeverity.Critical => "failing",
            PolityConditionSeverity.Collapse => "collapsing",
            _ => "unclear"
        };
    }

    private static string DescribeGovernmentForm(GovernmentForm form)
    {
        return form switch
        {
            GovernmentForm.TribalClanRule => "tribal or clan rule",
            GovernmentForm.Confederation => "confederation",
            GovernmentForm.CouncilRule => "council rule",
            GovernmentForm.MerchantRule => "merchant rule",
            GovernmentForm.FeudalRule => "feudal rule",
            GovernmentForm.ImperialBureaucracy => "imperial bureaucracy",
            GovernmentForm.Republic => "republic",
            GovernmentForm.AbsoluteRule => "absolute rule",
            GovernmentForm.DespoticRule => "despotic rule",
            GovernmentForm.Theocracy => "theocracy",
            _ => "polity"
        };
    }

    private static string DescribeScaleForm(PoliticalScaleForm form)
    {
        return form switch
        {
            PoliticalScaleForm.LocalPolity => "local polity",
            PoliticalScaleForm.RegionalState => "regional state",
            PoliticalScaleForm.KingdomRealm => "kingdom-like realm",
            PoliticalScaleForm.CompositeRealm => "composite realm",
            PoliticalScaleForm.EmpireLike => "empire-like state",
            PoliticalScaleForm.Fragmenting => "fragmenting realm",
            _ => "polity"
        };
    }
}
