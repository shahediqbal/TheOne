import type { Block } from "../../../packages/blog/types";

export type Language = "bn" | "en";

/** A single content block, per the shared allowlist used by both blog and website bodies. */
export type ContentBlock = Block;

/** The common `content` shape every CMS kind shares, regardless of its typed `fields`. */
export type WebsiteContent = {
  title: string;
  slug: string;
  summary?: string;
  blocks: ContentBlock[];
  tags?: string[];
  seoTitle?: string;
  seoDescription?: string;
  provenance?: string;
};

export type WebsitePayload<TFields = Record<string, unknown>> = {
  content: WebsiteContent;
  fields: TFields;
};

/** A published, public-facing CMS record. Never includes staff identities or approval actors. */
export type PublicWebsiteRecord<TFields = Record<string, unknown>> = {
  canonicalId: string;
  kind: string;
  language: Language;
  document: WebsitePayload<TFields>;
  publishedAtUtc: string;
  /** The slug of this record in the other language, when a translation exists. */
  otherLanguageSlug?: string | null;
};

export type SocialLink = { label: string; href: string };

export type SiteSettingsFields = {
  organizationName: string;
  tagline?: string;
  contactEmail: string;
  phone: string;
  address: string;
  footerText?: string;
  socialLinks?: SocialLink[];
  heroAssetId?: string;
};

export type MenuItem = { key: string; label: string; href: string; parentKey?: string | null };

export type PublicMenuFields = { location: "main" | "footer"; items: MenuItem[] };

export type LineageNodeFields = {
  parentId?: string | null;
  order: number;
  lifeDates?: string;
  portraitAssetId?: string;
};

export type PlaceFields = {
  address: string;
  latitude: number;
  longitude: number;
  visitingHours?: string;
  contact?: string;
  imageAssetId?: string;
};

export type EventFields = {
  startsAt: string;
  endsAt: string;
  placeId?: string | null;
  venue?: string;
  contact?: string;
  cancelled?: boolean;
  hijriLabel?: string;
};

export type PublicationFields = {
  author: string;
  publicationYear?: number;
  coverAssetId?: string;
  readingUrl?: string;
  availability: "Preparing" | "Available";
  publisher?: string;
};

export type MediaType = "Photo" | "Video" | "Audio";

export type MediaItemFields = {
  mediaType: MediaType;
  assetId?: string;
  url?: string;
  alt: string;
  category?: string;
  credit?: string;
};

/** Distinct, non-empty category values present in a media list, for building the filter control. */
export function distinctMediaCategories(items: PublicWebsiteRecord<MediaItemFields>[]): string[] {
  const set = new Set<string>();
  for (const item of items) {
    const category = item.document.fields.category?.trim();
    if (category) set.add(category);
  }
  return [...set].sort((a, b) => a.localeCompare(b));
}

/**
 * Filters media by type and category. `null` means "no filter" for that dimension — a distinct
 * sentinel from any real value, since a category could legitimately be named "All". Category
 * comparison trims both sides, matching {@link distinctMediaCategories}'s own trimming, so a
 * stored value like " Branches " still matches the "Branches" option it produced.
 */
export function filterMedia(
  items: PublicWebsiteRecord<MediaItemFields>[],
  mediaType: MediaType | null,
  category: string | null,
): PublicWebsiteRecord<MediaItemFields>[] {
  return items.filter((item) => {
    const f = item.document.fields;
    if (mediaType !== null && f.mediaType !== mediaType) return false;
    if (category !== null && (f.category ?? "").trim() !== category) return false;
    return true;
  });
}

/** Client-side filter for the book catalogue: matches title or author, case-insensitively. */
export function filterPublications(
  publications: PublicWebsiteRecord<PublicationFields>[],
  query: string,
): PublicWebsiteRecord<PublicationFields>[] {
  const q = query.trim().toLowerCase();
  if (!q) return publications;
  return publications.filter(
    (p) =>
      p.document.content.title.toLowerCase().includes(q) ||
      p.document.fields.author.toLowerCase().includes(q),
  );
}

/** Sorts Event records chronologically by start time, earliest first. */
export function sortEventsByStart(
  events: PublicWebsiteRecord<EventFields>[],
): PublicWebsiteRecord<EventFields>[] {
  return [...events].sort(
    (a, b) => new Date(a.document.fields.startsAt).getTime() - new Date(b.document.fields.startsAt).getTime(),
  );
}

/** Builds a public URL for an approved CMS asset (image). Never resolves unpublished uploads. */
export function assetUrl(assetId?: string | null): string | undefined {
  return assetId ? `/api/v1/website/assets/${assetId}` : undefined;
}

function apiOrigin(): string {
  return process.env.MEMBERSHIP_API_ORIGIN || "https://localhost:7198";
}

/**
 * Fetches a single published record by kind/language/slug. Returns null on 404 (unpublished,
 * not yet translated, or genuinely absent) rather than throwing, so callers can render a
 * missing-translation banner instead of an error page.
 */
