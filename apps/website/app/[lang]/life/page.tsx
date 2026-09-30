import { FamilyTree } from "../../../components/site/FamilyTree";
import { introduction } from "../../../../../packages/website/starter-content";
import type { Language, LineageNodeFields } from "../../../lib/website-api";
import { resolveFixedSlugPage, listAllWebsiteRecords, buildLineageTree } from "../../../lib/website-api";
import { pageMetadata } from "../../../lib/seo";
import { CmsPageBody } from "../../../components/site/CmsPageBody";
import { LineageTree } from "../../../components/site/LineageTree";
import { TruncatedNotice } from "../../../components/site/TruncatedNotice";

export function generateStaticParams() {
  return [{ lang: "bn" }, { lang: "en" }];
}

export async function generateMetadata({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const language = lang as Language;
  try {
    const { record } = await resolveFixedSlugPage("Page", language, "life");
    return pageMetadata(language, record, heading[language].title, heading[language].empty, `/${language}/life`);
  } catch {
    return { title: heading[language].title, description: heading[language].empty };
  }
}

const heading: Record<Language, { title: string; lineage: string; empty: string }> = {
  en: {
    title: "Mawla's Life & Teachings",
    lineage: "Additional published lineage",
    empty: "Biography content is being prepared and will appear here once published.",
  },
  bn: {
    title: "মাওলার জীবন ও শিক্ষা",
    lineage: "প্রকাশিত পরম্পরার আরও তথ্য",
    empty: "জীবনী প্রকাশিত হলে এখানে প্রদর্শিত হবে।",
  },
};

export default async function LifePage({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const language = lang as Language;
  const copy = heading[language];

  // Primary content: a genuine failure propagates (real 500), not caught here — see Home for why.
  const { record: page, isFallback } = await resolveFixedSlugPage("Page", language, "life");
  // The lineage tree is a secondary, supplementary section: if it fails, the biography above
  // still renders rather than failing the whole page — a real service outage on this one
  // secondary piece isn't worth taking down primary content the visitor came for.
  const { records: lineageRecords, truncated } = await listAllWebsiteRecords<LineageNodeFields>(
    "LineageNode",
    language,
  ).catch(() => ({ records: [], truncated: false }));

  const tree = buildLineageTree(lineageRecords);

  return (
    <div className="home-section" style={{ marginTop: 40 }}>
      {page ? <CmsPageBody
        language={language}
        page={page}
        isFallback={isFallback}
        fallbackHref={page ? `/${page.language}/life` : null}
        fallbackTitle={copy.title}
        emptyMessage={copy.empty}
      /> : <section className="heritage-section"><div><p className="eyebrow">{language === "en" ? "LIFE & TEACHINGS" : "জীবন ও শিক্ষা"}</p><h1>{introduction[language].name}</h1><p>{introduction[language].biography}</p></div><figure className="editorial-portrait"><img src="/images/mawla-portrait.jpg" alt={introduction[language].name} /><figcaption>{language === "en" ? "1914 — 2006" : "১৯১৪ — ২০০৬"}</figcaption></figure></section>}

      <FamilyTree language={language} />

      {tree.length > 0 && (
        <section style={{ marginTop: 48 }}>
          <h2 style={{ fontFamily: "var(--font-display)", fontSize: "1.6rem", color: "var(--ink)" }}>
            {copy.lineage}
          </h2>
          {truncated && <TruncatedNotice language={language} />}
          <LineageTree nodes={tree} />
        </section>
      )}
    </div>
  );
}
