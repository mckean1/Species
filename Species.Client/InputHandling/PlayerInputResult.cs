namespace Species.Client.InputHandling;

[Flags]
public enum PlayerInputResult
{
    None = 0,
    Render = 1,
    ResetTickTimer = 2,
    Exit = 4
}
