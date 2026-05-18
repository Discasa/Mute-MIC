namespace MuteMIC.App;

internal static class Strings
{
    private static readonly Dictionary<string, Dictionary<string, string>> Values = new()
    {
        ["en"] = new Dictionary<string, string>
        {
            ["AppName"] = "Mute MIC",
            ["Hotkeys"] = "Hotkeys",
            ["ColorScheme"] = "Color scheme",
            ["Monochrome"] = "Monochrome",
            ["Colorful"] = "Colorful",
            ["Language"] = "Language",
            ["English"] = "English",
            ["Portuguese"] = "Portuguese",
            ["Exit"] = "Exit",
            ["ToggleLabel"] = "Toggle hotkey",
            ["MuteLabel"] = "Mute hotkey",
            ["UnmuteLabel"] = "Unmute hotkey",
            ["Reset"] = "reset",
            ["NoInputs"] = "No active audio input devices",
            ["PartialFailure"] = "Some devices could not be updated.",
            ["ClickToMute"] = "Click to mute",
            ["ClickToUnmute"] = "Click to unmute"
        },
        ["pt-BR"] = new Dictionary<string, string>
        {
            ["AppName"] = "Mute MIC",
            ["Hotkeys"] = "Atalhos",
            ["ColorScheme"] = "Esquema de cores",
            ["Monochrome"] = "Monocromático",
            ["Colorful"] = "Colorido",
            ["Language"] = "Idioma",
            ["English"] = "Inglês",
            ["Portuguese"] = "Português",
            ["Exit"] = "Sair",
            ["ToggleLabel"] = "Atalho para alternar",
            ["MuteLabel"] = "Atalho para mutar",
            ["UnmuteLabel"] = "Atalho para desmutar",
            ["Reset"] = "limpar",
            ["NoInputs"] = "Nenhuma entrada de áudio ativa",
            ["PartialFailure"] = "Alguns dispositivos não puderam ser atualizados.",
            ["ClickToMute"] = "Clique para mutar",
            ["ClickToUnmute"] = "Clique para desmutar"
        }
    };

    public static string Get(string language, string key)
    {
        if (!Values.TryGetValue(language, out Dictionary<string, string>? languageValues))
        {
            languageValues = Values["en"];
        }

        return languageValues.TryGetValue(key, out string? value) ? value : Values["en"][key];
    }
}
