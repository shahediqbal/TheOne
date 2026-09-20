import type { Language, MediaItemFields } from "../../../lib/website-api";
import { listAllWebsiteRecords } from "../../../lib/website-api";
import { MediaGallery } from "../../../components/site/MediaGallery";
import { TruncatedNotice } from "../../../components/site/TruncatedNotice";
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
  en: { title: "Media", intro: "Photographs, video and audio from the archive and ongoing programmes." },
  bn: { title: "মিডিয়া", intro: "আর্কাইভ ও চলমান কার্যক্রম থেকে আলোকচিত্র, ভিডিও ও অডিও।" },
} as const;

export default async function MediaPage({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const language = lang as Language;
  const t = copy[language];

  // A genuine service failure propagates (real 500) rather than being caught here.
  const { records: items, truncated } = await listAllWebsiteRecords<MediaItemFields>("MediaItem", language);

  return (
    <div className="home-section" style={{ marginTop: 40 }}>
      <h1 style={{ fontFamily: "var(--font-display)", fontSize: "2.2rem", color: "var(--ink)" }}>
        {t.title}
      </h1>
      <p style={{ color: "#6b6b66", marginBottom: 28 }}>{t.intro}</p>
      {truncated && <TruncatedNotice language={language} />}
      <MediaGallery language={language} items={items} />
    </div>
  );
}
