using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Domain.Simulation;

namespace Species.Domain.Validation;

public static class AdvancementStateValidator
{
    public static IReadOnlyList<string> Validate(World world, AdvancementCatalog advancementCatalog, SimulationTickResult tickResult)
    {
        var errors = new List<string>();
        var validAdvancementIds = advancementCatalog.Definitions.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var group in world.PopulationGroups)
        {
            if (group.AdvancementEvidence is null)
            {
                errors.Add($"Population group {group.Id} has null AdvancementEvidence.");
                continue;
            }

            foreach (var advancementId in group.LearnedAdvancementIds)
            {
                if (!validAdvancementIds.Contains(advancementId))
                {
                    errors.Add($"Population group {group.Id} references invalid advancement ID {advancementId}.");
                }
            }
        }

        foreach (var change in tickResult.AdvancementChanges)
        {
            if (string.IsNullOrWhiteSpace(change.GroupId))
            {
                errors.Add("An advancement change is missing a group ID.");
            }
        }

        return errors;
    }
}
