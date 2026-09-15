using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace TheOne.Infrastructure.Translation;

/// <summary>Configures the Google Cloud Translation (NMT) API.</summary>
public sealed class GoogleTranslateOptions
{
    /// <summary>Gets or sets the API key supplied from secret configuration.</summary>
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the v2 REST endpoint.</summary>
    public string BaseUrl { get; set; } = "https://translation.googleapis.com/language/translate/v2";
}

/// <summary>Calls the Google Cloud Translation API. Reserved for identity-adjacent, high-accuracy fields.</summary>
public sealed class GoogleTranslateClient(HttpClient http, IOptions<GoogleTranslateOptions> options)
{
    /// <summary>Requests a translation from Google's NMT endpoint.</summary>
    public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang, CancellationToken ct)
    {
        var url = $"{options.Value.BaseUrl}?key={options.Value.ApiKey}";
        var response = await http.PostAsJsonAsync(url, new
        {
            q = text,
            source = sourceLang,
            target = targetLang,
            format = "text"
        }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GoogleTranslateResponse>(cancellationToken: ct);
        return result?.Data?.Translations?.FirstOrDefault()?.TranslatedText ?? string.Empty;
    }

    private sealed class GoogleTranslateResponse { public GoogleTranslateData? Data { get; set; } }
    private sealed class GoogleTranslateData { public List<GoogleTranslation>? Translations { get; set; } }
    private sealed class GoogleTranslation { public string TranslatedText { get; set; } = string.Empty; }
}
