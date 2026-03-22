using Species.Client.Enums;
using Species.Client.ViewModelFactories;

namespace Species.Client.InputHandling;

public static class LawsInputHandler
{
    public static PlayerInputResult HandleKey(ConsoleKeyInfo key, PlayerInputContext context)
    {
        var lawSelection = LawsViewModelFactory.GetSelectionInfo(context.CurrentWorld, context.ViewState.FocalPolityId, context.ViewState.CurrentLawIndex);
        var lawCount = lawSelection.LawCount;

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (context.ViewState.IsLawActionMenuOpen)
                {
                    context.ViewState.MoveLawActionSelection(-1);
                    return PlayerInputResult.Render;
                }

                if (lawCount > 0)
                {
                    context.ViewState.MoveLawSelection(lawCount, -1);
                    return PlayerInputResult.Render;
                }

                return PlayerInputResult.None;
            case ConsoleKey.DownArrow:
                if (context.ViewState.IsLawActionMenuOpen)
                {
                    context.ViewState.MoveLawActionSelection(1);
                    return PlayerInputResult.Render;
                }

                if (lawCount > 0)
                {
                    context.ViewState.MoveLawSelection(lawCount, 1);
                    return PlayerInputResult.Render;
                }

                return PlayerInputResult.None;
            case ConsoleKey.Enter:
                if (!context.ViewState.IsSimulationRunning && lawSelection.HasSelectedPendingDecision)
                {
                    if (!context.ViewState.IsLawActionMenuOpen)
                    {
                        context.ViewState.OpenLawActionMenu();
                        return PlayerInputResult.Render;
                    }

                    var actionApplied = context.ViewState.GetSelectedLawAction() == LawDecisionAction.Pass
                        ? context.SimulationEngine.PassActiveLawProposal()
                        : context.SimulationEngine.VetoActiveLawProposal();
                    if (actionApplied)
                    {
                        context.ViewState.CloseLawActionMenu();
                        return PlayerInputResult.Render;
                    }
                }

                return PlayerInputResult.None;
            case ConsoleKey.Backspace:
                if (context.ViewState.IsLawActionMenuOpen)
                {
                    context.ViewState.CloseLawActionMenu();
                    return PlayerInputResult.Render;
                }

                return PlayerInputResult.None;
            default:
                return PlayerInputResult.None;
        }
    }
}
