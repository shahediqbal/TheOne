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
    lineage: "The Lineage",
    empty: "Biography content is being prepared and will appear here once published.",
  },
  bn: {
    title: "মাওলার জীবন ও শিক্ষা",
    lineage: "বংশপরম্পরা",
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
      <CmsPageBody
        language={language}
        page={page}
        isFallback={isFallback}
        fallbackHref={page ? `/${page.language}/life` : null}
        fallbackTitle={copy.title}
        emptyMessage={copy.empty}
      />

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
