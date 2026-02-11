using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GroqWhisperPTT.STT;

public class GroqTranscriptionService
{
    private const string ApiUrl = "https://api.groq.com/openai/v1/audio/transcriptions";
    private const string Model = "whisper-large-v3-turbo";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GroqTranscriptionService()
    {
        _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") 
            ?? throw new InvalidOperationException("GROQ_API_KEY environment variable is not set.");
        
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(audioFilePath))
        {
            throw new FileNotFoundException("Audio file not found", audioFilePath);
        }

        using var content = new MultipartFormDataContent();
        
        // Add file
        var fileContent = new StreamContent(File.OpenRead(audioFilePath));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "file", Path.GetFileName(audioFilePath));

        // Add model
        content.Add(new StringContent(Model), "model");

        // Add language (auto-detect by not sending this parameter)
        // content.Add(new StringContent("ko"), "language");

        // Add response format
        content.Add(new StringContent("json"), "response_format");

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = content;

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorMessage;
            try
            {
                var errorResponse = JsonSerializer.Deserialize<GroqErrorResponse>(responseBody);
                errorMessage = errorResponse?.Error?.Message ?? $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            }
            catch
            {
                errorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            }
            throw new HttpRequestException(errorMessage);
        }

        var result = JsonSerializer.Deserialize<TranscriptionResponse>(responseBody);
        return result?.Text?.Trim() ?? string.Empty;
    }
}
