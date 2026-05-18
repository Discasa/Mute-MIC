using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MuteMIC.App;

internal static class ThemeService
{
    private const string PersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const int DwmwcpRound = 2;

    public static bool IsLightTheme()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizePath);
        object? value = key?.GetValue("AppsUseLightTheme");
        return value is not int intValue || intValue != 0;
    }

    public static void ApplyWindowFrame(Form form, bool lightTheme)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return;
        }

        int useDarkMode = lightTheme ? 0 : 1;
        DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        int cornerPreference = DwmwcpRound;
        int captionColor = ToColorRef(lightTheme ? Color.White : Color.FromArgb(32, 32, 32));
        int textColor = ToColorRef(lightTheme ? Color.FromArgb(24, 24, 24) : Color.White);
        int borderColor = ToColorRef(lightTheme ? Color.FromArgb(208, 208, 208) : Color.FromArgb(64, 64, 64));

        DwmSetWindowAttribute(form.Handle, DwmwaWindowCornerPreference, ref cornerPreference, sizeof(int));
        DwmSetWindowAttribute(form.Handle, DwmwaCaptionColor, ref captionColor, sizeof(int));
        DwmSetWindowAttribute(form.Handle, DwmwaTextColor, ref textColor, sizeof(int));
        DwmSetWindowAttribute(form.Handle, DwmwaBorderColor, ref borderColor, sizeof(int));
    }

    private static int ToColorRef(Color color)
    {
        return color.R | (color.G << 8) | (color.B << 16);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);
}
