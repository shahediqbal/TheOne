import type { Language, SiteSettingsFields } from "../../lib/website-api";
import { getSiteSettings, getPublicMenu } from "../../lib/website-api";

const fallback: Record<Language, { org: string; note: string }> = {
  en: { org: "Sadria Society", note: "Content is being prepared. Contact details will appear here once published." },
  bn: { org: "সাদরিয়া সোসাইটি", note: "বিষয়বস্তু প্রস্তুত করা হচ্ছে। প্রকাশিত হলে যোগাযোগের বিবরণ এখানে প্রদর্শিত হবে।" },
};

export async function Footer({ language }: { language: Language }) {
  // Each request is handled independently: one failing must not discard the other's result.
  const [settingsResult, menuResult] = await Promise.allSettled([
    getSiteSettings(language),
    getPublicMenu(language, "footer"),
  ]);
  const settings = settingsResult.status === "fulfilled" ? settingsResult.value : null;
  const footerMenu = menuResult.status === "fulfilled" ? menuResult.value : null;
  const fields = settings?.document.fields as SiteSettingsFields | undefined;
  const footerLinks = footerMenu?.document.fields.items ?? [];
  const year = new Date().getFullYear();

  return (
    <footer className="site-footer">
      <div className="site-footer-inner">
        <div>
          <h2>{fields?.organizationName ?? fallback[language].org}</h2>
          <p>{fields?.footerText ?? fallback[language].note}</p>
          {fields?.socialLinks?.length ? (
            <div className="site-footer-social">
              {fields.socialLinks.map((link) => (
                <a key={link.href} href={link.href} rel="noopener noreferrer" target="_blank">
                  {link.label}
                </a>
              ))}
            </div>
          ) : null}
        </div>
        <div>
          <h2>{language === "en" ? "Contact" : "যোগাযোগ"}</h2>
          {fields?.address ? <p>{fields.address}</p> : null}
          {fields?.phone ? <p>{fields.phone}</p> : null}
          {fields?.contactEmail ? (
            <p>
              <a href={`mailto:${fields.contactEmail}`}>{fields.contactEmail}</a>
            </p>
          ) : null}
        </div>
        {footerLinks.length > 0 ? (
          <div>
            <h2>{language === "en" ? "Links" : "লিংক"}</h2>
            <div className="site-footer-links">
              {footerLinks.map((link) => (
                <a key={link.key} href={link.href}>
                  {link.label}
                </a>
              ))}
            </div>
          </div>
        ) : null}
      </div>
      <p className="site-footer-copyright">
        © {year} {fields?.organizationName ?? fallback[language].org}
      </p>
    </footer>
  );
}
