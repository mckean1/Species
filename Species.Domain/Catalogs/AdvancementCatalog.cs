using Species.Domain.Constants;
using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Catalogs;

public sealed class AdvancementCatalog
{
    public const string ForagingId = "advancement-foraging";
    public const string SmallGameHuntingId = "advancement-small-game-hunting";
    public const string LargeGameHuntingId = "advancement-large-game-hunting";
    public const string FishingId = "advancement-fishing";
    public const string TrappingId = "advancement-trapping";
    public const string FoodDryingId = "advancement-food-drying";
    public const string FoodStorageId = "advancement-food-storage";
    public const string StoneToolmakingId = "advancement-stone-toolmaking";
    public const string HideWorkingId = "advancement-hide-working";
    public const string FiberWorkingId = "advancement-fiber-working";

    private readonly Dictionary<string, AdvancementDefinition> definitionsById;

    public AdvancementCatalog(IReadOnlyList<AdvancementDefinition> definitions)
    {
        Definitions = definitions;
        definitionsById = definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);
    }

    public IReadOnlyList<AdvancementDefinition> Definitions { get; }

    public AdvancementDefinition? GetById(string id)
    {
        return definitionsById.GetValueOrDefault(id);
    }

    public static AdvancementCatalog CreateStarterSet()
    {
        return new AdvancementCatalog(
        [
            new AdvancementDefinition
            {
                Id = ForagingId,
                Name = "Foraging",
                Description = "Learned practical gathering of discovered edible flora.",
                Category = AdvancementCategory.FoodSurvival,
                PracticalEffectSummary = "Improves deliberate gathering from discovered edible flora.",
                PrerequisiteSummary = "Requires discovered edible flora, current access, food pressure, and repeated flora opportunity.",
                RequiredFloraTags = [FloraTag.Edible],
                RequiresCurrentAccess = true,
                RequiresPressureOrIncentive = true,
                OpportunityMonthsRequired = AdvancementConstants.ForagingOpportunityMonthsRequired
            },
            new AdvancementDefinition
            {
                Id = SmallGameHuntingId,
                Name = "Small Game Hunting",
                Description = "Learned practical hunting of discovered small prey.",
                Category = AdvancementCategory.FoodSurvival,
                PracticalEffectSummary = "Improves hunting against discovered small prey opportunities.",
                PrerequisiteSummary = "Requires discovered small prey, current access, food pressure, and repeated prey opportunity.",
                RequiredFaunaTags = [FaunaTag.SmallPrey],
                RequiresCurrentAccess = true,
                RequiresPressureOrIncentive = true,
                OpportunityMonthsRequired = AdvancementConstants.SmallGameHuntingOpportunityMonthsRequired
            },
            new AdvancementDefinition
            {
                Id = LargeGameHuntingId,
                Name = "Large Game Hunting",
                Description = "Learned coordinated hunting of discovered large prey.",
                Category = AdvancementCategory.FoodSurvival,
                PracticalEffectSummary = "Improves hunting when large prey opportunities can be organized and sustained.",
                PrerequisiteSummary = "Requires Small Game Hunting first, discovered large prey, access, organizational viability, and repeated opportunity.",
                RequiredAdvancementIds = [SmallGameHuntingId],
                RequiredFaunaTags = [FaunaTag.LargePrey],
                RequiresCurrentAccess = true,
                RequiresPressureOrIncentive = true,
                RequiresContinuity = true,
                OpportunityMonthsRequired = AdvancementConstants.LargeGameHuntingOpportunityMonthsRequired
            },
            new AdvancementDefinition
            {
                Id = FishingId,
                Name = "Fishing",
                Description = "Learned acquisition of discovered aquatic food where water access makes it practical.",
                Category = AdvancementCategory.FoodSurvival,
                PracticalEffectSummary = "Improves food acquisition from aquatic fauna where water reality allows it.",
                PrerequisiteSummary = "Requires discovered aquatic edible fauna, real water access, and repeated aquatic opportunity.",
                RequiredFaunaTags = [FaunaTag.AquaticFood],
                RequiresCurrentAccess = true,
                RequiresPressureOrIncentive = true,
                OpportunityMonthsRequired = AdvancementConstants.FishingOpportunityMonthsRequired
            },
            new AdvancementDefinition
            {
                Id = TrappingId,
                Name = "Trapping",
                Description = "Learned capture methods for small prey in repeated local conditions.",
                Category = AdvancementCategory.FoodSurvival,
                PracticalEffectSummary = "Improves small-prey acquisition where repeated trapping opportunity exists.",
                PrerequisiteSummary = "Requires Small Game Hunting first, discovered small prey, and repeated small-prey opportunity.",
                RequiredAdvancementIds = [SmallGameHuntingId],
                RequiredFaunaTags = [FaunaTag.SmallPrey],
                RequiresCurrentAccess = true,
                OpportunityMonthsRequired = AdvancementConstants.TrappingOpportunityMonthsRequired
            },
            new AdvancementDefinition
            {
                Id = FoodDryingId,
                Name = "Food Drying",
                Description = "Learned preservation through drying when repeated surplus and scarcity make it worthwhile.",
                Category = AdvancementCategory.FoodPreservation,
                PracticalEffectSummary = "Improves the survival value of carried food before formal storage is established.",
                PrerequisiteSummary = "Requires discovered food-producing species, a learned food-acquisition method, spoilage pressure, and repeated surplus opportunity.",
                RequiresCurrentAccess = true,
                RequiresPressureOrIncentive = true,
                OpportunityMonthsRequired = AdvancementConstants.FoodDryingOpportunityMonthsRequired
            },
            new AdvancementDefinition
            {
                Id = FoodStorageId,
                Name = "Food Storage",
                Description = "Learned durable storage built on preservation practice and anchored continuity.",
                Category = AdvancementCategory.FoodPreservation,
                PracticalEffectSummary = "Improves the effective value of carried and settlement food reserves.",
                PrerequisiteSummary = "Requires Food Drying, repeated surplus under storage-feasible conditions, and enough continuity to make storage real.",
                RequiredAdvancementIds = [FoodDryingId],
                RequiresCurrentAccess = true,
                RequiresContinuity = true,
                OpportunityMonthsRequired = AdvancementConstants.FoodStorageOpportunityMonthsRequired
            },
            new AdvancementDefinition
            {
                Id = StoneToolmakingId,
                Name = "Stone Toolmaking",
                Description = "Learned shaping and use of discovered toolmaking stone.",
                Category = AdvancementCategory.MaterialsCraft,
                PracticalEffectSummary = "Improves food processing and material extraction through practical stone tools.",
                PrerequisiteSummary = "Requires discovered tool stone, current access, and repeated food or material processing need.",
                RequiredDiscoveryIds = [DiscoveryCatalog.ToolStoneId],
                RequiredResourceTags = [ResourceTag.ToolStone],
                RequiresCurrentAccess = true,
                RequiresPressureOrIncentive = true,
                OpportunityMonthsRequired = AdvancementConstants.StoneToolmakingOpportunityMonthsRequired
            },
            new AdvancementDefinition
            {
                Id = HideWorkingId,
                Name = "Hide Working",
                Description = "Learned working of hides from acquired fauna.",
                Category = AdvancementCategory.MaterialsCraft,
                PracticalEffectSummary = "Improves practical use of hide-producing fauna for textile and shelter support.",
                PrerequisiteSummary = "Requires discovered hide-producing fauna, a learned fauna-acquisition method, repeated hide access, and cold or material need.",
                RequiredFaunaTags = [FaunaTag.HideSource],
                RequiresCurrentAccess = true,
                RequiresPressureOrIncentive = true,
                OpportunityMonthsRequired = AdvancementConstants.HideWorkingOpportunityMonthsRequired
            },
            new AdvancementDefinition
            {
                Id = FiberWorkingId,
                Name = "Fiber Working",
                Description = "Learned working of fiber-producing flora into practical material.",
                Category = AdvancementCategory.MaterialsCraft,
                PracticalEffectSummary = "Improves practical use of fiber flora for textile and shelter support.",
                PrerequisiteSummary = "Requires discovered fiber flora, current access, and repeated fiber opportunity under material need.",
                RequiredFloraTags = [FloraTag.FiberSource],
                RequiresCurrentAccess = true,
                RequiresPressureOrIncentive = true,
                OpportunityMonthsRequired = AdvancementConstants.FiberWorkingOpportunityMonthsRequired
            }
        ]);
    }
}
