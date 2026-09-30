import type { Language, MenuItem } from "../../lib/website-api";
import { getPublicMenu, buildMenuTree } from "../../lib/website-api";
import { LanguageSwitcher } from "./LanguageSwitcher";
import { ResponsiveNav } from "./ResponsiveNav";
import { portalUrl } from "../../../../packages/website/portal";
import { NavItem } from "./NavItem";

/**
 * Used only if staff haven't published a "main" PublicMenu record yet (e.g. immediately after
 * launch). Mirrors the navigation agreed on for the public site: Home is the wordmark itself.
 */
const fallbackNav: Record<Language, MenuItem[]> = {
  en: [
    { key: "life", label: "Mawla's Life & Teachings", href: "/en/life" },
    { key: "organization", label: "Organization", href: "/en/organization" },
    { key: "books", label: "Books & Reading", href: "/en/books" },
    { key: "blog", label: "Blog", href: "/en/blog" },
    { key: "media", label: "Media", href: "/en/media" },
    { key: "programmes", label: "Programmes & Places", href: "/en/programmes" },
    { key: "membership", label: "Membership", href: "/en/membership" },
    { key: "contact", label: "Contact", href: "/en/contact" },
  ],
  bn: [
    { key: "life", label: "মাওলার জীবন ও শিক্ষা", href: "/bn/life" },
    { key: "organization", label: "প্রতিষ্ঠান", href: "/bn/organization" },
    { key: "books", label: "বই ও পাঠ", href: "/bn/books" },
    { key: "blog", label: "ব্লগ", href: "/bn/blog" },
    { key: "media", label: "মিডিয়া", href: "/bn/media" },
    { key: "programmes", label: "অনুষ্ঠান ও স্থান", href: "/bn/programmes" },
    { key: "membership", label: "সদস্যপদ", href: "/bn/membership" },
    { key: "contact", label: "যোগাযোগ", href: "/bn/contact" },
  ],
};

const wordmark: Record<Language, string> = { en: "Sadria Society", bn: "সাদরিয়া সোসাইটি" };

export async function Header({ language }: { language: Language }) {
  let items: MenuItem[] = fallbackNav[language];
  try {
    const menu = await getPublicMenu(language, "main");
    if (menu?.document.fields.items?.length) {
      items = menu.document.fields.items;
    }
  } catch {
    // Content service unreachable: fall back to the hardcoded nav rather than breaking every page.
  }
  const tree = buildMenuTree(items);

  const staffUrl = portalUrl(process.env.MANAGEMENT_PUBLIC_ORIGIN, process.env.NODE_ENV === "development" ? "https://localhost:5173" : "/staff");
  return (
    <header className="site-header">
      <a href={`/${language}`} className="site-wordmark">
        {wordmark[language]}
      </a>
      <ResponsiveNav language={language}>
        {tree.map((item) => (
          <NavItem key={item.key} item={item} depth={0} />
        ))}
      </ResponsiveNav>
      <div className="header-tools"><LanguageSwitcher current={language} />
      <a className="staff-login" href={staffUrl}>{language === "en" ? "Staff login" : "স্টাফ লগইন"} <span aria-hidden="true">↗</span></a></div>
    </header>
  );
}
