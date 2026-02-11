using System.Windows;

namespace GroqWhisperPTT.Util;

public static class ClipboardService
{
    public static void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        try
        {
            Clipboard.SetText(text);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
