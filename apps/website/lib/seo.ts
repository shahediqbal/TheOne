import type { Metadata } from "next";
import type { Language, PublicWebsiteRecord } from "./website-api";

/**
 * Builds page metadata from a CMS record when present, falling back to hardcoded copy when the
 * record isn't published yet. Matches the canonical-URL convention already used by
 * `app/[lang]/blog/[slug]/page.tsx`, via the same WEBSITE_PUBLIC_ORIGIN env var.
 */
export function pageMetadata(
  language: Language,
  page: PublicWebsiteRecord | null,
  fallbackTitle: string,
  fallbackDescription: string,
  canonicalPath: string,
): Metadata {
  const origin = process.env.WEBSITE_PUBLIC_ORIGIN;
  const title = page?.document.content.seoTitle?.trim() || page?.document.content.title || fallbackTitle;
  const description =
    page?.document.content.seoDescription?.trim() || page?.document.content.summary || fallbackDescription;
  return {
    title,
    description,
    alternates: origin ? { canonical: new URL(canonicalPath, origin).href } : undefined,
    openGraph: { title, description },
  };
}
