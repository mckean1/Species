using Species.Client.Enums;
using Species.Client.ViewModelFactories;

namespace Species.Client.InputHandling;

public static class ChronicleInputHandler
{
    public static PlayerInputResult HandleKey(ConsoleKeyInfo key, PlayerInputContext context)
    {
        switch (key.Key)
        {
            case ConsoleKey.Backspace:
                {
                    var chronicleData = ChronicleViewModelFactory.Build(context.CurrentWorld, context.ViewState.FocalPolityId, context.ViewState);
                    context.ViewState.ReturnChronicleToLive(chronicleData.UrgentItems.Count);
                    return PlayerInputResult.Render;
                }
            case ConsoleKey.LeftArrow:
                context.ViewState.MoveToPreviousChronicleMode();
                return PlayerInputResult.Render;
            case ConsoleKey.RightArrow:
                context.ViewState.MoveToNextChronicleMode();
                return PlayerInputResult.Render;
            case ConsoleKey.UpArrow:
                {
                    var chronicleData = ChronicleViewModelFactory.Build(context.CurrentWorld, context.ViewState.FocalPolityId, context.ViewState);
                    context.ViewState.MoveChronicleSelection(chronicleData.UrgentItems.Count, chronicleData.Entries.Count, -1);
                    return PlayerInputResult.Render;
                }
            case ConsoleKey.DownArrow:
                {
                    var chronicleData = ChronicleViewModelFactory.Build(context.CurrentWorld, context.ViewState.FocalPolityId, context.ViewState);
                    context.ViewState.MoveChronicleSelection(chronicleData.UrgentItems.Count, chronicleData.Entries.Count, 1);
                    return PlayerInputResult.Render;
                }
            case ConsoleKey.Enter:
                {
                    var chronicleData = ChronicleViewModelFactory.Build(context.CurrentWorld, context.ViewState.FocalPolityId, context.ViewState);
                    if (chronicleData.SelectedUrgent is null)
                    {
                        return PlayerInputResult.None;
                    }

                    context.ViewState.SetScreen(chronicleData.SelectedUrgent.TargetScreen);
                    if (chronicleData.SelectedUrgent.TargetScreen == PlayerScreen.Laws)
                    {
                        context.ViewState.SetLawSelection(0);
                    }

                    return PlayerInputResult.Render;
                }
            default:
                return PlayerInputResult.None;
        }
    }
}
