import type { Language } from "../../lib/website-api";

const copy: Record<Language, { message: string; linkLabel: string }> = {
  en: { message: "This page isn't available in English yet.", linkLabel: "Read it in Bangla" },
  bn: { message: "এই পাতাটি এখনও বাংলায় প্রস্তুত নয়।", linkLabel: "ইংরেজিতে পড়ুন" },
};

/**
 * Shown whenever a requested page's language doesn't match what's actually published —
 * the site-wide policy: never hide content, never silently swap languages, always link across.
 */
export function MissingTranslationBanner({
  requestedLanguage,
  fallbackHref,
}: {
  requestedLanguage: Language;
  fallbackHref: string;
}) {
  const text = copy[requestedLanguage];
  return (
    <div className="missing-translation" role="status">
      {text.message} <a href={fallbackHref}>{text.linkLabel} →</a>
    </div>
  );
}
