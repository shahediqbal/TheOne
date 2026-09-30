import { notFound } from "next/navigation";
import type { Language } from "../../lib/website-api";
import { Header } from "../../components/site/Header";
import { Footer } from "../../components/site/Footer";
import "../globals.css";

/**
 * This is the true root layout — there is deliberately no separate app/layout.tsx above it.
 * Next.js's own recommended i18n structure puts <html lang> in the [lang] segment's layout so
 * the correct language is set during server rendering, not corrected client-side after the fact
 * (which left English pages briefly marked as Bangla on first paint and permanently marked that
 * way with JavaScript disabled). The "/" redirect that used to live in a root app/page.tsx now
 * lives in next.config.ts's redirects(), so no page needs to exist outside this [lang] tree.
 */
export const metadata = {
  title: "Sadria Society",
  description:
    "The life, teachings and written works of Mawla, and the ongoing work of the Sadria Society.",
};

export function generateStaticParams() {
  return [{ lang: "bn" }, { lang: "en" }];
}

export default async function LangLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ lang: string }>;
}) {
  const { lang } = await params;
  if (lang !== "bn" && lang !== "en") notFound();
  const language = lang as Language;

  return (
    <html lang={language} data-lang={language}>
      <body>
        <a className="skip-link" href="#main-content">{language === "en" ? "Skip to content" : "মূল বিষয়বস্তুতে যান"}</a>
        <Header language={language} />
        <main id="main-content">{children}</main>
        <Footer language={language} />
      </body>
    </html>
  );
}
