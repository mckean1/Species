public sealed class PlayerViewState
{
    public PlayerScreen CurrentScreen { get; private set; } = PlayerScreen.Chronicle;

    public int CurrentRegionIndex { get; private set; }

    public bool IsSimulationRunning { get; private set; }

    public void CycleScreen()
    {
        CurrentScreen = CurrentScreen == PlayerScreen.Chronicle
            ? PlayerScreen.RegionViewer
            : PlayerScreen.Chronicle;
    }

    public void MoveToNextRegion(int regionCount)
    {
        if (regionCount <= 0)
        {
            CurrentRegionIndex = 0;
            return;
        }

        CurrentRegionIndex = (CurrentRegionIndex + 1) % regionCount;
    }

    public void MoveToPreviousRegion(int regionCount)
    {
        if (regionCount <= 0)
        {
            CurrentRegionIndex = 0;
            return;
        }

        CurrentRegionIndex = (CurrentRegionIndex - 1 + regionCount) % regionCount;
    }

    public void ClampRegionIndex(int regionCount)
    {
        if (regionCount <= 0)
        {
            CurrentRegionIndex = 0;
            return;
        }

        if (CurrentRegionIndex >= regionCount)
        {
            CurrentRegionIndex = regionCount - 1;
        }
    }

    public void ToggleSimulation()
    {
        IsSimulationRunning = !IsSimulationRunning;
    }
}
