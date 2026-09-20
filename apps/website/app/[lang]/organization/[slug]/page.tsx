import { notFound, permanentRedirect } from "next/navigation";
import type { Language, PublicWebsiteRecord } from "../../../../lib/website-api";
import { getWebsiteRecord, otherLanguageHref } from "../../../../lib/website-api";
import { pageMetadata } from "../../../../lib/seo";
import { CmsPageBody } from "../../../../components/site/CmsPageBody";

const emptyMessage: Record<Language, string> = {
  en: "This page is being prepared and will appear here once published.",
  bn: "এই পাতাটি প্রস্তুত করা হচ্ছে, প্রকাশিত হলে এখানে দেখা যাবে।",
};

const readInOther: Record<Language, string> = { en: "বাংলায় পড়ুন", bn: "Read in English" };

type Props = { params: Promise<{ lang: string; slug: string }> };
type ReadResult =
  | { status: "ok"; language: Language; slug: string; page: PublicWebsiteRecord }
  | { status: "not-found" };

/**
 * Loads the record and enforces the "organization" tag boundary. A genuine service failure is
 * deliberately NOT caught here — it propagates to a real HTTP 500, same reasoning as every other
 * page. Only the not-found/tag-mismatch case is distinguished here, since getWebsiteRecord
 * already returns null (not a throw) for a genuine 404 — no ambiguity to resolve for that case.
 */
async function read(params: Props["params"]): Promise<ReadResult> {
  const { lang, slug } = await params;
  if (lang !== "bn" && lang !== "en") return { status: "not-found" };
  const language = lang as Language;
  const page = await getWebsiteRecord("Page", language, slug);
  if (!page || !page.document.content.tags?.includes("organization")) return { status: "not-found" };
  return { status: "ok", language, slug, page };
}

export async function generateMetadata({ params }: Props) {
  const result = await read(params);
  if (result.status !== "ok") return { title: "Sadria Society" };
  return pageMetadata(
    result.language,
    result.page,
    result.page.document.content.title,
    emptyMessage[result.language],
    `/${result.language}/organization/${result.page.document.content.slug}`,
  );
}

export default async function OrganizationDetail({ params }: Props) {
  const result = await read(params);
  if (result.status === "not-found") notFound();

  const { language, slug, page } = result;

  // Alias redirect: the requested slug matched via an older alias, not the record's current slug.
  if (slug !== page.document.content.slug) {
    permanentRedirect(`/${language}/organization/${encodeURIComponent(page.document.content.slug)}`);
  }

  const otherHref = otherLanguageHref(page, "/organization");

  return (
    <div className="home-section" style={{ marginTop: 40 }}>
      {otherHref ? (
        <p style={{ marginBottom: 12 }}>
          <a href={otherHref} style={{ color: "var(--brown)", fontSize: "0.9rem" }}>
            {readInOther[language]} →
          </a>
        </p>
      ) : null}
      <CmsPageBody
        language={language}
        page={page}
        isFallback={false}
        fallbackHref={null}
        fallbackTitle={slug}
        emptyMessage={emptyMessage[language]}
      />
    </div>
  );
}
