namespace GroqWhisperPTT.Core;

public class AppStateMachine
{
    public AppState CurrentState { get; private set; } = AppState.Idle;

    public event Action<AppState>? StateChanged;
    public event Action<string>? TranscriptionReady;
    public event Action<string>? ErrorOccurred;

    public void HandleEvent(AppEvent evt, object? data = null)
    {
        var prevState = CurrentState;

        CurrentState = (prevState, evt) switch
        {
            // Idle -> Recording (HotkeyDown)
            (AppState.Idle, AppEvent.HotkeyDown) => AppState.Recording,

            // Recording -> Finalizing (HotkeyUp)
            (AppState.Recording, AppEvent.HotkeyUp) => AppState.Finalizing,

            // Recording -> Idle (Cancel)
            (AppState.Recording, AppEvent.Cancel) => AppState.Idle,

            // Finalizing -> Editing (TranscribeSuccess)
            (AppState.Finalizing, AppEvent.TranscribeSuccess) => AppState.Editing,

            // Finalizing -> Error (TranscribeFail)
            (AppState.Finalizing, AppEvent.TranscribeFail) => AppState.Error,

            // Finalizing -> Idle (Cancel)
            (AppState.Finalizing, AppEvent.Cancel) => AppState.Idle,

            // Editing -> Idle (CopyDone or Cancel)
            (AppState.Editing, AppEvent.CopyDone) => AppState.Idle,
            (AppState.Editing, AppEvent.Cancel) => AppState.Idle,

            // Editing/Error -> Recording (HotkeyDown for new recording)
            (AppState.Editing, AppEvent.HotkeyDown) => AppState.Recording,
            (AppState.Error, AppEvent.HotkeyDown) => AppState.Recording,

            // Error -> Idle (Cancel or Retry)
            (AppState.Error, AppEvent.Cancel) => AppState.Idle,

            // Ignore invalid transitions
            _ => prevState
        };

        if (CurrentState != prevState)
        {
            StateChanged?.Invoke(CurrentState);
        }

        // Handle data events
        if (evt == AppEvent.TranscribeSuccess && data is string text)
        {
            TranscriptionReady?.Invoke(text);
        }
        else if (evt == AppEvent.TranscribeFail && data is string error)
        {
            ErrorOccurred?.Invoke(error);
        }
    }

    public void Reset()
    {
        CurrentState = AppState.Idle;
        StateChanged?.Invoke(CurrentState);
    }
}
