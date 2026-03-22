using Species.Client.Enums;
using Species.Client.ViewModelFactories;

namespace Species.Client.InputHandling;

public static class ChronicleInputHandler
{
    public static PlayerInputResult HandleKey(ConsoleKeyInfo key, PlayerInputContext context)
    {
        var request = context.ViewState.CreateChronicleViewRequest();

        switch (key.Key)
        {
            case ConsoleKey.Backspace:
                {
                    var selectionInfo = ChronicleViewModelFactory.GetSelectionInfo(context.CurrentWorld, context.ViewState.FocalPolityId, request);
                    context.ViewState.ReturnChronicleToLive(selectionInfo.UrgentCount);
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
                    var selectionInfo = ChronicleViewModelFactory.GetSelectionInfo(context.CurrentWorld, context.ViewState.FocalPolityId, request);
                    context.ViewState.MoveChronicleSelection(selectionInfo.UrgentCount, selectionInfo.EntryCount, -1);
                    return PlayerInputResult.Render;
                }
            case ConsoleKey.DownArrow:
                {
                    var selectionInfo = ChronicleViewModelFactory.GetSelectionInfo(context.CurrentWorld, context.ViewState.FocalPolityId, request);
                    context.ViewState.MoveChronicleSelection(selectionInfo.UrgentCount, selectionInfo.EntryCount, 1);
                    return PlayerInputResult.Render;
                }
            case ConsoleKey.Enter:
                {
                    var selectionInfo = ChronicleViewModelFactory.GetSelectionInfo(context.CurrentWorld, context.ViewState.FocalPolityId, request);
                    if (selectionInfo.SelectedUrgent is null)
                    {
                        return PlayerInputResult.None;
                    }

                    context.ViewState.SetScreen(selectionInfo.SelectedUrgent.TargetScreen);
                    if (selectionInfo.SelectedUrgent.TargetScreen == PlayerScreen.Laws)
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
