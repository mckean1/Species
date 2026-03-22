using Species.Client.ViewModelFactories;

namespace Species.Client.InputHandling;

public static class KnownPolitiesInputHandler
{
    public static PlayerInputResult HandleKey(ConsoleKeyInfo key, PlayerInputContext context)
    {
        var polityCount = KnownPolitiesViewModelFactory.Build(
            context.CurrentWorld,
            context.ViewState.FocalPolityId,
            context.ViewState.CurrentKnownPolityIndex,
            context.DiscoveryCatalog,
            context.AdvancementCatalog).Polities.Count;

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
