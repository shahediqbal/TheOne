import type { Language } from "../../lib/website-api";

const copy: Record<Language, string> = {
  en: "Showing a partial list — there may be more content than is displayed here.",
  bn: "একটি আংশিক তালিকা দেখানো হচ্ছে — এখানে প্রদর্শিত চেয়ে আরও বিষয়বস্তু থাকতে পারে।",
};

/** Shown when listAllWebsiteRecords hit its safety cap — the visitor is told, not just the operator's logs. */
export function TruncatedNotice({ language }: { language: Language }) {
  return (
    <p style={{ color: "var(--brown)", fontSize: "0.85rem", marginBottom: 16 }} role="status">
      {copy[language]}
    </p>
  );
}
