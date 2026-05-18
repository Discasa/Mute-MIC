using System.Runtime.InteropServices;

namespace MuteMIC.App;

internal sealed class GlobalHotkeys : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;
    private readonly IntPtr _handle;
    private readonly Dictionary<int, Action> _actions = [];

    public GlobalHotkeys(IntPtr handle)
    {
        _handle = handle;
    }

    public bool HandleMessage(ref Message message)
    {
        if (message.Msg != WmHotkey)
        {
            return false;
        }

        int id = message.WParam.ToInt32();
        if (_actions.TryGetValue(id, out Action? action))
        {
            action();
            return true;
        }

        return false;
    }

    public void Register(int id, HotkeyDefinition hotkey, Action action)
    {
        Unregister(id);
        if (hotkey.IsEmpty)
        {
            return;
        }

        uint modifiers = (uint)hotkey.Modifiers | ModNoRepeat;
        if (!RegisterHotKey(_handle, id, modifiers, (uint)hotkey.Key))
        {
            throw new InvalidOperationException($"Could not register hotkey {hotkey.ToDisplayString("en")}.");
        }

        _actions[id] = action;
    }

    public void Unregister(int id)
    {
        if (_actions.Remove(id))
        {
            UnregisterHotKey(_handle, id);
        }
    }

    public void Dispose()
    {
        foreach (int id in _actions.Keys.ToArray())
        {
            Unregister(id);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
