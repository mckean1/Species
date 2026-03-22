using Species.Client.ViewModelFactories;

namespace Species.Client.InputHandling;

public static class KnownSpeciesInputHandler
{
    public static PlayerInputResult HandleKey(ConsoleKeyInfo key, PlayerInputContext context)
    {
        var speciesCount = KnownSpeciesViewModelFactory.GetKnownSpeciesCount(context.CurrentWorld, context.FaunaCatalog, context.ViewState.FocalPolityId);

        if (speciesCount == 0)
        {
            return PlayerInputResult.None;
        }

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                context.ViewState.MoveKnownSpeciesSelection(speciesCount, -1);
                return PlayerInputResult.Render;
            case ConsoleKey.DownArrow:
                context.ViewState.MoveKnownSpeciesSelection(speciesCount, 1);
                return PlayerInputResult.Render;
            default:
                return PlayerInputResult.None;
        }
    }
}
