namespace GroqWhisperPTT.Core;

public enum AppState
{
    Idle,
    Recording,
    Finalizing,
    Editing,
    Error
}

public enum AppEvent
{
    HotkeyDown,
    HotkeyUp,
    TranscribeSuccess,
    TranscribeFail,
    Cancel,
    CopyDone
}

public class TranscriptionResult
{
    public string Text { get; set; } = string.Empty;
}

public class TranscriptionError
{
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}
