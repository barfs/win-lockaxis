using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LockAxis;

// Global keyboard hook. Reports watched keys and hides them from other apps.
internal sealed class KeyHook : IDisposable {
    private static readonly IntPtr Swallow = 1;

    private readonly HashSet<uint> _keys;
    private readonly Native.HookProc _proc;
    private readonly IntPtr _handle;

    public event Action<uint>? Pressed;
    public event Action<uint>? Released;

    public KeyHook(IEnumerable<uint> keys) {
        _keys = [.. keys];

        // Delegate kept in a field so the GC does not collect it while hooked.
        _proc = OnKey;
        _handle = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _proc, Native.GetModuleHandle(null), 0);
        if (_handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    private IntPtr OnKey(int nCode, IntPtr wParam, IntPtr lParam) {
        if (nCode < 0) return Native.CallNextHookEx(_handle, nCode, wParam, lParam);

        var info = Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(lParam);
        if (!_keys.Contains(info.vkCode)) return Native.CallNextHookEx(_handle, nCode, wParam, lParam);

        if ((int)wParam is Native.WM_KEYDOWN or Native.WM_SYSKEYDOWN) Pressed?.Invoke(info.vkCode);
        else Released?.Invoke(info.vkCode);

        return Swallow;
    }

    public void Dispose() => Native.UnhookWindowsHookEx(_handle);
}
