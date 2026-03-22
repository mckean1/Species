using Species.Client.Enums;

namespace Species.Client.Models;

public sealed class ChronicleUrgentItem
{
    public ChronicleUrgentItem(
        string id,
        string text,
        string cause,
        string impact,
        PlayerScreen targetScreen,
        string? targetId)
    {
        Id = id;
        Text = text;
        Cause = cause;
        Impact = impact;
        TargetScreen = targetScreen;
        TargetId = targetId;
    }

    public string Id { get; }

    public string Text { get; }

    public string Cause { get; }

    public string Impact { get; }

    public PlayerScreen TargetScreen { get; }

    public string? TargetId { get; }
}
