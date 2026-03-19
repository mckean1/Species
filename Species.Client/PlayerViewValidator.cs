using Species.Domain.Catalogs;
using Species.Domain.Models;

public static class PlayerViewValidator
{
    public static IReadOnlyList<string> Validate(PlayerViewState viewState, World world, AdvancementCatalog advancementCatalog)
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(viewState.CurrentScreen))
        {
            errors.Add("Current player screen is invalid.");
        }

        if (viewState.CurrentScreen != PlayerScreen.Chronicle &&
            viewState.CurrentScreen != PlayerScreen.Polity &&
            viewState.CurrentScreen != PlayerScreen.Advancements &&
            viewState.CurrentScreen != PlayerScreen.Laws &&
            viewState.CurrentScreen != PlayerScreen.Regions &&
            viewState.CurrentScreen != PlayerScreen.KnownPolities &&
            viewState.CurrentScreen != PlayerScreen.KnownSpecies)
        {
            errors.Add("Current player screen is not part of the MVP screen set.");
        }

        if (world.Regions.Count == 0 && viewState.CurrentScreen == PlayerScreen.Regions)
        {
            errors.Add("Regions cannot be active without regions.");
        }

        if (world.Regions.Count > 0 && (viewState.CurrentRegionIndex < 0 || viewState.CurrentRegionIndex >= world.Regions.Count))
        {
            errors.Add("Regions points at an invalid region.");
        }

        if (world.PopulationGroups.Count > 0 &&
            (viewState.CurrentKnownPolityIndex < 0 || viewState.CurrentKnownPolityIndex >= world.PopulationGroups.Count))
        {
            errors.Add("Known Polities points at an invalid polity.");
        }

        if (advancementCatalog.Definitions.Count > 0 &&
            (viewState.CurrentAdvancementIndex < 0 || viewState.CurrentAdvancementIndex >= advancementCatalog.Definitions.Count))
        {
            errors.Add("Advancements points at an invalid advancement.");
        }

        var lawCount = LawsScreenDataBuilder.Build(world, viewState.CurrentLawIndex).Laws.Count;
        if (lawCount > 0 &&
            (viewState.CurrentLawIndex < 0 || viewState.CurrentLawIndex >= lawCount))
        {
            errors.Add("Laws points at an invalid law.");
        }

        var knownSpeciesCount = KnownSpeciesScreenDataBuilder.Build(world, viewState.CurrentKnownSpeciesIndex).Species.Count;
        if (knownSpeciesCount > 0 &&
            (viewState.CurrentKnownSpeciesIndex < 0 || viewState.CurrentKnownSpeciesIndex >= knownSpeciesCount))
        {
            errors.Add("Known Species points at an invalid species.");
        }

        return errors;
    }
}
