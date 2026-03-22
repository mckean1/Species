using Species.Domain.Catalogs;
using Species.Domain.Models;
using Species.Domain.Simulation;
using Species.Client.Enums;
using Species.Client.ViewModelFactories;

namespace Species.Client.Presentation;

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

        if (!Enum.IsDefined(viewState.CurrentChronicleMode))
        {
            errors.Add("Current Chronicle mode is invalid.");
        }

        if (!Enum.IsDefined(viewState.CurrentChronicleSelectionArea))
        {
            errors.Add("Current Chronicle selection area is invalid.");
        }

        if (viewState.CurrentScreen != PlayerScreen.Chronicle &&
            viewState.CurrentScreen != PlayerScreen.Polity &&
            viewState.CurrentScreen != PlayerScreen.Government &&
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
        var regionCount = RegionsViewModelFactory.GetKnownRegionCount(world, focalPolityId);
        if (viewState.CurrentRegionIndex < 0 || (regionCount > 0 && viewState.CurrentRegionIndex >= regionCount))
        {
            errors.Add("Regions points at an invalid region.");
        }

        var polityCount = KnownPolitiesViewModelFactory.GetKnownPolityCount(world, focalPolityId);
        if (viewState.CurrentKnownPolityIndex < 0 ||
            (polityCount > 0 && viewState.CurrentKnownPolityIndex >= polityCount))
        {
            errors.Add("Known Polities points at an invalid polity.");
        }

        var advancementCount = AdvancementViewModelFactory.GetAdvancementCount(advancementCatalog);
        if (viewState.CurrentAdvancementIndex < 0 ||
            (advancementCount > 0 && viewState.CurrentAdvancementIndex >= advancementCount))
        {
            errors.Add("Advancements points at an invalid advancement.");
        }

        var lawSelection = LawsViewModelFactory.QuerySelectionInfo(world, focalPolityId, viewState.CurrentLawIndex);
        var lawCount = lawSelection.LawCount;
        if (viewState.CurrentLawIndex < 0 ||
            (lawCount > 0 && viewState.CurrentLawIndex >= lawCount))
        {
            errors.Add("Laws points at an invalid law.");
        }

        var knownSpeciesCount = KnownSpeciesViewModelFactory.GetKnownSpeciesCount(world, faunaCatalog, focalPolityId);
        if (viewState.CurrentKnownSpeciesIndex < 0 ||
            (knownSpeciesCount > 0 && viewState.CurrentKnownSpeciesIndex >= knownSpeciesCount))
        {
            errors.Add("Known Species points at an invalid species.");
        }

        var chronicleRequest = viewState.CreateChronicleViewRequest();
        var chronicleSelection = ChronicleViewModelFactory.QuerySelectionInfo(world, focalPolityId, chronicleRequest);
        var chronicleData = ChronicleViewModelFactory.Build(world, focalPolityId, chronicleRequest);
        if (viewState.CurrentChronicleUrgentIndex < 0 ||
            (chronicleSelection.UrgentCount > 0 && viewState.CurrentChronicleUrgentIndex >= chronicleSelection.UrgentCount))
        {
            errors.Add("Chronicle urgent selection points at an invalid item.");
        }

        var chronicleEntryIndex = viewState.CurrentChronicleMode switch
        {
            ChronicleMode.Live => viewState.CurrentChronicleLiveIndex,
            ChronicleMode.Archive => viewState.CurrentChronicleArchiveIndex,
            _ => viewState.CurrentChronicleMilestoneIndex
        };
        if (chronicleEntryIndex < 0 ||
            (chronicleSelection.EntryCount > 0 && chronicleEntryIndex >= chronicleSelection.EntryCount))
        {
            errors.Add("Chronicle entry selection points at an invalid item.");
        }

        if (viewState.IsLawActionMenuOpen &&
            (viewState.CurrentScreen != PlayerScreen.Laws || viewState.CurrentLawActionIndex < 0 || viewState.CurrentLawActionIndex > 1))
        {
            errors.Add("Law action selection is invalid.");
        }

        foreach (var urgent in chronicleData.UrgentItems)
        {
            if (urgent.TargetScreen == PlayerScreen.Laws &&
                focalPolity?.ActiveLawProposal is not null &&
                !string.IsNullOrWhiteSpace(urgent.TargetId) &&
                !string.Equals(urgent.TargetId, focalPolity.ActiveLawProposal.Id, StringComparison.Ordinal))
            {
                errors.Add("Chronicle urgent law reference does not match the active proposal.");
            }
        }

        if (focalPolity is not null && focalContext is null)
        {
            errors.Add($"Player view focal polity {focalPolity.Id} cannot build an aggregate polity context.");
        }

        return errors;
    }
}
