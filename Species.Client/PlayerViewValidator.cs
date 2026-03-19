using Species.Domain.Models;

public static class PlayerViewValidator
{
    public static IReadOnlyList<string> Validate(PlayerViewState viewState, World world)
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(viewState.CurrentScreen))
        {
            errors.Add("Current player screen is invalid.");
        }

        if (viewState.CurrentScreen != PlayerScreen.Chronicle && viewState.CurrentScreen != PlayerScreen.RegionViewer)
        {
            errors.Add("Current player screen is not part of the MVP screen set.");
        }

        if (world.Regions.Count == 0 && viewState.CurrentScreen == PlayerScreen.RegionViewer)
        {
            errors.Add("Region Viewer cannot be active without regions.");
        }

        if (world.Regions.Count > 0 && (viewState.CurrentRegionIndex < 0 || viewState.CurrentRegionIndex >= world.Regions.Count))
        {
            errors.Add("Region Viewer points at an invalid region.");
        }

        return errors;
    }
}
