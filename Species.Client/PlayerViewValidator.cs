using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Domain.Simulation;

public static class PlayerViewValidator
{
    public static IReadOnlyList<string> Validate(
        PlayerViewState viewState,
        World world,
        FloraSpeciesCatalog floraCatalog,
        FaunaSpeciesCatalog faunaCatalog,
        DiscoveryCatalog discoveryCatalog,
        AdvancementCatalog advancementCatalog)
    {
        var errors = new List<string>();
        var focalPolity = PlayerFocus.Resolve(world, viewState.FocalPolityId);
        var focalContext = PlayerFocus.ResolveContext(world, viewState.FocalPolityId);

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

        if (world.Polities.Count > 0 && focalPolity is null)
        {
            errors.Add("Player view focal polity does not resolve to a current polity.");
        }

        if (world.Regions.Count == 0 && viewState.CurrentScreen == PlayerScreen.Regions)
        {
            errors.Add("Regions cannot be active without regions.");
        }

        var focalPolityId = focalPolity?.Id ?? string.Empty;
        var regionCount = RegionsScreenDataBuilder.Build(world, focalPolityId, viewState.CurrentRegionIndex, floraCatalog, faunaCatalog, discoveryCatalog).Regions.Count;
        if (viewState.CurrentRegionIndex < 0 || (regionCount > 0 && viewState.CurrentRegionIndex >= regionCount))
        {
            errors.Add("Regions points at an invalid region.");
        }

        var polityCount = KnownPolitiesScreenDataBuilder.Build(world, focalPolityId, viewState.CurrentKnownPolityIndex, discoveryCatalog, advancementCatalog).Polities.Count;
        if (viewState.CurrentKnownPolityIndex < 0 ||
            (polityCount > 0 && viewState.CurrentKnownPolityIndex >= polityCount))
        {
            errors.Add("Known Polities points at an invalid polity.");
        }

        var advancementCount = AdvancementsScreenDataBuilder.Build(world, focalPolityId, discoveryCatalog, advancementCatalog, viewState.CurrentAdvancementIndex).Items.Count;
        if (viewState.CurrentAdvancementIndex < 0 ||
            (advancementCount > 0 && viewState.CurrentAdvancementIndex >= advancementCount))
        {
            errors.Add("Advancements points at an invalid advancement.");
        }

        var lawCount = LawsScreenDataBuilder.Build(world, focalPolityId, viewState.CurrentLawIndex).Laws.Count;
        if (viewState.CurrentLawIndex < 0 ||
            (lawCount > 0 && viewState.CurrentLawIndex >= lawCount))
        {
            errors.Add("Laws points at an invalid law.");
        }

        var knownSpeciesCount = KnownSpeciesScreenDataBuilder.Build(world, faunaCatalog, focalPolityId, viewState.CurrentKnownSpeciesIndex).Species.Count;
        if (viewState.CurrentKnownSpeciesIndex < 0 ||
            (knownSpeciesCount > 0 && viewState.CurrentKnownSpeciesIndex >= knownSpeciesCount))
        {
            errors.Add("Known Species points at an invalid species.");
        }

        if (focalPolity is not null && focalContext is null)
        {
            errors.Add($"Player view focal polity {focalPolity.Id} cannot build an aggregate polity context.");
        }

        return errors;
    }
}
