import type { Language, SiteSettingsFields } from "../../../lib/website-api";
import { resolveFixedSlugPage, getSiteSettings } from "../../../lib/website-api";
import { pageMetadata } from "../../../lib/seo";
import { CmsPageBody } from "../../../components/site/CmsPageBody";

export function generateStaticParams() {
  return [{ lang: "bn" }, { lang: "en" }];
}

export async function generateMetadata({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const language = lang as Language;
  try {
    const { record } = await resolveFixedSlugPage("Page", language, "contact");
    return pageMetadata(language, record, copy[language].title, copy[language].empty, `/${language}/contact`);
  } catch {
    return { title: copy[language].title, description: copy[language].empty };
  }
}

const copy = {
  en: {
    title: "Contact",
    empty: "Contact information is being prepared and will appear here once published.",
    address: "Address",
    phone: "Phone",
    email: "Email",
    follow: "Follow",
  },
  bn: {
    title: "যোগাযোগ",
    empty: "যোগাযোগের তথ্য প্রস্তুত করা হচ্ছে, প্রকাশিত হলে এখানে দেখা যাবে।",
    address: "ঠিকানা",
    phone: "ফোন",
    email: "ইমেইল",
    follow: "অনুসরণ করুন",
  },
} as const;

export default async function ContactPage({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  const language = lang as Language;
  const t = copy[language];

  let page, isFallback;
  ({ record: page, isFallback } = await resolveFixedSlugPage("Page", language, "contact"));
  const settings = await getSiteSettings(language).catch(() => null);
  const fields = settings?.document.fields as SiteSettingsFields | undefined;

  return (
    <div className="home-section" style={{ marginTop: 40 }}>
      <CmsPageBody
        language={language}
        page={page}
        isFallback={isFallback}
        fallbackHref={page ? `/${page.language}/contact` : null}
        fallbackTitle={t.title}
        emptyMessage={t.empty}
      />

      {fields && (fields.address || fields.phone || fields.contactEmail || fields.socialLinks?.length) ? (
        <div className="contact-details">
          {fields.address ? (
            <div>
              <h2>{t.address}</h2>
              <p>{fields.address}</p>
            </div>
          ) : null}
          {fields.phone ? (
            <div>
              <h2>{t.phone}</h2>
              <p>
                <a href={`tel:${fields.phone.replace(/\s+/g, "")}`}>{fields.phone}</a>
              </p>
            </div>
          ) : null}
          {fields.contactEmail ? (
            <div>
              <h2>{t.email}</h2>
              <p>
                <a href={`mailto:${fields.contactEmail}`}>{fields.contactEmail}</a>
              </p>
            </div>
          ) : null}
          {fields.socialLinks?.length ? (
            <div>
              <h2>{t.follow}</h2>
              <div className="site-footer-social">
                {fields.socialLinks.map((link) => (
                  <a key={link.href} href={link.href} target="_blank" rel="noopener noreferrer">
                    {link.label}
                  </a>
                ))}
              </div>
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
