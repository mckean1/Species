using Species.Domain.Catalogs;
using Species.Domain.Constants;
using Species.Domain.Discovery;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Simulation;

public static class FirstWaveAdvancementEvaluator
{
    public sealed record AdvancementEligibility(
        bool PrerequisitesMet,
        int OpportunityCount,
        int RequiredOpportunityCount,
        float NeedFactor,
        string LockReason,
        string StatusSummary,
        string ListHint,
        IReadOnlyList<string> Requirements);

    public static AdvancementEligibility Evaluate(
        AdvancementDefinition definition,
        PopulationGroup group,
        Polity? polity,
        PolityContext? polityContext,
        Region? region,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        if (region is null)
        {
            return new AdvancementEligibility(false, 0, definition.OpportunityMonthsRequired, 0.0f, "Current region is unavailable.", "No current region.", "no region", ["Current region could not be resolved."]);
        }

        return definition.Id switch
        {
            var id when id == AdvancementCatalog.ForagingId => EvaluateForaging(group, polity, region, floraCatalog),
            var id when id == AdvancementCatalog.SmallGameHuntingId => EvaluateSmallGame(group, polity, region, faunaCatalog),
            var id when id == AdvancementCatalog.LargeGameHuntingId => EvaluateLargeGame(group, polity, polityContext, region, faunaCatalog),
            var id when id == AdvancementCatalog.FishingId => EvaluateFishing(group, polity, region, faunaCatalog),
            var id when id == AdvancementCatalog.TrappingId => EvaluateTrapping(group, polity, region, faunaCatalog),
            var id when id == AdvancementCatalog.FoodDryingId => EvaluateFoodDrying(group, polity, region, floraCatalog, faunaCatalog),
            var id when id == AdvancementCatalog.FoodStorageId => EvaluateFoodStorage(group, polityContext),
            var id when id == AdvancementCatalog.StoneToolmakingId => EvaluateStoneToolmaking(group, region),
            var id when id == AdvancementCatalog.HideWorkingId => EvaluateHideWorking(group, polity, polityContext, region, faunaCatalog),
            var id when id == AdvancementCatalog.FiberWorkingId => EvaluateFiberWorking(group, polity, polityContext, region, floraCatalog),
            _ => new AdvancementEligibility(false, 0, definition.OpportunityMonthsRequired, 0.0f, "Unsupported advancement definition.", "No rules available.", "unknown", ["This advancement has no current evaluator."])
        };
    }

    private static AdvancementEligibility EvaluateForaging(PopulationGroup group, Polity? polity, Region region, FloraSpeciesCatalog floraCatalog)
    {
        var discoveredEdible = HasDiscoveredFloraTag(polity, region, floraCatalog, FloraTag.Edible);
        var requirements = new[]
        {
            $"{Describe(discoveredEdible)} discovered edible flora in the current region.",
            $"{Describe(group.AdvancementEvidence.FoodPressureMonths > 0)} food pressure or scarcity incentive.",
            $"Repeated flora opportunity: {group.AdvancementEvidence.ForagingOpportunityMonths}/{AdvancementConstants.ForagingOpportunityMonthsRequired}."
        };

        return Build(
            discoveredEdible,
            group.AdvancementEvidence.ForagingOpportunityMonths,
            AdvancementConstants.ForagingOpportunityMonthsRequired,
            ResolveFoodNeed(group),
            requirements,
            discoveredEdible ? "edible flora is discovered and reachable" : "needs discovered edible flora");
    }

