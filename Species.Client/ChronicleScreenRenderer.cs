using Species.Domain.Diagnostics;
using Species.Domain.Models;

public static class ChronicleScreenRenderer
{
    public static string Render(World world)
    {
        return string.Join(
            Environment.NewLine,
            [
                "Species MVP",
                "Screen: Chronicle",
                "Controls: TAB switch screen | ESC quit",
                string.Empty,
                ChronicleFeedFormatter.Format(world.Chronicle)
            ]);
    }
}
