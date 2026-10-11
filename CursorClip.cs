namespace LockAxis;

internal enum Axis { None, Vertical, Horizontal }

// Confines the cursor to a 1 pixel line through its position at lock time.
internal sealed class CursorClip {
    private Native.RECT? _line;

    public void Lock(Axis axis) {
        if (axis == Axis.None) {
            Release();
            return;
        }

        Native.GetCursorPos(out var pos);
        int left = Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN);
        int top = Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN);
        int right = left + Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN);
        int bottom = top + Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN);

        _line = axis == Axis.Vertical
            ? new Native.RECT { Left = pos.X, Right = pos.X + 1, Top = top, Bottom = bottom }
            : new Native.RECT { Left = left, Right = right, Top = pos.Y, Bottom = pos.Y + 1 };

        Refresh();
    }

    public void Release() {
        _line = null;
        Native.UnclipCursor(IntPtr.Zero);
    }

    // Re-apply the clip, since window activation in other apps can reset it.
    public void Refresh() {
        if (_line is not { } line) return;

        Native.ClipCursor(ref line);
    }
}