    private static AdvancementEligibility EvaluateSmallGame(PopulationGroup group, Polity? polity, Region region, FaunaSpeciesCatalog faunaCatalog)
    {
        var discoveredSmallPrey = HasDiscoveredFaunaTag(polity, region, faunaCatalog, FaunaTag.SmallPrey);
        var requirements = new[]
        {
            $"{Describe(discoveredSmallPrey)} discovered small prey in the current region.",
            $"{Describe(group.AdvancementEvidence.FoodPressureMonths > 0)} food pressure or scarcity incentive.",
            $"Repeated small-prey opportunity: {group.AdvancementEvidence.SmallPreyOpportunityMonths}/{AdvancementConstants.SmallGameHuntingOpportunityMonthsRequired}."
        };

        return Build(
            discoveredSmallPrey,
            group.AdvancementEvidence.SmallPreyOpportunityMonths,
            AdvancementConstants.SmallGameHuntingOpportunityMonthsRequired,
            ResolveFoodNeed(group),
            requirements,
            discoveredSmallPrey ? "small prey is discovered and reachable" : "needs discovered small prey");
    }

    private static AdvancementEligibility EvaluateLargeGame(PopulationGroup group, Polity? polity, PolityContext? polityContext, Region region, FaunaSpeciesCatalog faunaCatalog)
    {
        var discoveredLargePrey = HasDiscoveredFaunaTag(polity, region, faunaCatalog, FaunaTag.LargePrey);
        var hasSmallGame = group.LearnedAdvancementIds.Contains(AdvancementCatalog.SmallGameHuntingId);
        var organized = group.AdvancementEvidence.OrganizationalReadinessMonths > 0 &&
                        (group.Population >= 35 || polityContext?.AnchoringKind is not null and not PolityAnchoringKind.Mobile);
        var prereq = discoveredLargePrey && hasSmallGame && organized;
        var requirements = new[]
        {
            $"{Describe(discoveredLargePrey)} discovered large prey in the current region.",
            $"{Describe(hasSmallGame)} learned Small Game Hunting.",
            $"{Describe(organized)} enough organizational viability to sustain large-prey effort.",
            $"Repeated large-prey opportunity: {group.AdvancementEvidence.LargePreyOpportunityMonths}/{AdvancementConstants.LargeGameHuntingOpportunityMonthsRequired}."
        };

        return Build(
            prereq,
            group.AdvancementEvidence.LargePreyOpportunityMonths,
            AdvancementConstants.LargeGameHuntingOpportunityMonthsRequired,
            ResolveFoodNeed(group) + 0.10f,
            requirements,
            prereq ? "large prey is reachable and organized effort is possible" : "needs large prey, small-game practice, and organization");
    }

    private static AdvancementEligibility EvaluateFishing(PopulationGroup group, Polity? polity, Region region, FaunaSpeciesCatalog faunaCatalog)
    {
        var waterAccess = region.WaterAvailability is WaterAvailability.Medium or WaterAvailability.High;
        var aquaticAccess = waterAccess && HasDiscoveredFaunaTag(polity, region, faunaCatalog, FaunaTag.AquaticFood);
        var requirements = new[]
        {
            $"{Describe(aquaticAccess)} discovered aquatic food in usable water conditions.",
            $"{Describe(waterAccess)} real water access.",
            $"Repeated aquatic opportunity: {group.AdvancementEvidence.AquaticOpportunityMonths}/{AdvancementConstants.FishingOpportunityMonthsRequired}."
        };

        return Build(
            aquaticAccess && waterAccess,
            group.AdvancementEvidence.AquaticOpportunityMonths,
            AdvancementConstants.FishingOpportunityMonthsRequired,
            ResolveFoodNeed(group),
            requirements,
            aquaticAccess ? "aquatic food is discovered and reachable" : "needs discovered aquatic food and usable water");
    }

