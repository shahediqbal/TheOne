import type { Language } from "../../lib/website-api";
import { getSiteSettings, resolveFixedSlugPage, assetUrl } from "../../lib/website-api";
import { pageMetadata } from "../../lib/seo";
import { MissingTranslationBanner } from "../../components/site/MissingTranslationBanner";
import { blogApi, type PublicBlog } from "../../components/blog-api";
import BlogBlocks from "../../components/BlogBlocks";

export function generateStaticParams() {
  return [{ lang: "bn" }, { lang: "en" }];
}

export async function generateMetadata({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const language = lang as Language;
  try {
    const { record } = await resolveFixedSlugPage("Page", language, "home");
    return pageMetadata(language, record, heroFallback[language].title, heroFallback[language].summary, `/${language}`);
  } catch {
    return { title: heroFallback[language].title, description: heroFallback[language].summary };
  }
}

const heroFallback: Record<Language, { title: string; summary: string }> = {
  en: {
    title: "A living spiritual and literary heritage",
    summary:
      "The life, teachings and written works of Mawla, and the ongoing work of the Sadria Society.",
  },
  bn: {
    title: "একটি জীবন্ত আধ্যাত্মিক ও সাহিত্যিক ঐতিহ্য",
    summary: "মাওলার জীবন, শিক্ষা ও লেখনী, এবং সাদরিয়া সোসাইটির চলমান কার্যক্রম।",
  },
};

const entries: Record<Language, { href: string; title: string; body: string; primary?: boolean }[]> = {
  en: [
    { href: "/en/books", title: "Read Quran Darshan", body: "The three-volume Bangla tafsir, being prepared for the reading archive.", primary: true },
    { href: "/en/life", title: "The Lineage", body: "Mawla's life, teachings and spiritual lineage." },
    { href: "/en/programmes", title: "Visit a Shrine", body: "Branches, shrines and upcoming programmes." },
  ],
  bn: [
    { href: "/bn/books", title: "কোরান দর্শন পড়ুন", body: "তিন খণ্ডের বাংলা তাফসির, পাঠ আর্কাইভের জন্য প্রস্তুত করা হচ্ছে।", primary: true },
    { href: "/bn/life", title: "বংশপরম্পরা", body: "মাওলার জীবন, শিক্ষা ও আধ্যাত্মিক ধারা।" },
    { href: "/bn/programmes", title: "মাজার পরিদর্শন", body: "শাখা, মাজার ও আসন্ন অনুষ্ঠান।" },
  ],
};

export default async function HomePage({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const language = lang as Language;

  // A genuine service failure (not a 404) is deliberately NOT caught here: letting it propagate
  // gives a real HTTP 500 via the route's error boundary, which matters for uptime monitoring
  // and so a crawler doesn't index a temporary failure as if it were real content. Only a 404
  // (already returned as null by resolveFixedSlugPage, never thrown) is a normal, non-error state.
  const { record: homePage, isFallback } = await resolveFixedSlugPage("Page", language, "home");
  const [settings, posts] = await Promise.all([
    getSiteSettings(language).catch(() => null),
    blogApi<PublicBlog[]>(`${language}?page=1`).catch(() => null),
  ]);

  const hero = homePage?.document.content ?? null;
  const heroImage = assetUrl(settings?.document.fields.heroAssetId);

  return (
    <>
      {isFallback && homePage && (
        <MissingTranslationBanner requestedLanguage={language} fallbackHref={`/${homePage.language}`} />
      )}

      <section
        className="home-hero"
        style={heroImage ? { backgroundImage: `url(${heroImage})` } : undefined}
      >
        <div className="home-hero-content">
          <h1>{hero?.title ?? heroFallback[language].title}</h1>
          <p>{hero?.summary ?? heroFallback[language].summary}</p>
        </div>
      </section>

      <div className="home-entries">
        {entries[language].map((entry) => (
          <a key={entry.href} href={entry.href} className={`home-entry${entry.primary ? " primary" : ""}`}>
            <h3>{entry.title}</h3>
            <p>{entry.body}</p>
          </a>
        ))}
      </div>

      {hero?.blocks?.length ? (
        <section className="home-section">
          <h2>{language === "en" ? "Selected teachings" : "নির্বাচিত শিক্ষা"}</h2>
          <BlogBlocks blocks={hero.blocks} language={language} />
        </section>
      ) : null}

      {posts?.length ? (
        <section className="home-section">
          <h2>{language === "en" ? "From the blog" : "ব্লগ থেকে"}</h2>
          <div className="blog-index">
            {posts.slice(0, 3).map((post) => (
              <article key={post.postId}>
                <h2>
                  <a href={`/${language}/blog/${post.document.slug}`}>{post.document.title}</a>
                </h2>
                {post.document.summary ? <p className="blog-summary">{post.document.summary}</p> : null}
              </article>
            ))}
          </div>
        </section>
      ) : null}
    </>
  );
}
