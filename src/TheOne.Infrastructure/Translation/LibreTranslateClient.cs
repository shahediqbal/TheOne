using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TheOne.Infrastructure.Translation;

/// <summary>Calls a self-hosted LibreTranslate instance. CPU-only, no per-character billing.</summary>
public sealed class LibreTranslateClient(HttpClient http)
{
    /// <summary>Requests a translation from the sidecar container.</summary>
    public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/translate", new
        {
            q = text,
            source = sourceLang,
            target = targetLang,
            format = "text"
        }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LibreTranslateResponse>(cancellationToken: ct);
        return result?.TranslatedText ?? string.Empty;
    }

    private sealed class LibreTranslateResponse
    {
        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; } = string.Empty;
    }
}
