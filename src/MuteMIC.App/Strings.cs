namespace MuteMIC.App;

internal static class Strings
{
    private static readonly Dictionary<string, Dictionary<string, string>> Values = new()
    {
        ["en"] = new Dictionary<string, string>
        {
            ["AppName"] = "Mute MIC",
            ["ToggleAll"] = "Toggle all inputs",
            ["MuteAll"] = "Mute all inputs",
            ["UnmuteAll"] = "Unmute all inputs",
            ["Hotkeys"] = "Hotkeys",
            ["Language"] = "Language",
            ["English"] = "English",
            ["Portuguese"] = "Portuguese",
            ["Exit"] = "Exit",
            ["ToggleLabel"] = "Toggle hotkey",
            ["MuteLabel"] = "Mute hotkey",
            ["UnmuteLabel"] = "Unmute hotkey",
            ["Reset"] = "reset",
            ["NoInputs"] = "No active audio input devices",
            ["AllMuted"] = "All inputs muted",
            ["InputsOpen"] = "Audio inputs open",
            ["PartialFailure"] = "Some devices could not be updated.",
            ["DeviceCount"] = "{0} active input(s)"
        },
        ["pt-BR"] = new Dictionary<string, string>
        {
            ["AppName"] = "Mute MIC",
            ["ToggleAll"] = "Alternar todas as entradas",
            ["MuteAll"] = "Mutar todas as entradas",
            ["UnmuteAll"] = "Desmutar todas as entradas",
            ["Hotkeys"] = "Atalhos",
            ["Language"] = "Idioma",
            ["English"] = "Ingles",
            ["Portuguese"] = "Portugues",
            ["Exit"] = "Sair",
            ["ToggleLabel"] = "Atalho para alternar",
            ["MuteLabel"] = "Atalho para mutar",
            ["UnmuteLabel"] = "Atalho para desmutar",
            ["Reset"] = "limpar",
            ["NoInputs"] = "Nenhuma entrada de audio ativa",
            ["AllMuted"] = "Todas as entradas mutadas",
            ["InputsOpen"] = "Entradas de audio abertas",
            ["PartialFailure"] = "Alguns dispositivos nao puderam ser atualizados.",
            ["DeviceCount"] = "{0} entrada(s) ativa(s)"
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
