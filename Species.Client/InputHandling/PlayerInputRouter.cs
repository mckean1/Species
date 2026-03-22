using Species.Client.Enums;
using Species.Client.Presentation;

namespace Species.Client.InputHandling;

public static class PlayerInputRouter
{
    public static PlayerInputResult ProcessKey(ConsoleKeyInfo key, PlayerInputContext context)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            return PlayerInputResult.Exit;
        }

        if (PlayerScreenNavigation.TryGetScreenForHotkey(key.Key, out var hotkeyScreen))
        {
            context.ViewState.SetScreen(hotkeyScreen);
            return PlayerInputResult.Render;
        }

        switch (key.Key)
        {
            case ConsoleKey.Spacebar:
                context.ViewState.ToggleSimulation();
                context.ViewState.CloseLawActionMenu();
                return PlayerInputResult.Render | PlayerInputResult.ResetTickTimer;
            case ConsoleKey.N when !context.ViewState.IsSimulationRunning:
                return context.AdvanceOneMonth()
                    ? PlayerInputResult.Render | PlayerInputResult.ResetTickTimer
                    : PlayerInputResult.Exit;
            case ConsoleKey.Tab:
                context.ViewState.CycleScreen();
                return PlayerInputResult.Render;
        }

        return context.ViewState.CurrentScreen switch
        {
            PlayerScreen.Chronicle => ChronicleInputHandler.HandleKey(key, context),
            PlayerScreen.Regions => RegionsInputHandler.HandleKey(key, context),
            PlayerScreen.KnownPolities => KnownPolitiesInputHandler.HandleKey(key, context),
            PlayerScreen.Advancements => AdvancementsInputHandler.HandleKey(key, context),
            PlayerScreen.Laws => LawsInputHandler.HandleKey(key, context),
            PlayerScreen.KnownSpecies => KnownSpeciesInputHandler.HandleKey(key, context),
            _ => PlayerInputResult.None
        };
    }
}
