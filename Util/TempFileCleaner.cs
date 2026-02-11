using System.IO;

namespace GroqWhisperPTT.Util;

public static class TempFileCleaner
{
    public static void Cleanup(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Best effort cleanup, ignore errors
        }
    }
}
