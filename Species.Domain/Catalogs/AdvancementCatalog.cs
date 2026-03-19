using Species.Domain.Enums;
using Species.Domain.Models;

namespace Species.Domain.Catalogs;

public sealed class AdvancementCatalog
{
    public const string ImprovedGatheringId = "advancement-improved-gathering";
    public const string ImprovedHuntingId = "advancement-improved-hunting";
    public const string FoodStorageId = "advancement-food-storage";
    public const string OrganizedTravelId = "advancement-organized-travel";
    public const string LocalResourceUseId = "advancement-local-resource-use";

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
                Id = ImprovedGatheringId,
                Name = "Improved Gathering",
                Description = "Practical methods that make gathering more productive.",
                Category = AdvancementCategory.Gathering,
                PracticalEffectSummary = "Directly increases food gained when gathering."
            },
            new AdvancementDefinition
            {
                Id = ImprovedHuntingId,
                Name = "Improved Hunting",
                Description = "Practical methods that make hunting more productive.",
                Category = AdvancementCategory.Hunting,
                PracticalEffectSummary = "Directly increases food gained when hunting."
            },
            new AdvancementDefinition
            {
                Id = FoodStorageId,
                Name = "Food Storage",
                Description = "Practical storage habits that preserve more carried food value.",
                Category = AdvancementCategory.Storage,
                PracticalEffectSummary = "Directly improves the effective value of StoredFood when it is used."
            },
            new AdvancementDefinition
            {
                Id = OrganizedTravelId,
                Name = "Organized Travel",
                Description = "Practical travel organization that makes route use more reliable.",
                Category = AdvancementCategory.Travel,
                PracticalEffectSummary = "Directly improves migration execution on known routes."
            },
            new AdvancementDefinition
            {
                Id = LocalResourceUseId,
                Name = "Local Resource Use",
                Description = "Practical use of familiar regional resources.",
                Category = AdvancementCategory.ResourceUse,
                PracticalEffectSummary = "Directly improves gathering and hunting in regions the group understands well."
            }
        ]);
    }
}
