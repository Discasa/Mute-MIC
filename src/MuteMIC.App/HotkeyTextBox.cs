namespace MuteMIC.App;

using System.ComponentModel;

internal sealed class HotkeyTextBox : TextBox
{
    private HotkeyDefinition _hotkey = HotkeyDefinition.None;

    public event EventHandler? HotkeyChanged;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Language { get; set; } = "en";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public HotkeyDefinition Hotkey
    {
        get => _hotkey;
        set => SetHotkey(value, true);
    }

    public HotkeyTextBox()
    {
        ReadOnly = true;
        TabStop = true;
        ShortcutsEnabled = false;
    }

    public void RefreshText()
    {
        Text = _hotkey.ToDisplayString(Language);
    }

    private void SetHotkey(HotkeyDefinition value, bool raiseEvent)
    {
        _hotkey = value;
        RefreshText();
        if (raiseEvent)
        {
            HotkeyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.SuppressKeyPress = true;

        if (e.KeyCode is Keys.Back or Keys.Delete or Keys.Escape)
        {
            Hotkey = HotkeyDefinition.None;
            return;
        }

        if (IsModifierKey(e.KeyCode))
        {
            return;
        }

        HotkeyModifiers modifiers = HotkeyModifiers.None;
        if (e.Control) modifiers |= HotkeyModifiers.Control;
        if (e.Shift) modifiers |= HotkeyModifiers.Shift;
        if (e.Alt) modifiers |= HotkeyModifiers.Alt;

        Hotkey = new HotkeyDefinition(modifiers, e.KeyCode);
    }

    private static bool IsModifierKey(Keys key)
    {
        return key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin;
    }
}
