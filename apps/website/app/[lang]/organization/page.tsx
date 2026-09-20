import type { Language } from "../../../lib/website-api";
import { listPagesByTag } from "../../../lib/website-api";
import { TruncatedNotice } from "../../../components/site/TruncatedNotice";
import type { Metadata } from "next";

export function generateStaticParams() {
  return [{ lang: "bn" }, { lang: "en" }];
}

export async function generateMetadata({ params }: { params: Promise<{ lang: string }> }): Promise<Metadata> {
  const { lang } = await params;
  const language = lang as Language;
  return { title: heading[language].title, description: heading[language].intro };
}

const heading: Record<Language, { title: string; intro: string }> = {
  en: {
    title: "Organization",
    intro: "The institutions carrying this work forward.",
  },
  bn: {
    title: "প্রতিষ্ঠান",
    intro: "এই কাজ এগিয়ে নিয়ে যাওয়া প্রতিষ্ঠানসমূহ।",
  },
};

/**
 * Shown only if staff haven't tagged any Page records "organization" yet (e.g. immediately
 * after launch). Slugs match the sub-organizations from the previous site's navigation, so
 * nothing from the old structure gets silently lost while content migrates.
 */
const fallbackEntries: Record<Language, { slug: string; title: string }[]> = {
  en: [
    { slug: "dorbar-institute", title: "Dorbar Institute" },
    { slug: "imamia-chistia-nezamia-sangha", title: "Imamia Chistia Nezamia Sangha" },
    { slug: "sadria-society", title: "Sadria Society" },
  ],
  bn: [
    { slug: "dorbar-institute", title: "দরবার ইনস্টিটিউট" },
    { slug: "imamia-chistia-nezamia-sangha", title: "ইমামিয়া চিশতিয়া নিজামিয়া সংঘ" },
    { slug: "sadria-society", title: "সাদরিয়া সোসাইটি" },
  ],
};

export default async function OrganizationIndex({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const language = lang as Language;
  const copy = heading[language];

  // A genuine service failure propagates (real 500) rather than being caught here — catching it
  // would otherwise mean silently falling back to the hardcoded list as if the CMS were merely
  // empty, hiding a real outage. Only the "genuinely empty, 200" case reaches the check below.
  const { pages: tagged, truncated } = await listPagesByTag(language, "organization");
  const entries = tagged.length
    ? tagged.map((p) => ({ slug: p.document.content.slug, title: p.document.content.title }))
    : fallbackEntries[language];

  return (
    <div className="home-section" style={{ marginTop: 40 }}>
      <h1 style={{ fontFamily: "var(--font-display)", fontSize: "2.2rem", color: "var(--ink)" }}>
        {copy.title}
      </h1>
      <p style={{ color: "#6b6b66", marginBottom: 32 }}>{copy.intro}</p>
      {truncated && <TruncatedNotice language={language} />}
      <div className="home-entries" style={{ margin: 0, gridTemplateColumns: "repeat(auto-fit, minmax(240px, 1fr))" }}>
        {entries.map((entry) => (
          <a key={entry.slug} href={`/${language}/organization/${entry.slug}`} className="home-entry">
            <h3>{entry.title}</h3>
          </a>
        ))}
      </div>
    </div>
  );
}