export async function getWebsiteRecord<TFields = Record<string, unknown>>(
  kind: string,
  language: Language,
  slug: string,
): Promise<PublicWebsiteRecord<TFields> | null> {
  const response = await fetch(
    new URL(`/api/v1/website/${kind}/${language}/${slug}`, apiOrigin()),
    { cache: "no-store" },
  );
  if (response.status === 404) return null;
  if (!response.ok) throw new Error("The content service is unavailable. Please try again.");
  return (await response.json()).data;
}

/** Same as {@link getWebsiteRecord}, but looks up by the language-independent canonical ID. */
export async function getWebsiteRecordByCanonicalId<TFields = Record<string, unknown>>(
  kind: string,
  language: Language,
  canonicalId: string,
): Promise<PublicWebsiteRecord<TFields> | null> {
  const response = await fetch(
    new URL(`/api/v1/website/${kind}/${language}/by-id/${canonicalId}`, apiOrigin()),
    { cache: "no-store" },
  );
  if (response.status === 404) return null;
  if (!response.ok) throw new Error("The content service is unavailable. Please try again.");
  return (await response.json()).data;
}

/** Lists published records of one kind in one language, 30 per page per the API contract. */
export async function listWebsiteRecords<TFields = Record<string, unknown>>(
  kind: string,
  language: Language,
  page = 1,
): Promise<PublicWebsiteRecord<TFields>[]> {
  const response = await fetch(
    new URL(`/api/v1/website/${kind}/${language}?page=${page}`, apiOrigin()),
    { cache: "no-store" },
  );
  if (!response.ok) throw new Error("The content service is unavailable. Please try again.");
  return (await response.json()).data ?? [];
}

/** Result of fetching "everything" of a kind: the records, and whether the safety cap was hit. */
export type CompleteListResult<TFields> = {
  records: PublicWebsiteRecord<TFields>[];
  /** True if the cap was reached — the list may be incomplete. Callers should disclose this. */
  truncated: boolean;
};

/**
 * Fetches every published page for a kind, up to a generous safety cap (50 pages). Stops on the
 * first genuinely empty page rather than assuming any particular page size — different list
 * endpoints on this backend use different page sizes (the generic Website CMS endpoint returns
 * 30, blog's public endpoint returns 12), and hardcoding one was exactly the bug that silently
 * dropped blog posts past the first page in the sitemap. If the cap is genuinely hit, this both
 * logs loudly server-side AND returns `truncated: true` so the calling page can tell the visitor,
 * not just the operator — per finding #5, a cap must be visibly exposed, not silently hidden.
 */
export async function listAllWebsiteRecords<TFields = Record<string, unknown>>(
  kind: string,
  language: Language,
  maxPages = 50,
): Promise<CompleteListResult<TFields>> {
  const all: PublicWebsiteRecord<TFields>[] = [];
  let page = 1;
  for (; page <= maxPages; page++) {
    const batch = await listWebsiteRecords<TFields>(kind, language, page);
    if (batch.length === 0) return { records: all, truncated: false };
    all.push(...batch);
  }
  // Reached maxPages without an empty final page — there may be more records we never fetched.
  console.error(
    `listAllWebsiteRecords: hit the ${maxPages}-page cap for kind="${kind}" language="${language}" ` +
    `after collecting ${all.length} records. The result is incomplete — raise maxPages or add real ` +
    `pagination for this kind.`,
  );
  return { records: all, truncated: true };
}

/**
 * Resolves the two site-wide singletons by kind/language (SiteSettings) or kind/language/location
 * (PublicMenu) — the properties the backend actually enforces uniqueness on — rather than
 * assuming a fixed slug. The backend does not require staff to use any particular slug for
 * these records, so a slug-based lookup can silently miss a validly published singleton.
 */
export async function getSiteSettings(language: Language): Promise<PublicWebsiteRecord<SiteSettingsFields> | null> {
  const results = await listWebsiteRecords<SiteSettingsFields>("SiteSettings", language, 1);
  return results[0] ?? null;
}

export async function getPublicMenu(
  language: Language,
  location: "main" | "footer",
): Promise<PublicWebsiteRecord<PublicMenuFields> | null> {
  const { records } = await listAllWebsiteRecords<PublicMenuFields>("PublicMenu", language);
  return records.find((r) => r.document.fields.location === location) ?? null;
}

/**
 * Fetches all published Page records and filters by a content tag. Used for sections composed
 * of an editorially flexible set of pages (Organization: Dorbar Institute, the Sangha, Sadria
 * Society itself) where staff should be able to add or rename entries without a frontend change.
 * `truncated` propagates from the underlying fetch so the page can disclose an incomplete list.
 */
