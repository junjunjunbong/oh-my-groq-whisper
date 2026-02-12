using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GroqWhisperPTT.STT;

public class GroqTranscriptionService
{
    private const string ApiUrl = "https://api.groq.com/openai/v1/audio/transcriptions";
    private const string Model = "whisper-large-v3";
    private const string DefaultPrompt =
        "코딩 작업을 진행합니다. 이 함수에서 async await 패턴으로 refactoring하고, "
        + "React component에 useState hook 추가해줘. TypeScript interface를 "
        + "export해서 API response type으로 사용할게. dotnet build 실행하고 npm install "
        + "한 다음에 git commit 해줘. Python에서 FastAPI endpoint 만들고, Docker container "
        + "설정도 해야 해. className은 camelCase로, variable은 snake_case로 네이밍 해줘. "
        + "Next.js에서 getServerSideProps 구현하고 Tailwind CSS로 styling 할게. "
        + "database migration 실행하고 SQL query optimize 해줘. "
        + "GitHub PR review 하고 merge conflict resolve 해줘. "
        + "Claude Code한테 프롬프트 작성해서 refactor 시키고, Copilot suggestion 수락해. "
        + "Cursor에서 Gemini로 코드 생성하고 ChatGPT한테 에러 물어봐. "
        + "Codex로 autocomplete 하고 컨텍스트 넘겨줘.";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _prompt;

    public GroqTranscriptionService()
    {
        _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
            ?? throw new InvalidOperationException("GROQ_API_KEY environment variable is not set.");
        _prompt = Environment.GetEnvironmentVariable("GROQ_WHISPER_PROMPT") ?? DefaultPrompt;

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

        // Add prompt hint for mixed Korean/English programming recognition
        content.Add(new StringContent(_prompt), "prompt");

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
