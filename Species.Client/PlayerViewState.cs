public sealed class PlayerViewState
{
    public PlayerScreen CurrentScreen { get; private set; } = PlayerScreen.Chronicle;

    public int CurrentRegionIndex { get; private set; }

    public int CurrentKnownPolityIndex { get; private set; }

    public int CurrentAdvancementIndex { get; private set; }

    public int CurrentLawIndex { get; private set; }

    public int CurrentKnownSpeciesIndex { get; private set; }

    public bool IsSimulationRunning { get; private set; }

    public void CycleScreen()
    {
        CurrentScreen = PlayerScreenNavigation.GetNext(CurrentScreen);
    }

    public void SetScreen(PlayerScreen screen)
    {
        CurrentScreen = screen;
    }

    public void MoveToNextKnownPolity(int polityCount)
    {
        if (polityCount <= 0)
        {
            CurrentKnownPolityIndex = 0;
            return;
        }

        CurrentKnownPolityIndex = (CurrentKnownPolityIndex + 1) % polityCount;
    }

    public void MoveToPreviousKnownPolity(int polityCount)
    {
        if (polityCount <= 0)
        {
            CurrentKnownPolityIndex = 0;
            return;
        }

        CurrentKnownPolityIndex = (CurrentKnownPolityIndex - 1 + polityCount) % polityCount;
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

    public void MoveToNextAdvancement(int advancementCount)
    {
        if (advancementCount <= 0)
        {
            CurrentAdvancementIndex = 0;
            return;
        }

        CurrentAdvancementIndex = (CurrentAdvancementIndex + 1) % advancementCount;
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

    public void MoveToPreviousAdvancement(int advancementCount)
    {
        if (advancementCount <= 0)
        {
            CurrentAdvancementIndex = 0;
            return;
        }

        CurrentAdvancementIndex = (CurrentAdvancementIndex - 1 + advancementCount) % advancementCount;
    }

    public void MoveToNextLaw(int lawCount)
    {
        if (lawCount <= 0)
        {
            CurrentLawIndex = 0;
            return;
        }

        CurrentLawIndex = (CurrentLawIndex + 1) % lawCount;
    }

    public void MoveToPreviousLaw(int lawCount)
    {
        if (lawCount <= 0)
        {
            CurrentLawIndex = 0;
            return;
        }

        CurrentLawIndex = (CurrentLawIndex - 1 + lawCount) % lawCount;
    }

    public void MoveToNextKnownSpecies(int speciesCount)
    {
        if (speciesCount <= 0)
        {
            CurrentKnownSpeciesIndex = 0;
            return;
        }

        CurrentKnownSpeciesIndex = (CurrentKnownSpeciesIndex + 1) % speciesCount;
    }

    public void MoveToPreviousKnownSpecies(int speciesCount)
    {
        if (speciesCount <= 0)
        {
            CurrentKnownSpeciesIndex = 0;
            return;
        }

        CurrentKnownSpeciesIndex = (CurrentKnownSpeciesIndex - 1 + speciesCount) % speciesCount;
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

    public void ClampKnownPolityIndex(int polityCount)
    {
        if (polityCount <= 0)
        {
            CurrentKnownPolityIndex = 0;
            return;
        }

        if (CurrentKnownPolityIndex >= polityCount)
        {
            CurrentKnownPolityIndex = polityCount - 1;
        }
    }

    public void ClampAdvancementIndex(int advancementCount)
    {
        if (advancementCount <= 0)
        {
            CurrentAdvancementIndex = 0;
            return;
        }

        if (CurrentAdvancementIndex >= advancementCount)
        {
            CurrentAdvancementIndex = advancementCount - 1;
        }
    }

    public void ClampLawIndex(int lawCount)
    {
        if (lawCount <= 0)
        {
            CurrentLawIndex = 0;
            return;
        }

        if (CurrentLawIndex >= lawCount)
        {
            CurrentLawIndex = lawCount - 1;
        }
    }

    public void ClampKnownSpeciesIndex(int speciesCount)
    {
        if (speciesCount <= 0)
        {
            CurrentKnownSpeciesIndex = 0;
            return;
        }

        if (CurrentKnownSpeciesIndex >= speciesCount)
        {
            CurrentKnownSpeciesIndex = speciesCount - 1;
        }
    }

    public void ToggleSimulation()
    {
        IsSimulationRunning = !IsSimulationRunning;
    }
}
