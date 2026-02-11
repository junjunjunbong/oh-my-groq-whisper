using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace GroqWhisperPTT.Input;

public class HotkeyHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc;
    private bool _isRecording;

    public event Action? HotkeyPressed;
    public event Action? HotkeyReleased;

    public void Start()
    {
        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }

    public void Stop()
    {
        UnhookWindowsHookEx(_hookId);
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule?.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var key = KeyInterop.KeyFromVirtualKey(vkCode);

            bool isCtrlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            bool isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            bool isSpace = key == Key.Space;

            bool isHotkey = isCtrlDown && isShiftDown && isSpace;

            if (wParam == (IntPtr)WM_KEYDOWN && isHotkey && !_isRecording)
            {
                _isRecording = true;
                HotkeyPressed?.Invoke();
            }
            else if (wParam == (IntPtr)WM_KEYUP && _isRecording)
            {
                // Only trigger release when Space is released (ignore Ctrl/Shift release)
                if (key == Key.Space)
                {
                    _isRecording = false;
                    HotkeyReleased?.Invoke();
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Stop();
    }

    #region Native Methods

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    #endregion
}
