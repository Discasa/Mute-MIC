using System.Runtime.InteropServices;

namespace MuteMIC.App;

internal sealed class GlobalHotkeys : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;
    private readonly HotkeyMessageWindow _window = new();
    private readonly Dictionary<int, Action> _actions = [];

    public GlobalHotkeys()
    {
        _window.HotkeyPressed += OnHotkeyPressed;
    }

    public void Register(int id, HotkeyDefinition hotkey, Action action)
    {
        Unregister(id);
        if (hotkey.IsEmpty)
        {
            return;
        }

        uint modifiers = (uint)hotkey.Modifiers | ModNoRepeat;
        if (!RegisterHotKey(_window.Handle, id, modifiers, (uint)hotkey.Key))
        {
            throw new InvalidOperationException($"Could not register hotkey {hotkey.ToDisplayString("en")}.");
        }

        _actions[id] = action;
    }

    public void Unregister(int id)
    {
        if (_actions.Remove(id))
        {
            UnregisterHotKey(_window.Handle, id);
        }
    }

    public void Dispose()
    {
        foreach (int id in _actions.Keys.ToArray())
        {
            Unregister(id);
        }

        _window.HotkeyPressed -= OnHotkeyPressed;
        _window.DestroyHandle();
    }

    private void OnHotkeyPressed(object? sender, int id)
    {
        if (_actions.TryGetValue(id, out Action? action))
        {
            action();
        }
    }

    private sealed class HotkeyMessageWindow : NativeWindow
    {
        public event EventHandler<int>? HotkeyPressed;

        public HotkeyMessageWindow()
        {
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey)
            {
                HotkeyPressed?.Invoke(this, m.WParam.ToInt32());
                return;
            }

            base.WndProc(ref m);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
