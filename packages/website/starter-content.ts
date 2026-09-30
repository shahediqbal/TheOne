import type { BlogDocument } from "../blog/types";
export type SiteLanguage = "bn" | "en";
// Deliberately curated text, not a scrape of the legacy homepage or its external links.
export const introduction = {
  en: {title: "A life devoted to spiritual understanding", name: "Mawla Sadar Uddin Ahmad Chisty", summary: "Explore Mawla’s life, writings and teachings, and the community that carries this heritage forward.", biography: "Born in Chunkutia, Keraniganj, Dhaka, on 22 September 1914, Sadar Uddin Ahmad Chisty studied at Dhaka University. He passed away on 22 September 2006. His life and writings remain at the heart of this collection."},
  bn: {title: "আধ্যাত্মিক উপলব্ধিতে নিবেদিত এক জীবন", name: "মাওলা সদর উদ্দিন আহ্‌মদ চিশ্‌তী", summary: "মাওলার জীবন, লেখনী ও শিক্ষা এবং এই ঐতিহ্যকে ধারণ করা মানুষের সঙ্গে পরিচিত হোন।", biography: "১৯১৪ সালের ২২ সেপ্টেম্বর ঢাকার কেরানীগঞ্জের চুনকুটিয়ায় সদর উদ্দিন আহ্‌মদ চিশ্‌তীর জন্ম। তিনি ঢাকা বিশ্ববিদ্যালয়ে পড়াশোনা করেন। ২০০৬ সালের ২২ সেপ্টেম্বর তাঁর তিরোভাব হয়। তাঁর জীবন ও লেখনী এই সংগ্রহের মূল বিষয়।"}
};
export function starterPage(language: SiteLanguage, slug: "home" | "life"): BlogDocument {
  const text = introduction[language];
  return {
    title: slug === "home" ? text.title : text.name, slug,
    summary: slug === "home" ? text.summary : text.biography,
    seoTitle: "", seoDescription: "", tags: [], provenance: "ai_assisted_draft",
    blocks: [{type: "Paragraph", text: text.biography}]
  };
}
