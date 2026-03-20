public sealed class PlayerViewState
{
    public PlayerScreen CurrentScreen { get; private set; } = PlayerScreen.Chronicle;

    public string FocalPolityId { get; private set; } = string.Empty;

    public int CurrentRegionIndex { get; private set; }

    public int CurrentKnownPolityIndex { get; private set; }

    public int CurrentAdvancementIndex { get; private set; }

    public int CurrentLawIndex { get; private set; }

    public int CurrentKnownSpeciesIndex { get; private set; }

    public bool IsSimulationRunning { get; private set; }

    public ChronicleMode CurrentChronicleMode { get; private set; } = ChronicleMode.Live;

    public ChronicleSelectionArea CurrentChronicleSelectionArea { get; private set; } = ChronicleSelectionArea.Entries;

    public int CurrentChronicleUrgentIndex { get; private set; }

    public int CurrentChronicleLiveIndex { get; private set; }

    public int CurrentChronicleArchiveIndex { get; private set; }

    public int CurrentChronicleMilestoneIndex { get; private set; }

    public bool IsLawActionMenuOpen { get; private set; }

    public int CurrentLawActionIndex { get; private set; }

    public void EnsureFocalPolity(Species.Domain.Models.World world)
    {
        FocalPolityId = PlayerFocus.ResolveId(world, FocalPolityId);
    }

    public void CycleScreen()
    {
        CurrentScreen = PlayerScreenNavigation.GetNext(CurrentScreen);
        if (CurrentScreen != PlayerScreen.Laws)
        {
            CloseLawActionMenu();
        }
    }

    public void SetScreen(PlayerScreen screen)
    {
        CurrentScreen = screen;
        if (CurrentScreen != PlayerScreen.Laws)
        {
            CloseLawActionMenu();
        }
    }

    public void ToggleSimulation()
    {
        IsSimulationRunning = !IsSimulationRunning;
    }

    public void MoveKnownPolitySelection(int polityCount, int delta)
    {
        CurrentKnownPolityIndex = MoveIndex(CurrentKnownPolityIndex, polityCount, delta);
    }

    public void MoveRegionSelection(int regionCount, int delta)
    {
        CurrentRegionIndex = MoveIndex(CurrentRegionIndex, regionCount, delta);
    }

    public void MoveAdvancementSelection(int advancementCount, int delta)
    {
        CurrentAdvancementIndex = MoveIndex(CurrentAdvancementIndex, advancementCount, delta);
    }

    public void MoveLawSelection(int lawCount, int delta)
    {
        CurrentLawIndex = MoveIndex(CurrentLawIndex, lawCount, delta);
        CloseLawActionMenu();
    }

    public void SetLawSelection(int index)
    {
        CurrentLawIndex = Math.Max(0, index);
        CloseLawActionMenu();
    }

    public void MoveKnownSpeciesSelection(int speciesCount, int delta)
    {
        CurrentKnownSpeciesIndex = MoveIndex(CurrentKnownSpeciesIndex, speciesCount, delta);
    }

    public void ClampRegionIndex(int regionCount)
    {
        CurrentRegionIndex = ClampIndex(CurrentRegionIndex, regionCount);
    }

    public void ClampKnownPolityIndex(int polityCount)
    {
        CurrentKnownPolityIndex = ClampIndex(CurrentKnownPolityIndex, polityCount);
    }

    public void ClampAdvancementIndex(int advancementCount)
    {
        CurrentAdvancementIndex = ClampIndex(CurrentAdvancementIndex, advancementCount);
    }

    public void ClampLawIndex(int lawCount)
    {
        CurrentLawIndex = ClampIndex(CurrentLawIndex, lawCount);
        if (lawCount == 0)
        {
            CloseLawActionMenu();
        }
    }

    public void ClampKnownSpeciesIndex(int speciesCount)
    {
        CurrentKnownSpeciesIndex = ClampIndex(CurrentKnownSpeciesIndex, speciesCount);
    }

    public void MoveToNextChronicleMode()
    {
        CurrentChronicleMode = CurrentChronicleMode switch
        {
            ChronicleMode.Live => ChronicleMode.Archive,
            ChronicleMode.Archive => ChronicleMode.Milestones,
            _ => ChronicleMode.Live
        };
    }

    public void MoveToPreviousChronicleMode()
    {
        CurrentChronicleMode = CurrentChronicleMode switch
        {
            ChronicleMode.Live => ChronicleMode.Milestones,
            ChronicleMode.Archive => ChronicleMode.Live,
            _ => ChronicleMode.Archive
        };
    }

    public void ReturnChronicleToLive(int urgentCount)
    {
        CurrentChronicleMode = ChronicleMode.Live;
        CurrentChronicleLiveIndex = 0;
        CurrentChronicleUrgentIndex = 0;
        CurrentChronicleSelectionArea = urgentCount > 0
            ? ChronicleSelectionArea.Urgent
            : ChronicleSelectionArea.Entries;
    }

    public void MoveChronicleSelection(int urgentCount, int entryCount, int delta)
    {
        var totalCount = Math.Max(0, urgentCount) + Math.Max(0, entryCount);
        if (totalCount <= 0)
        {
            CurrentChronicleSelectionArea = ChronicleSelectionArea.Entries;
            CurrentChronicleUrgentIndex = 0;
            SetCurrentChronicleEntryIndex(0);
            return;
        }

        var combinedIndex = CurrentChronicleSelectionArea == ChronicleSelectionArea.Urgent
            ? ClampIndex(CurrentChronicleUrgentIndex, urgentCount)
            : Math.Max(0, urgentCount) + ClampIndex(GetCurrentChronicleEntryIndex(), entryCount);
        combinedIndex = MoveIndex(combinedIndex, totalCount, delta);

        if (combinedIndex < urgentCount)
        {
            CurrentChronicleSelectionArea = ChronicleSelectionArea.Urgent;
            CurrentChronicleUrgentIndex = combinedIndex;
            return;
        }

        CurrentChronicleSelectionArea = ChronicleSelectionArea.Entries;
        SetCurrentChronicleEntryIndex(combinedIndex - Math.Max(0, urgentCount));
    }

    public void ClampChronicleSelection(int urgentCount, int entryCount)
    {
        CurrentChronicleUrgentIndex = ClampIndex(CurrentChronicleUrgentIndex, urgentCount);
        SetCurrentChronicleEntryIndex(ClampIndex(GetCurrentChronicleEntryIndex(), entryCount));

        if (urgentCount <= 0 && entryCount <= 0)
        {
            CurrentChronicleSelectionArea = ChronicleSelectionArea.Entries;
            return;
        }

        if (CurrentChronicleSelectionArea == ChronicleSelectionArea.Urgent && urgentCount <= 0)
        {
            CurrentChronicleSelectionArea = ChronicleSelectionArea.Entries;
        }
        else if (CurrentChronicleSelectionArea == ChronicleSelectionArea.Entries && entryCount <= 0)
        {
            CurrentChronicleSelectionArea = urgentCount > 0
                ? ChronicleSelectionArea.Urgent
                : ChronicleSelectionArea.Entries;
        }
    }

    public void OpenLawActionMenu()
    {
        IsLawActionMenuOpen = true;
        CurrentLawActionIndex = 0;
    }

    public void CloseLawActionMenu()
    {
        IsLawActionMenuOpen = false;
        CurrentLawActionIndex = 0;
    }

    public void MoveLawActionSelection(int delta)
    {
        if (!IsLawActionMenuOpen)
        {
            return;
        }

        CurrentLawActionIndex = MoveIndex(CurrentLawActionIndex, 2, delta);
    }

    public LawDecisionAction GetSelectedLawAction()
    {
        return CurrentLawActionIndex == 0 ? LawDecisionAction.Pass : LawDecisionAction.Veto;
    }

    private int GetCurrentChronicleEntryIndex()
    {
        return CurrentChronicleMode switch
        {
            ChronicleMode.Live => CurrentChronicleLiveIndex,
            ChronicleMode.Archive => CurrentChronicleArchiveIndex,
            _ => CurrentChronicleMilestoneIndex
        };
    }

    private void SetCurrentChronicleEntryIndex(int value)
    {
        switch (CurrentChronicleMode)
        {
            case ChronicleMode.Live:
                CurrentChronicleLiveIndex = Math.Max(0, value);
                break;
            case ChronicleMode.Archive:
                CurrentChronicleArchiveIndex = Math.Max(0, value);
                break;
            default:
                CurrentChronicleMilestoneIndex = Math.Max(0, value);
                break;
        }
    }

    private static int ClampIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        if (index < 0)
        {
            return 0;
        }

        return index >= count ? count - 1 : index;
    }

    private static int MoveIndex(int index, int count, int delta)
    {
        if (count <= 0)
        {
            return 0;
        }

        return (index + delta % count + count) % count;
    }
}

public enum ChronicleMode
{
    Live,
    Archive,
    Milestones
}

public enum ChronicleSelectionArea
{
    Urgent,
    Entries
}

public enum LawDecisionAction
{
    Pass,
    Veto
}
