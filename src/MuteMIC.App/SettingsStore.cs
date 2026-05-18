using Microsoft.Win32;

namespace MuteMIC.App;

internal sealed class AppSettings
{
    public HotkeyDefinition ToggleHotkey { get; set; } = new(HotkeyModifiers.Control, Keys.M);
    public HotkeyDefinition MuteHotkey { get; set; } = HotkeyDefinition.None;
    public HotkeyDefinition UnmuteHotkey { get; set; } = HotkeyDefinition.None;
    public string Language { get; set; } = "en";
}

internal sealed class SettingsStore
{
    private const string RegistryPath = @"Software\Mute MIC";

    public AppSettings Load()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        AppSettings settings = new();
        if (key is null)
        {
            return settings;
        }

        settings.ToggleHotkey = HotkeyDefinition.Parse(
            key.GetValue("ToggleHotkey") as string,
            new HotkeyDefinition(HotkeyModifiers.Control, Keys.M));
        settings.MuteHotkey = HotkeyDefinition.Parse(
            key.GetValue("MuteHotkey") as string,
            HotkeyDefinition.None);
        settings.UnmuteHotkey = HotkeyDefinition.Parse(
            key.GetValue("UnmuteHotkey") as string,
            HotkeyDefinition.None);
        settings.Language = (key.GetValue("Language") as string) == "pt-BR" ? "pt-BR" : "en";
        return settings;
    }

    public void Save(AppSettings settings)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        key.SetValue("ToggleHotkey", settings.ToggleHotkey.ToStorageString());
        key.SetValue("MuteHotkey", settings.MuteHotkey.ToStorageString());
        key.SetValue("UnmuteHotkey", settings.UnmuteHotkey.ToStorageString());
        key.SetValue("Language", settings.Language);
    }
}
