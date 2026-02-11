using System.IO;
using System.Windows;
using GroqWhisperPTT.Audio;
using GroqWhisperPTT.Core;
using GroqWhisperPTT.Input;
using GroqWhisperPTT.STT;
using GroqWhisperPTT.UI;
using GroqWhisperPTT.Util;

namespace GroqWhisperPTT;

public partial class App : Application
{
    private HotkeyHook _hotkeyHook = null!;
    private AudioRecorder _audioRecorder = null!;
    private GroqTranscriptionService _transcriptionService = null!;
    private AppStateMachine _stateMachine = null!;
    private OverlayWindow? _overlayWindow;
    
    private string? _currentAudioFile;
    private CancellationTokenSource? _transcriptionCts;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load .env file if exists
        var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                    continue;
                var sep = trimmed.IndexOf('=');
                if (sep <= 0) continue;
                var key = trimmed[..sep].Trim();
                var value = trimmed[(sep + 1)..].Trim();
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        // Check for API key
        var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            MessageBox.Show(
                "GROQ_API_KEY environment variable is not set.\n\n" +
                "Please set it before running the application:\n" +
                "[PowerShell] $env:GROQ_API_KEY = \"your-api-key\"\n" +
                "[CMD] set GROQ_API_KEY=your-api-key",
                "Configuration Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        InitializeServices();
        InitializeStateMachine();
        _hotkeyHook.Start();

    }

    private void InitializeServices()
    {
        _hotkeyHook = new HotkeyHook();
        _hotkeyHook.HotkeyPressed += OnHotkeyPressed;
        _hotkeyHook.HotkeyReleased += OnHotkeyReleased;

        _audioRecorder = new AudioRecorder();
        _transcriptionService = new GroqTranscriptionService();
        _stateMachine = new AppStateMachine();
    }

    private void InitializeStateMachine()
    {
        _stateMachine.StateChanged += OnStateChanged;
        _stateMachine.TranscriptionReady += OnTranscriptionReady;
        _stateMachine.ErrorOccurred += OnErrorOccurred;
    }

    private void OnHotkeyPressed()
    {
        if (_stateMachine.CurrentState != AppState.Idle)
            return;

        _stateMachine.HandleEvent(AppEvent.HotkeyDown);
    }

    private void OnHotkeyReleased()
    {
        if (_stateMachine.CurrentState != AppState.Recording)
            return;

        _stateMachine.HandleEvent(AppEvent.HotkeyUp);
    }

    private void OnStateChanged(AppState state)
    {
        Dispatcher.Invoke(async () =>
        {
            switch (state)
            {
                case AppState.Recording:
                    StartRecording();
                    break;

                case AppState.Finalizing:
                    await FinalizeRecordingAsync();
                    break;

                case AppState.Idle:
                    HideOverlay();
                    break;
            }
        });
    }

    private void StartRecording()
    {
        try
        {
            _currentAudioFile = _audioRecorder.StartRecording();
            ShowOverlay().ShowRecording();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start recording: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            _stateMachine.Reset();
        }
    }

    private async Task FinalizeRecordingAsync()
    {
        try
        {
            _currentAudioFile = _audioRecorder.StopRecording();

            // Check minimum duration
            if (_audioRecorder.RecordedDuration.TotalMilliseconds < 300)
            {
                TempFileCleaner.Cleanup(_currentAudioFile);
                _stateMachine.HandleEvent(AppEvent.Cancel);
                return;
            }

            ShowOverlay().ShowTranscribing();

            _transcriptionCts = new CancellationTokenSource();
            
            try
            {
                var text = await _transcriptionService.TranscribeAsync(_currentAudioFile!, _transcriptionCts.Token);
                _stateMachine.HandleEvent(AppEvent.TranscribeSuccess, text);
            }
            catch (OperationCanceledException)
            {
                _stateMachine.HandleEvent(AppEvent.Cancel);
            }
            catch (Exception ex)
            {
                _stateMachine.HandleEvent(AppEvent.TranscribeFail, ex.Message);
            }
            finally
            {
                TempFileCleaner.Cleanup(_currentAudioFile);
                _currentAudioFile = null;
            }
        }
        catch (Exception ex)
        {
            _stateMachine.HandleEvent(AppEvent.TranscribeFail, ex.Message);
        }
    }

    private void OnTranscriptionReady(string text)
    {
        Dispatcher.Invoke(() =>
        {
            ShowOverlay().ShowEditing(text);
        });
    }

    private void OnErrorOccurred(string error)
    {
        Dispatcher.Invoke(() =>
        {
            ShowOverlay().ShowError(error);
        });
    }

    private OverlayWindow ShowOverlay()
    {
        if (_overlayWindow == null || !_overlayWindow.IsLoaded)
        {
            _overlayWindow = new OverlayWindow();
            _overlayWindow.CopyRequested += OnCopyRequested;
            _overlayWindow.CloseRequested += OnCloseRequested;
            _overlayWindow.RetryRequested += OnRetryRequested;
        }
        return _overlayWindow;
    }

    private void HideOverlay()
    {
        _transcriptionCts?.Cancel();
        _overlayWindow?.Close();
        _overlayWindow = null;
    }

    private void OnCopyRequested(string text)
    {
        ClipboardService.CopyToClipboard(text);
    }

    private void OnCloseRequested()
    {
        if (_stateMachine.CurrentState == AppState.Recording)
        {
            _audioRecorder.StopRecording();
            TempFileCleaner.Cleanup(_currentAudioFile);
            _currentAudioFile = null;
        }
        
        _transcriptionCts?.Cancel();
        _stateMachine.HandleEvent(AppEvent.Cancel);
    }

    private void OnRetryRequested()
    {
        _stateMachine.Reset();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _transcriptionCts?.Cancel();
        _hotkeyHook?.Dispose();
        _audioRecorder?.Dispose();
        
        if (_currentAudioFile != null)
        {
            TempFileCleaner.Cleanup(_currentAudioFile);
        }

        base.OnExit(e);
    }
}
