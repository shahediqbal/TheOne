export type Block = {
  type:
    | "Paragraph"
    | "Heading"
    | "Image"
    | "Gallery"
    | "Quote"
    | "AudioEmbed"
    | "PullQuote"
    | "RelatedPosts";
  text?: string;
  url?: string;
  alt?: string;
  level?: number;
  citation?: string;
  images?: { url: string; alt: string; caption?: string }[];
  postIds?: string[];
};
export type BlogDocument = {
  title: string;
  slug: string;
  summary: string;
  seoTitle: string;
  seoDescription: string;
  blocks: Block[];
  tags: string[];
  authorIds?: string[];
  provenance: "human" | "ai_assisted_draft" | "ai_assisted_reviewed";
};
export type BlogView = {
  id: string;
  postId: string;
  language: string;
  status: number;
  version: number;
  document: BlogDocument;
  publishedRevisionId: string | null;
  approvedBy: string | null;
  approvedAtUtc: string | null;
  publishedAtUtc: string | null;
};
export type PublicBlog = {
  postId: string;
  language: string;
  document: BlogDocument;
  publishedAtUtc: string;
  otherLanguageSlug: string | null;
};
export const states = [
  "Draft",
  "In review",
  "Approved",
  "Published",
  "Archived",
];
export const blockTypes = [
  "Paragraph",
  "Heading",
  "Image",
  "Gallery",
  "Quote",
  "AudioEmbed",
  "PullQuote",
  "RelatedPosts",
] as const;
export function safeUrl(url?: string) {
  if (!url) return false;
  try {
    const parsed = new URL(url);
    return parsed.protocol === "https:" && !parsed.username && !parsed.password;
  } catch {
    return false;
  }
}
