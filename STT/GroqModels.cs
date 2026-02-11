using System.Text.Json.Serialization;

namespace GroqWhisperPTT.STT;

public class TranscriptionResponse
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class GroqErrorResponse
{
    [JsonPropertyName("error")]
    public GroqError? Error { get; set; }
}

public class GroqError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
