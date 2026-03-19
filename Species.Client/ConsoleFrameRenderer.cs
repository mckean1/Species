public sealed class ConsoleFrameRenderer
{
    private string _lastFrame = string.Empty;
    private TerminalViewport _lastViewport;
    private bool _hasRendered;

    public void Render(string frame, TerminalViewport viewport)
    {
        if (Console.IsOutputRedirected)
        {
            Console.WriteLine(frame);
            return;
        }

        if (_hasRendered &&
            _lastViewport == viewport &&
            string.Equals(_lastFrame, frame, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            if (!_hasRendered || _lastViewport != viewport)
            {
                Console.Clear();
            }

            Console.SetCursorPosition(0, 0);
            Console.Write(frame);
            _lastFrame = frame;
            _lastViewport = viewport;
            _hasRendered = true;
        }
        catch (IOException)
        {
            Console.Clear();
            Console.Write(frame);
            _lastFrame = frame;
            _lastViewport = viewport;
            _hasRendered = true;
        }
    }

    public void Reset()
    {
        _lastFrame = string.Empty;
        _lastViewport = default;
        _hasRendered = false;
    }
}
