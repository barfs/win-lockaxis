using System.Globalization;
using LockAxis;

const string DefaultVertical = "V";
const string DefaultHorizontal = "H";
const uint RefreshMs = 50;
const string HexPrefix = "0x";

uint vertical = ParseKey(args.ElementAtOrDefault(0) ?? DefaultVertical);
uint horizontal = ParseKey(args.ElementAtOrDefault(1) ?? DefaultHorizontal);

// Match cursor coordinates to physical pixels on scaled displays.
Native.SetProcessDpiAwarenessContext(Native.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

var clip = new CursorClip();
using var hook = new KeyHook([vertical, horizontal]);

// Keys in press order. The most recent held key decides the axis.
var held = new List<uint>();

hook.Pressed += vk => {
    if (held.Contains(vk)) return; // Auto repeat

    held.Add(vk);
    clip.Lock(AxisOf(vk));
};

hook.Released += vk => {
    held.Remove(vk);
    clip.Lock(held.Count == 0 ? Axis.None : AxisOf(held[^1]));
};

Console.CancelKeyPress += (_, _) => clip.Release();
AppDomain.CurrentDomain.ProcessExit += (_, _) => clip.Release();

Native.TimerProc refresh = (_, _, _, _) => clip.Refresh();
Native.SetTimer(IntPtr.Zero, IntPtr.Zero, RefreshMs, refresh);

Console.WriteLine($"Hold {KeyName(vertical)} for vertical, {KeyName(horizontal)} for horizontal. Ctrl+C quits.");

// Low level hooks and timers need a message loop on this thread.
while (Native.GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
    Native.DispatchMessage(ref msg);

GC.KeepAlive(refresh);

Axis AxisOf(uint vk) => vk == vertical ? Axis.Vertical : Axis.Horizontal;

// Accepts a key name ("V", "F8", "Tab") or a virtual key code ("0x14" for Caps Lock).
static uint ParseKey(string text) {
    if (text.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase))
        return uint.Parse(text[HexPrefix.Length..], NumberStyles.HexNumber);

    return (uint)Enum.Parse<ConsoleKey>(text, ignoreCase: true);
}

static string KeyName(uint vk) =>
    Enum.IsDefined((ConsoleKey)vk) ? ((ConsoleKey)vk).ToString() : $"{HexPrefix}{vk:X2}";
