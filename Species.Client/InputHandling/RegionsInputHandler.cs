namespace Species.Client.InputHandling;

public static class RegionsInputHandler
{
    public static PlayerInputResult HandleKey(ConsoleKeyInfo key, PlayerInputContext context)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                context.ViewState.MoveRegionSelection(context.CurrentWorld.Regions.Count, -1);
                return PlayerInputResult.Render;
            case ConsoleKey.DownArrow:
                context.ViewState.MoveRegionSelection(context.CurrentWorld.Regions.Count, 1);
                return PlayerInputResult.Render;
            default:
                return PlayerInputResult.None;
        }
    }
}
