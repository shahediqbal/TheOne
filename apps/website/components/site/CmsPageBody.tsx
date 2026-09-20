import type { Language, PublicWebsiteRecord } from "../../lib/website-api";
import { MissingTranslationBanner } from "./MissingTranslationBanner";
import BlogBlocks from "../BlogBlocks";

/**
 * Shared shape for rendering any `Page`-kind CMS record as a standalone page. `isFallback` must
 * be computed by the caller using the real fallback resolution (see resolveFixedSlugPage) —
 * comparing `page.language !== language` here is not meaningful, because the plain slug lookup
 * is language-scoped and never itself returns a cross-language record.
 */
export function CmsPageBody({
  language,
  page,
  isFallback,
  fallbackHref,
  fallbackTitle,
  emptyMessage,
}: {
  language: Language;
  page: PublicWebsiteRecord | null;
  isFallback: boolean;
  fallbackHref: string | null;
  fallbackTitle: string;
  emptyMessage: string;
}) {
  return (
    <>
      {isFallback && fallbackHref && (
        <MissingTranslationBanner requestedLanguage={language} fallbackHref={fallbackHref} />
      )}
      <h1 style={{ fontFamily: "var(--font-display)", fontSize: "2.2rem", color: "var(--ink)" }}>
        {page?.document.content.title ?? fallbackTitle}
      </h1>
      {page?.document.content.blocks?.length ? (
        <BlogBlocks blocks={page.document.content.blocks} language={language} />
      ) : (
        <p style={{ color: "#6b6b66" }}>{emptyMessage}</p>
      )}
    </>
  );
}
