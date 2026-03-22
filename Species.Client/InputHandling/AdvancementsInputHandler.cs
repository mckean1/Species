using Species.Client.ViewModelFactories;

namespace Species.Client.InputHandling;

public static class AdvancementsInputHandler
{
    public static PlayerInputResult HandleKey(ConsoleKeyInfo key, PlayerInputContext context)
    {
        var advancementCount = AdvancementViewModelFactory.Build(
            context.CurrentWorld,
            context.ViewState.FocalPolityId,
            context.DiscoveryCatalog,
            context.AdvancementCatalog,
            context.ViewState.CurrentAdvancementIndex).Items.Count;

        if (advancementCount == 0)
        {
            return PlayerInputResult.None;
        }

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                context.ViewState.MoveAdvancementSelection(advancementCount, -1);
                return PlayerInputResult.Render;
            case ConsoleKey.DownArrow:
                context.ViewState.MoveAdvancementSelection(advancementCount, 1);
                return PlayerInputResult.Render;
            default:
                return PlayerInputResult.None;
        }
    }
}
