using TheOne.Application.Abstractions.Translation;

namespace TheOne.Infrastructure.Translation;

/// <summary>
/// Routes each call to the engine matching its declared sensitivity: self-hosted LibreTranslate
/// for low-stakes free text, Google NMT for anything identity-adjacent. Names never reach either
/// engine; transliteration is a separate, manual-only concern handled entirely on the frontend.
/// </summary>
public sealed class TieredTranslationService(LibreTranslateClient libreTranslate, GoogleTranslateClient googleTranslate)
    : ITranslationService
{
    /// <inheritdoc />
    public Task<string> TranslateAsync(string text, string sourceLang, string targetLang, TranslationTier tier, CancellationToken ct) =>
        tier switch
        {
            TranslationTier.HighAccuracy => googleTranslate.TranslateAsync(text, sourceLang, targetLang, ct),
            _ => libreTranslate.TranslateAsync(text, sourceLang, targetLang, ct)
        };
}
