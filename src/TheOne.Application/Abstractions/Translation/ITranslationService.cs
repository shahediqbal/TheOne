namespace TheOne.Application.Abstractions.Translation;

/// <summary>
/// Which translation engine a field is routed to. Kept explicit per call so the routing decision
/// lives with the caller (who knows the field's sensitivity), not inside the translation service.
/// </summary>
public enum TranslationTier
{
    /// <summary>Low-stakes free text (e.g. purpose, life goal): self-hosted LibreTranslate.</summary>
    Standard = 0,
    /// <summary>Identity-sensitive text: higher-quality Google NMT. Names never use this either; they bypass translation entirely.</summary>
    HighAccuracy = 1
}

/// <summary>Suggests a translation for applicant review; never overwrites a user-corrected value.</summary>
public interface ITranslationService
{
    /// <summary>Translates <paramref name="text"/> from <paramref name="sourceLang"/> to <paramref name="targetLang"/> using the given tier.</summary>
    Task<string> TranslateAsync(string text, string sourceLang, string targetLang, TranslationTier tier, CancellationToken ct);
}
