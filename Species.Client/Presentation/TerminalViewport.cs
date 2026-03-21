namespace Species.Client.Presentation;

public readonly record struct TerminalViewport(int Width, int Height)
{
    public static TerminalViewport GetCurrent()
    {
        if (Console.IsOutputRedirected)
        {
            return new TerminalViewport(120, 32);
        }

        try
        {
            return new TerminalViewport(
                Math.Max(1, Console.WindowWidth),
                Math.Max(1, Console.WindowHeight));
        }
        catch (IOException)
        {
            return new TerminalViewport(120, 32);
        }
    }
}
