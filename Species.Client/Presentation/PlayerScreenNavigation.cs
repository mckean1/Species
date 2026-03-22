using Species.Client.Enums;

namespace Species.Client.Presentation;

public static class PlayerScreenNavigation
{
    private static readonly PlayerScreen[] OrderedScreens =
    [
        PlayerScreen.Chronicle,
        PlayerScreen.Polity,
        PlayerScreen.Government,
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
            ConsoleKey.D8 or ConsoleKey.NumPad8 => 7,
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
                $"Main: {dim}[1]{reset} Chronicle {dim}[2]{reset} Overview {dim}[3]{reset} Government {dim}[4]{reset} Regions  |  " +
                $"Civic: {dim}[5]{reset} Laws {dim}[6]{reset} Advancements  |  " +
                $"Discovery: {dim}[7]{reset} Known Polities {dim}[8]{reset} Known Species  " +
                $"{dim}[TAB]{reset} Next {dim}[SPACE]{reset} Run/Pause {dim}[ENTER]{reset} Step";

            return selectionLabel is null
                ? text
                : $"{text}  {dim}[LEFT/RIGHT]{reset} {selectionLabel}";
        }

        if (width >= 104)
        {
            var text =
                $"Main: {dim}[1]{reset} Chronicle {dim}[2]{reset} Overview {dim}[3]{reset} Gov {dim}[4]{reset} Regions  |  " +
                $"Civic: {dim}[5]{reset} Laws {dim}[6]{reset} Adv  |  " +
                $"Disc: {dim}[7]{reset} Polities {dim}[8]{reset} Species  " +
                $"{dim}[TAB]{reset} Next  {dim}[SPACE]{reset} Run/Pause  {dim}[ENTER]{reset} Step";

            return selectionLabel is null
                ? text
                : $"{text}  {dim}[LEFT/RIGHT]{reset} {selectionLabel}";
        }

        if (width >= 78)
        {
            var text =
                $"Main {dim}[1]{reset}{dim}[2]{reset}{dim}[3]{reset}{dim}[4]{reset} | Civ {dim}[5]{reset}{dim}[6]{reset} | Disc {dim}[7]{reset}{dim}[8]{reset}  " +
                $"{dim}[TAB]{reset} Next  {dim}[SPACE]{reset} Run/Pause  {dim}[ENTER]{reset} Step";

            return selectionLabel is null
                ? text
                : $"{text}  {dim}[LEFT/RIGHT]{reset} {selectionLabel}";
        }

        return selectionLabel is null
            ? $"{dim}M{reset}[1234] {dim}C{reset}[56] {dim}D{reset}[78]  {dim}[TAB]{reset}  {dim}[SPACE]{reset}  {dim}[ENTER]{reset}"
            : $"{dim}M{reset}[1234] {dim}C{reset}[56] {dim}D{reset}[78]  {dim}[TAB]{reset}  {dim}[SPACE]{reset}  {dim}[ENTER]{reset}  {dim}[< >]{reset}";
    }
}
