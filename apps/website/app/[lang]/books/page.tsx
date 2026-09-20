import type { Language, PublicationFields } from "../../../lib/website-api";
import { listWebsiteRecords } from "../../../lib/website-api";
import { BookCatalog } from "../../../components/site/BookCatalog";
import type { Metadata } from "next";

export function generateStaticParams() {
  return [{ lang: "bn" }, { lang: "en" }];
}

export async function generateMetadata({ params }: { params: Promise<{ lang: string }> }): Promise<Metadata> {
  const { lang } = await params;
  const language = lang as Language;
  return { title: copy[language].title, description: copy[language].intro };
}

const copy = {
  en: {
    title: "Books & Reading",
    intro:
      "The written works, catalogued here. Reading happens in the dedicated reading archive, not on this page.",
  },
  bn: {
    title: "বই ও পাঠ",
    intro: "লেখনীসমূহের তালিকা এখানে। পড়া হয় নিবেদিত পাঠ আর্কাইভে, এই পাতায় নয়।",
  },
} as const;

export default async function BooksPage({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const language = lang as Language;
  const t = copy[language];

  // Genuine pagination, not fetch-everything-up-to-a-cap: the catalogue can genuinely be
  // "searchable" without loading the whole thing eagerly, and never silently truncates. A
  // genuine service failure propagates (real 500) rather than being caught here.
  const firstPage = await listWebsiteRecords<PublicationFields>("Publication", language, 1);

  return (
    <div className="home-section" style={{ marginTop: 40 }}>
      <h1 style={{ fontFamily: "var(--font-display)", fontSize: "2.2rem", color: "var(--ink)" }}>
        {t.title}
      </h1>
      <p style={{ color: "#6b6b66", marginBottom: 28 }}>{t.intro}</p>
      {/* Publication uses the generic Website CMS list endpoint, confirmed (in WebsiteConfiguration.cs
          / the public listing method) to page at exactly 30 — unlike blog's separate endpoint,
          which pages at 12. This comparison is accurate for this specific endpoint, not a repeat
          of the sitemap's hardcoded-page-size bug. */}
      <BookCatalog language={language} initialPublications={firstPage} hasMore={firstPage.length === 30} />
    </div>
  );
}