    private static AdvancementEligibility EvaluateTrapping(PopulationGroup group, Polity? polity, Region region, FaunaSpeciesCatalog faunaCatalog)
    {
        var discoveredSmallPrey = HasDiscoveredFaunaTag(polity, region, faunaCatalog, FaunaTag.SmallPrey);
        var hasSmallGame = group.LearnedAdvancementIds.Contains(AdvancementCatalog.SmallGameHuntingId);
        var prereq = discoveredSmallPrey && hasSmallGame;
        var requirements = new[]
        {
            $"{Describe(discoveredSmallPrey)} discovered small prey in the current region.",
            $"{Describe(hasSmallGame)} learned Small Game Hunting.",
            $"Repeated small-prey opportunity: {group.AdvancementEvidence.SmallPreyOpportunityMonths}/{AdvancementConstants.TrappingOpportunityMonthsRequired}."
        };

        return Build(
            prereq,
            group.AdvancementEvidence.SmallPreyOpportunityMonths,
            AdvancementConstants.TrappingOpportunityMonthsRequired,
            ResolveFoodNeed(group) * 0.9f,
            requirements,
            prereq ? "small prey is reachable and hunting basics are learned" : "needs discovered small prey and Small Game Hunting");
    }

    private static AdvancementEligibility EvaluateFoodDrying(
        PopulationGroup group,
        Polity? polity,
        Region region,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog)
    {
        var hasFoodSpecies = HasDiscoveredFloraTag(polity, region, floraCatalog, FloraTag.Edible) ||
                             HasDiscoveredFaunaTag(polity, region, faunaCatalog, FaunaTag.SmallPrey) ||
                             HasDiscoveredFaunaTag(polity, region, faunaCatalog, FaunaTag.LargePrey) ||
                             HasDiscoveredFaunaTag(polity, region, faunaCatalog, FaunaTag.AquaticFood);
        var hasAcquisitionMethod = group.LearnedAdvancementIds.Contains(AdvancementCatalog.ForagingId) ||
                                   group.LearnedAdvancementIds.Contains(AdvancementCatalog.SmallGameHuntingId) ||
                                   group.LearnedAdvancementIds.Contains(AdvancementCatalog.LargeGameHuntingId) ||
                                   group.LearnedAdvancementIds.Contains(AdvancementCatalog.FishingId) ||
                                   group.LearnedAdvancementIds.Contains(AdvancementCatalog.TrappingId);
        var prereq = hasFoodSpecies && hasAcquisitionMethod;
        var opportunity = Math.Min(group.AdvancementEvidence.SurplusOpportunityMonths, group.AdvancementEvidence.SpoilagePressureMonths);
        var requirements = new[]
        {
            $"{Describe(hasFoodSpecies)} discovered food-producing species.",
            $"{Describe(hasAcquisitionMethod)} learned food-acquisition capability.",
            $"{Describe(group.AdvancementEvidence.SpoilagePressureMonths > 0)} spoilage or scarcity-cycle pressure.",
            $"Repeated surplus opportunity: {opportunity}/{AdvancementConstants.FoodDryingOpportunityMonthsRequired}."
        };

        return Build(
            prereq,
            opportunity,
            AdvancementConstants.FoodDryingOpportunityMonthsRequired,
            ResolvePreservationNeed(group),
            requirements,
            prereq ? "food species and acquisition practice are in place" : "needs discovered food species and a learned acquisition method");
    }

    private static AdvancementEligibility EvaluateFoodStorage(PopulationGroup group, PolityContext? polityContext)
    {
        var hasFoodDrying = group.LearnedAdvancementIds.Contains(AdvancementCatalog.FoodDryingId);
        var hasContinuity = polityContext?.AnchoringKind is PolityAnchoringKind.SemiRooted or PolityAnchoringKind.Anchored;
        var opportunity = Math.Min(group.AdvancementEvidence.SurplusOpportunityMonths, group.AdvancementEvidence.AnchoredContinuityMonths);
        var requirements = new[]
        {
            $"{Describe(hasFoodDrying)} learned Food Drying.",
            $"{Describe(hasContinuity)} enough continuity or anchoring to make storage real.",
            $"Repeated surplus plus continuity: {opportunity}/{AdvancementConstants.FoodStorageOpportunityMonthsRequired}."
        };

        return Build(
            hasFoodDrying && hasContinuity,
            opportunity,
            AdvancementConstants.FoodStorageOpportunityMonthsRequired,
            ResolvePreservationNeed(group) + 0.10f,
            requirements,
            hasFoodDrying && hasContinuity ? "drying practice and continuity are in place" : "needs Food Drying and anchored continuity");
    }

