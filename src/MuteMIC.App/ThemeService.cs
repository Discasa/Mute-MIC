using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MuteMIC.App;

internal static class ThemeService
{
    private const string PersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const int DwmwaUseImmersiveDarkMode = 20;

    public static bool IsLightTheme()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizePath);
        object? value = key?.GetValue("AppsUseLightTheme");
        return value is not int intValue || intValue != 0;
    }

    public static void ApplyTitleBar(Form form, bool lightTheme)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return;
        }

        int useDarkMode = lightTheme ? 0 : 1;
        DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);
}