export async function listPagesByTag(
  language: Language,
  tag: string,
): Promise<{ pages: PublicWebsiteRecord[]; truncated: boolean }> {
  const { records, truncated } = await listAllWebsiteRecords("Page", language);
  return { pages: records.filter((p) => p.document.content.tags?.includes(tag)), truncated };
}
/** A MenuItem with its children attached, recursively — supports the backend's up-to-4-level depth. */
export type MenuTreeNode = MenuItem & { children: MenuTreeNode[] };

export function buildMenuTree(items: MenuItem[]): MenuTreeNode[] {
  const byKey = new Map<string, MenuTreeNode>(items.map((item) => [item.key, { ...item, children: [] }]));
  const roots: MenuTreeNode[] = [];
  for (const item of byKey.values()) {
    if (item.parentKey && byKey.has(item.parentKey)) {
      byKey.get(item.parentKey)!.children.push(item);
    } else {
      roots.push(item);
    }
  }
  return roots;
}

/** A LineageNode record with its children attached, recursively. */
export type LineageTreeNode = PublicWebsiteRecord<LineageNodeFields> & { children: LineageTreeNode[] };

/**
 * Builds a nested tree from LineageNode records, keyed by canonicalId/fields.parentId rather
 * than the menu's key/parentKey. Orphaned references (a node whose parent isn't in this page
 * of results) surface as additional roots rather than silently disappearing.
 */
export function buildLineageTree(records: PublicWebsiteRecord<LineageNodeFields>[]): LineageTreeNode[] {
  const byId = new Map<string, LineageTreeNode>(
    records.map((r) => [r.canonicalId, { ...r, children: [] }]),
  );
  const roots: LineageTreeNode[] = [];
  for (const node of byId.values()) {
    const parentId = node.document.fields.parentId;
    if (parentId && byId.has(parentId)) {
      byId.get(parentId)!.children.push(node);
    } else {
      roots.push(node);
    }
  }
  const byOrder = (a: LineageTreeNode, b: LineageTreeNode) =>
    (a.document.fields.order ?? 0) - (b.document.fields.order ?? 0);
  const sortRecursive = (nodes: LineageTreeNode[]) => {
    nodes.sort(byOrder);
    nodes.forEach((n) => sortRecursive(n.children));
  };
  sortRecursive(roots);
  return roots;
}

/**
 * Resolves a fixed-slug singleton page (home/life/contact) with a real fallback path:
 * 1. Try the slug in the requested language.
 * 2. If absent, try the *same* slug in the other language — by convention these fixed pages use
 *    the same slug across languages — purely to discover the canonical ID.
 * 3. If that succeeds, call the by-id endpoint, which the backend documents as returning the
 *    requested translation or a published other-language fallback. This uses the backend's own
 *    fallback logic rather than guessing, and the response's own `document.content.slug` gives a
 *    truthful link for the missing-translation banner — never an inferred or swapped slug.
 * Returns `{ record: null, otherLanguage: null }` only when nothing is published in either
 * language, which callers should render as a genuine "not yet published" empty state, not a
 * missing-translation banner.
 */
export async function resolveFixedSlugPage(
  kind: string,
  language: Language,
  slug: string,
): Promise<{ record: PublicWebsiteRecord | null; isFallback: boolean }> {
  const direct = await getWebsiteRecord(kind, language, slug);
  if (direct) return { record: direct, isFallback: false };

  const otherLanguage: Language = language === "bn" ? "en" : "bn";
  const sibling = await getWebsiteRecord(kind, otherLanguage, slug);
  if (!sibling) return { record: null, isFallback: false };

  const authoritative = await getWebsiteRecordByCanonicalId(kind, language, sibling.canonicalId);
  return authoritative ? { record: authoritative, isFallback: authoritative.language !== language } : { record: null, isFallback: false };
}

/** Builds a truthful cross-language link for a loaded record, using its own otherLanguageSlug — never a guess. */
export function otherLanguageHref(
  record: PublicWebsiteRecord | null,
  basePath: string,
): string | null {
  if (!record?.otherLanguageSlug) return null;
  const otherLanguage: Language = record.language === "bn" ? "en" : "bn";
  return `/${otherLanguage}${basePath}/${record.otherLanguageSlug}`;
}
/**
 * Swaps the language segment of a public path for the site-wide header switcher. Safe to swap
 * directly for routes with no staff-chosen slug (home, listing pages, membership). For
 * detail routes with a real staff-chosen slug (organization/[slug], blog/[slug]) that slug is
 * not guaranteed to match across languages, so this degrades to that section's index instead of
 * guessing a URL that may 404 — the precise per-record link lives on the page itself, built from
 * that record's own otherLanguageSlug via {@link otherLanguageHref}, not from the path alone.
 */
export function swapLanguage(pathname: string, target: Language): string {
  const segments = pathname.split("/").filter(Boolean);
  const [, section, slug] = segments;
  if (slug && (section === "organization" || section === "blog")) {
    return `/${target}/${section}`;
  }
  segments[0] = target;
  return "/" + segments.join("/");
}