    private static AdvancementEligibility EvaluateStoneToolmaking(PopulationGroup group, Region region)
    {
        var discoveredToolStone = group.KnownDiscoveryIds.Contains(DiscoveryCatalog.ToolStoneId);
        var stoneAccess = discoveredToolStone && MaterialResourceTagResolver.HasTag(MaterialResource.Stone, ResourceTag.ToolStone) && region.MaterialProfile.Opportunities.Stone > 0;
        var requirements = new[]
        {
            $"{Describe(discoveredToolStone)} discovered tool-grade stone.",
            $"{Describe(stoneAccess)} current access to tool stone.",
            $"{Describe(group.AdvancementEvidence.MaterialNeedMonths > 0)} food or material processing need.",
            $"Repeated stone access: {group.AdvancementEvidence.StoneAccessMonths}/{AdvancementConstants.StoneToolmakingOpportunityMonthsRequired}."
        };

        return Build(
            discoveredToolStone && stoneAccess,
            group.AdvancementEvidence.StoneAccessMonths,
            AdvancementConstants.StoneToolmakingOpportunityMonthsRequired,
            ResolveMaterialNeed(group),
            requirements,
            stoneAccess ? "tool stone is discovered and reachable" : "needs discovered tool stone");
    }

    private static AdvancementEligibility EvaluateHideWorking(PopulationGroup group, Polity? polity, PolityContext? polityContext, Region region, FaunaSpeciesCatalog faunaCatalog)
    {
        var discoveredHide = HasDiscoveredFaunaTag(polity, region, faunaCatalog, FaunaTag.HideSource);
        var hasAcquisitionMethod = group.LearnedAdvancementIds.Contains(AdvancementCatalog.SmallGameHuntingId) ||
                                   group.LearnedAdvancementIds.Contains(AdvancementCatalog.LargeGameHuntingId) ||
                                   group.LearnedAdvancementIds.Contains(AdvancementCatalog.TrappingId);
        var prereq = discoveredHide && hasAcquisitionMethod;
        var requirements = new[]
        {
            $"{Describe(discoveredHide)} discovered hide-producing fauna.",
            $"{Describe(hasAcquisitionMethod)} learned fauna-acquisition method.",
            $"{Describe(group.AdvancementEvidence.MaterialNeedMonths > 0 || IsColdBiome(region))} cold or material need.",
            $"Repeated hide access: {group.AdvancementEvidence.HideAccessMonths}/{AdvancementConstants.HideWorkingOpportunityMonthsRequired}."
        };

        return Build(
            prereq,
            group.AdvancementEvidence.HideAccessMonths,
            AdvancementConstants.HideWorkingOpportunityMonthsRequired,
            Math.Max(ResolveMaterialNeed(group), IsColdBiome(region) ? 0.25f : 0.0f),
            requirements,
            prereq ? "hide sources and acquisition practice are in place" : "needs discovered hide fauna and a learned acquisition method");
    }

