"use client";

import { usePathname } from "next/navigation";
import type { Language } from "../../lib/website-api";
import { swapLanguage } from "../../lib/website-api";

/**
 * Preserves the current page when switching languages (e.g. staying on the same blog post's
 * other-language translation if it exists, or its own page's fallback route) rather than
 * bouncing the visitor back to the homepage — per the "language switching preserves context" principle.
 */
export function LanguageSwitcher({ current }: { current: Language }) {
  const pathname = usePathname() || "/";
  return (
    <nav className="lang-switch" aria-label="Language">
      {(["bn", "en"] as const).map((lang) => (
        <a
          key={lang}
          href={swapLanguage(pathname, lang)}
          aria-current={lang === current}
          lang={lang}
        >
          {lang === "bn" ? "বাংলা" : "EN"}
        </a>
      ))}
    </nav>
  );
}
