public static class PlayerScreenNavigation
{
    private static readonly PlayerScreen[] OrderedScreens =
    [
        PlayerScreen.Chronicle,
        PlayerScreen.Polity,
        PlayerScreen.Regions,
        PlayerScreen.Laws,
        PlayerScreen.Advancements,
        PlayerScreen.KnownPolities,
        PlayerScreen.KnownSpecies
    ];

    public static PlayerScreen GetNext(PlayerScreen currentScreen)
    {
        var currentIndex = Array.IndexOf(OrderedScreens, currentScreen);
        if (currentIndex < 0)
        {
            return OrderedScreens[0];
        }

        return OrderedScreens[(currentIndex + 1) % OrderedScreens.Length];
    }

    public static bool TryGetScreenForHotkey(ConsoleKey key, out PlayerScreen screen)
    {
        var index = key switch
        {
            ConsoleKey.D1 or ConsoleKey.NumPad1 => 0,
            ConsoleKey.D2 or ConsoleKey.NumPad2 => 1,
            ConsoleKey.D3 or ConsoleKey.NumPad3 => 2,
            ConsoleKey.D4 or ConsoleKey.NumPad4 => 3,
            ConsoleKey.D5 or ConsoleKey.NumPad5 => 4,
            ConsoleKey.D6 or ConsoleKey.NumPad6 => 5,
            ConsoleKey.D7 or ConsoleKey.NumPad7 => 6,
            _ => -1
        };

        if (index < 0)
        {
            screen = default;
            return false;
        }

        screen = OrderedScreens[index];
        return true;
    }

    public static string BuildFooterText(int width, string dim, string reset, string? selectionLabel = null)
    {
        if (width >= 136)
        {
            var text =
                $"Main: {dim}[1]{reset} Chronicle {dim}[2]{reset} Polity {dim}[3]{reset} Regions  |  " +
                $"Governance: {dim}[4]{reset} Laws {dim}[5]{reset} Advancements  |  " +
                $"Knowledge: {dim}[6]{reset} Known Polities {dim}[7]{reset} Known Species  " +
                $"{dim}[TAB]{reset} Next {dim}[SPACE]{reset} Run/Pause {dim}[ENTER]{reset} Step";

            return selectionLabel is null
                ? text
                : $"{text}  {dim}[LEFT/RIGHT]{reset} {selectionLabel}";
        }

        if (width >= 104)
        {
            var text =
                $"Main: {dim}[1]{reset} Chronicle {dim}[2]{reset} Polity {dim}[3]{reset} Regions  |  " +
                $"Gov: {dim}[4]{reset} Laws {dim}[5]{reset} Adv  |  " +
                $"Know: {dim}[6]{reset} Polities {dim}[7]{reset} Species  " +
                $"{dim}[TAB]{reset} Next  {dim}[SPACE]{reset} Run/Pause  {dim}[ENTER]{reset} Step";

            return selectionLabel is null
                ? text
                : $"{text}  {dim}[LEFT/RIGHT]{reset} {selectionLabel}";
        }

        if (width >= 78)
        {
            var text =
                $"Main {dim}[1]{reset}{dim}[2]{reset}{dim}[3]{reset} | Gov {dim}[4]{reset}{dim}[5]{reset} | Know {dim}[6]{reset}{dim}[7]{reset}  " +
                $"{dim}[TAB]{reset} Next  {dim}[SPACE]{reset} Run/Pause  {dim}[ENTER]{reset} Step";

            return selectionLabel is null
                ? text
                : $"{text}  {dim}[LEFT/RIGHT]{reset} {selectionLabel}";
        }

        return selectionLabel is null
            ? $"{dim}M{reset}[123] {dim}G{reset}[45] {dim}K{reset}[67]  {dim}[TAB]{reset}  {dim}[SPACE]{reset}  {dim}[ENTER]{reset}"
            : $"{dim}M{reset}[123] {dim}G{reset}[45] {dim}K{reset}[67]  {dim}[TAB]{reset}  {dim}[SPACE]{reset}  {dim}[ENTER]{reset}  {dim}[< >]{reset}";
    }
}
