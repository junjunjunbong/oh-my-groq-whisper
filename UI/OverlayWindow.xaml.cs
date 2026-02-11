using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GroqWhisperPTT.UI;

public partial class OverlayWindow : Window
{
    public event Action<string>? CopyRequested;
    public event Action? CloseRequested;
    public event Action? RetryRequested;

    public string TranscriptionText
    {
        get => TranscriptionTextBox.Text;
        set => TranscriptionTextBox.Text = value;
    }

    public OverlayWindow()
    {
        InitializeComponent();
    }

    private void PositionNearCursor()
    {
        var cursorPos = GetCursorPosition();

        // Get DPI scale for the monitor where the cursor is located
        double dpiScaleX = 1.0;
        double dpiScaleY = 1.0;
        var cursorPoint = new POINT { X = (int)cursorPos.X, Y = (int)cursorPos.Y };
        var monitor = MonitorFromPoint(cursorPoint, 2);
        if (GetDpiForMonitor(monitor, 0, out uint dpiX, out uint dpiY) == 0)
        {
            dpiScaleX = dpiX / 96.0;
            dpiScaleY = dpiY / 96.0;
        }

        // Get work area of the monitor where the cursor is (physical pixels)
        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(monitor, ref mi);

        // Convert everything to WPF DIPs using the same DPI scale
        double cursorX = cursorPos.X / dpiScaleX;
        double cursorY = cursorPos.Y / dpiScaleY;
        double workLeft = mi.rcWork.Left / dpiScaleX;
        double workTop = mi.rcWork.Top / dpiScaleY;
        double workRight = mi.rcWork.Right / dpiScaleX;
        double workBottom = mi.rcWork.Bottom / dpiScaleY;

        double left = cursorX + 16;
        double top = cursorY + 16;

        // Clamp to the current monitor's work area
        if (left + Width > workRight)
            left = workRight - Width - 16;
        if (top + Height > workBottom)
            top = workBottom - Height - 16;
        if (left < workLeft)
            left = workLeft + 16;
        if (top < workTop)
            top = workTop + 16;

        Left = left;
        Top = top;
    }

    public void ShowRecording()
    {
        StatusText.Text = "● Recording…";
        StatusText.Foreground = new SolidColorBrush(Colors.Red);
        
        RecordingPanel.Visibility = Visibility.Visible;
        TranscribingPanel.Visibility = Visibility.Collapsed;
        TranscriptionTextBox.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Collapsed;
        
        CopyButton.Visibility = Visibility.Collapsed;
        CloseButton.Content = "Cancel (Esc)";

        PositionNearCursor();
        Show();
        Activate();
    }

    public void ShowTranscribing()
    {
        StatusText.Text = "Transcribing…";
        StatusText.Foreground = new SolidColorBrush(Colors.Gray);

        RecordingPanel.Visibility = Visibility.Collapsed;
        TranscribingPanel.Visibility = Visibility.Visible;
        TranscriptionTextBox.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Collapsed;

        CopyButton.Visibility = Visibility.Collapsed;
        CloseButton.Content = "Cancel (Esc)";
    }

    public void ShowEditing(string text)
    {
        StatusText.Text = "Transcription Ready";
        StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 204));

        RecordingPanel.Visibility = Visibility.Collapsed;
        TranscribingPanel.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Collapsed;
        
        TranscriptionTextBox.Text = text;
        TranscriptionTextBox.Visibility = Visibility.Visible;
        TranscriptionTextBox.Focus();
        TranscriptionTextBox.SelectAll();

        CopyButton.Visibility = Visibility.Visible;
        CloseButton.Content = "Close (Esc)";

        // Auto resize to fit content
        SizeToContent = SizeToContent.Height;
    }

    public void ShowError(string message)
    {
        StatusText.Text = "Error";
        StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 107, 107));

        RecordingPanel.Visibility = Visibility.Collapsed;
        TranscribingPanel.Visibility = Visibility.Collapsed;
        TranscriptionTextBox.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Visible;

        ErrorText.Text = message;
        CopyButton.Visibility = Visibility.Collapsed;
        CloseButton.Content = "Close (Esc)";
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var text = TranscriptionTextBox.Text;
        CopyRequested?.Invoke(text);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke();
    }

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        RetryRequested?.Invoke();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseRequested?.Invoke();
        }
        else if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (CopyButton.Visibility == Visibility.Visible)
            {
                CopyButton_Click(sender, e);
            }
        }
    }

    #region Native Methods

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private static Point GetCursorPosition()
    {
        GetCursorPos(out POINT pt);
        return new Point(pt.X, pt.Y);
    }

    #endregion
}
