using Species.Client.ViewModelFactories;

namespace Species.Client.InputHandling;

public static class KnownPolitiesInputHandler
{
    public static PlayerInputResult HandleKey(ConsoleKeyInfo key, PlayerInputContext context)
    {
        var polityCount = KnownPolitiesViewModelFactory.GetKnownPolityCount(context.CurrentWorld, context.ViewState.FocalPolityId);

        if (polityCount == 0)
        {
            return PlayerInputResult.None;
        }

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                context.ViewState.MoveKnownPolitySelection(polityCount, -1);
                return PlayerInputResult.Render;
            case ConsoleKey.DownArrow:
                context.ViewState.MoveKnownPolitySelection(polityCount, 1);
                return PlayerInputResult.Render;
            default:
                return PlayerInputResult.None;
        }
    }
}
