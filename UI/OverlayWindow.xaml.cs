using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GroqWhisperPTT.Util;

namespace GroqWhisperPTT.UI;

public partial class OverlayWindow : Window
{
    public event Action<string>? CopyRequested;
    public event Action? CloseRequested;
    public event Action? RetryRequested;
    
    private WindowSettings _windowSettings;

    public string TranscriptionText
    {
        get => TranscriptionTextBox.Text;
        set => TranscriptionTextBox.Text = value;
    }

    public OverlayWindow(WindowSettings windowSettings)
    {
        InitializeComponent();
        _windowSettings = windowSettings;
        
        // Apply saved size
        Width = _windowSettings.Width;
        Height = _windowSettings.Height;
        
        // Calculate initial scale factor and apply font sizes
        _currentScaleFactor = Math.Clamp(Width / BaseWidth, 0.8, 1.5);
        
        // Subscribe to size changed event
        SizeChanged += OnSizeChanged;
        
        // Apply initial font sizes after layout is ready
        Loaded += (s, e) => UpdateFontSizes();
    }
    
    // Base font sizes (at width 400)
    private const double BaseStatusFontSize = 16;
    private const double BaseRecordingFontSize = 18;
    private const double BaseTranscribingFontSize = 18;
    private const double BaseTextBoxFontSize = 14;
    private const double BaseErrorFontSize = 14;
    private const double BaseButtonFontSize = 12;
    private const double BaseEllipseSize = 20;
    private const double BaseWidth = 400;
    
    private double _currentScaleFactor = 1.0;

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Save new size to settings
        _windowSettings.Width = Width;
        _windowSettings.Height = Height;
        SettingsManager.SaveWindowSettings(_windowSettings);
        
        // Calculate scale factor based on width
        _currentScaleFactor = Math.Clamp(Width / BaseWidth, 0.8, 1.5);
        
        // Apply scaled font sizes
        UpdateFontSizes();
    }
    
    private void UpdateFontSizes()
    {
        StatusText.FontSize = BaseStatusFontSize * _currentScaleFactor;
        RecordingText.FontSize = BaseRecordingFontSize * _currentScaleFactor;
        TranscribingText.FontSize = BaseTranscribingFontSize * _currentScaleFactor;
        TranscriptionTextBox.FontSize = BaseTextBoxFontSize * _currentScaleFactor;
        ErrorText.FontSize = BaseErrorFontSize * _currentScaleFactor;
        
        // Scale ellipse size
        RecordingDot.Width = BaseEllipseSize * _currentScaleFactor;
        RecordingDot.Height = BaseEllipseSize * _currentScaleFactor;
        
        // Scale button font sizes
        CopyButton.FontSize = BaseButtonFontSize * _currentScaleFactor;
        CloseButton.FontSize = BaseButtonFontSize * _currentScaleFactor;
        RetryButton.FontSize = BaseButtonFontSize * _currentScaleFactor;
        TitleCloseButton.FontSize = BaseButtonFontSize * _currentScaleFactor;
        
        // Adjust button padding based on scale
        double paddingH = 15 * _currentScaleFactor;
        double paddingV = 6 * _currentScaleFactor;
        var buttonPadding = new Thickness(paddingH, paddingV, paddingH, paddingV);
        CopyButton.Padding = buttonPadding;
        CloseButton.Padding = buttonPadding;
        RetryButton.Padding = new Thickness(paddingH, 5 * _currentScaleFactor, paddingH, 5 * _currentScaleFactor);
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

    private void TitleCloseButton_MouseEnter(object sender, MouseEventArgs e)
    {
        TitleCloseButton.Foreground = new SolidColorBrush(Colors.White);
    }

    private void TitleCloseButton_MouseLeave(object sender, MouseEventArgs e)
    {
        TitleCloseButton.Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204));
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
