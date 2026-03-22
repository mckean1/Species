namespace Species.Client.Presentation;

public sealed class LawSelectionInfo
{
    public LawSelectionInfo(int lawCount, bool hasSelectedPendingDecision)
    {
        LawCount = lawCount;
        HasSelectedPendingDecision = hasSelectedPendingDecision;
    }

    public int LawCount { get; }

    public bool HasSelectedPendingDecision { get; }
}
