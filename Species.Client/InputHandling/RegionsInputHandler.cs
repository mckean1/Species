using Species.Client.ViewModelFactories;

namespace Species.Client.InputHandling;

public static class RegionsInputHandler
{
    public static PlayerInputResult HandleKey(ConsoleKeyInfo key, PlayerInputContext context)
    {
        var regionCount = RegionsViewModelFactory.GetKnownRegionCount(context.CurrentWorld, context.ViewState.FocalPolityId);

        if (regionCount == 0)
        {
            return PlayerInputResult.None;
        }

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                context.ViewState.MoveRegionSelection(regionCount, -1);
                return PlayerInputResult.Render;
            case ConsoleKey.DownArrow:
                context.ViewState.MoveRegionSelection(regionCount, 1);
                return PlayerInputResult.Render;
            default:
                return PlayerInputResult.None;
        }
    }
}