    private static AdvancementEligibility EvaluateFiberWorking(PopulationGroup group, Polity? polity, PolityContext? polityContext, Region region, FloraSpeciesCatalog floraCatalog)
    {
        var discoveredFiber = HasDiscoveredFloraTag(polity, region, floraCatalog, FloraTag.FiberSource);
        var requirements = new[]
        {
            $"{Describe(discoveredFiber)} discovered fiber-producing flora.",
            $"{Describe(discoveredFiber)} current access to fiber flora.",
            $"{Describe(group.AdvancementEvidence.MaterialNeedMonths > 0 || (polityContext?.MaterialProduction.ShelterSupport ?? 100) < 45)} material or shelter need.",
            $"Repeated fiber opportunity: {group.AdvancementEvidence.FiberAccessMonths}/{AdvancementConstants.FiberWorkingOpportunityMonthsRequired}."
        };

        return Build(
            discoveredFiber,
            group.AdvancementEvidence.FiberAccessMonths,
            AdvancementConstants.FiberWorkingOpportunityMonthsRequired,
            ResolveMaterialNeed(group),
            requirements,
            discoveredFiber ? "fiber flora is discovered and reachable" : "needs discovered fiber flora");
    }

    private static AdvancementEligibility Build(
        bool prerequisitesMet,
        int opportunityCount,
        int requiredOpportunityCount,
        float needFactor,
        IReadOnlyList<string> requirements,
        string satisfiedStatus)
    {
        return new AdvancementEligibility(
            prerequisitesMet,
            opportunityCount,
            requiredOpportunityCount,
            Math.Clamp(needFactor, 0.0f, 1.25f),
            prerequisitesMet ? string.Empty : $"Waiting because {requirements.FirstOrDefault(item => item.StartsWith("Needs", StringComparison.OrdinalIgnoreCase)) ?? requirements.First()}",
            prerequisitesMet ? $"Ready to accumulate learning from {satisfiedStatus}." : "Waiting on causal prerequisites.",
            satisfiedStatus,
            requirements);
    }

    private static bool HasDiscoveredFloraTag(Polity? polity, Region region, FloraSpeciesCatalog floraCatalog, FloraTag tag)
    {
        return region.Ecosystem.FloraPopulations
            .Where(entry => entry.Value > 0)
            .Any(entry =>
            {
                var flora = floraCatalog.GetById(entry.Key);
                return flora is not null &&
                       flora.Tags.Contains(tag) &&
                       HasDiscoveredSpecies(polity, SpeciesClass.Flora, entry.Key);
            });
    }

    private static bool HasDiscoveredFaunaTag(Polity? polity, Region region, FaunaSpeciesCatalog faunaCatalog, FaunaTag tag)
    {
        return region.Ecosystem.FaunaPopulations
            .Where(entry => entry.Value > 0)
            .Any(entry =>
            {
                var fauna = faunaCatalog.GetById(entry.Key);
                return fauna is not null &&
                       fauna.Tags.Contains(tag) &&
                       HasDiscoveredSpecies(polity, SpeciesClass.Fauna, entry.Key);
            });
    }

    private static bool HasDiscoveredSpecies(Polity? polity, SpeciesClass speciesClass, string speciesId)
    {
        return polity?.SpeciesAwareness.Any(state =>
            state.SpeciesClass == speciesClass &&
            string.Equals(state.SpeciesId, speciesId, StringComparison.Ordinal) &&
            state.CurrentStage == DiscoveryStage.Discovered) == true;
    }

    private static float ResolveFoodNeed(PopulationGroup group)
    {
        return group.AdvancementEvidence.FoodPressureMonths > 0 || group.ShortageMonths > 0
            ? 0.95f
            : 0.45f;
    }

    private static float ResolvePreservationNeed(PopulationGroup group)
    {
        return group.AdvancementEvidence.SpoilagePressureMonths > 0
            ? 1.00f
            : group.AdvancementEvidence.SurplusOpportunityMonths > 0
                ? 0.65f
                : 0.30f;
    }

    private static float ResolveMaterialNeed(PopulationGroup group)
    {
        return group.AdvancementEvidence.MaterialNeedMonths > 0
            ? 0.95f
            : 0.40f;
    }

    private static bool IsColdBiome(Region region)
    {
        return region.Biome is Biome.Highlands or Biome.Tundra;
    }

    private static string Describe(bool satisfied)
    {
        return satisfied ? "Has" : "Needs";
    }
}
