namespace MuteMIC.App;

[Flags]
internal enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Windows = 0x0008
}

internal readonly record struct HotkeyDefinition(HotkeyModifiers Modifiers, Keys Key)
{
    public static HotkeyDefinition None => new(HotkeyModifiers.None, Keys.None);
    public bool IsEmpty => Key == Keys.None;

    public string ToStorageString()
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        List<string> parts = [];
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Control");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Windows)) parts.Add("Windows");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }

    public string ToDisplayString(string language)
    {
        if (IsEmpty)
        {
            return language == "pt-BR" ? "Nenhuma" : "None";
        }

        List<string> parts = [];
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Windows)) parts.Add("Win");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }

    public static HotkeyDefinition Parse(string? value, HotkeyDefinition fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string normalized = value.Replace(",", "+", StringComparison.Ordinal);
        string[] parts = normalized
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0 || parts.Any(part => part.Equals("None", StringComparison.OrdinalIgnoreCase)))
        {
            return None;
        }

        HotkeyModifiers modifiers = HotkeyModifiers.None;
        Keys key = Keys.None;

        foreach (string part in parts)
        {
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.Control;
                continue;
            }

            if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.Shift;
                continue;
            }

            if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.Alt;
                continue;
            }

            if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.Windows;
                continue;
            }

            if (Enum.TryParse(part, true, out Keys parsedKey))
            {
                key = parsedKey;
            }
        }

        return key == Keys.None ? fallback : new HotkeyDefinition(modifiers, key);
    }
}
