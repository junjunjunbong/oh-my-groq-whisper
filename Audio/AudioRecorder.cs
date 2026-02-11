using System.IO;
using NAudio.Wave;

namespace GroqWhisperPTT.Audio;

public class AudioRecorder : IDisposable
{
    private WaveInEvent? _capture;
    private WaveFileWriter? _writer;
    private string? _outputPath;
    private readonly object _lock = new();
    private DateTime _startTime;
    private TimeSpan _recordedDuration;

    public TimeSpan RecordedDuration => _capture != null ? DateTime.Now - _startTime : _recordedDuration;

    public string? StartRecording()
    {
        lock (_lock)
        {
            StopRecording();

            _outputPath = Path.Combine(Path.GetTempPath(), $"groq_whisper_{Guid.NewGuid()}.wav");
            
            try
            {
                _capture = new WaveInEvent();
                _capture.WaveFormat = new WaveFormat(16000, 16, 1); // 16kHz, 16-bit, mono

                _writer = new WaveFileWriter(_outputPath, _capture.WaveFormat);

                _capture.DataAvailable += (s, e) =>
                {
                    _writer?.Write(e.Buffer, 0, e.BytesRecorded);
                };

                _capture.RecordingStopped += (s, e) =>
                {
                    _writer?.Dispose();
                    _writer = null;
                };

                _startTime = DateTime.Now;
                _capture.StartRecording();

                return _outputPath;
            }
            catch
            {
                Cleanup();
                throw;
            }
        }
    }

    public string? StopRecording()
    {
        lock (_lock)
        {
            var path = _outputPath;

            // Save duration before stopping (once _capture is null, it's lost)
            if (_capture != null)
                _recordedDuration = DateTime.Now - _startTime;

            _capture?.StopRecording();
            _capture?.Dispose();
            _capture = null;

            _writer?.Dispose();
            _writer = null;

            _outputPath = null;

            return path;
        }
    }

    public void Dispose()
    {
        StopRecording();
    }

    private void Cleanup()
    {
        try
        {
            _writer?.Dispose();
            _writer = null;

            _capture?.Dispose();
            _capture = null;

            if (_outputPath != null && File.Exists(_outputPath))
            {
                File.Delete(_outputPath);
            }
        }
        catch { }
    }
}
