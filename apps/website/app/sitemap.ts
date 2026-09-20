import type { MetadataRoute } from "next";
import type { Language } from "../lib/website-api";
import { listPagesByTag } from "../lib/website-api";
import { blogApi, type PublicBlog } from "../components/blog-api";

const languages: Language[] = ["bn", "en"];

const staticPaths = ["", "/life", "/organization", "/books", "/media", "/programmes", "/membership", "/contact", "/blog"];

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const origin = process.env.WEBSITE_PUBLIC_ORIGIN || "https://sadarmawla.org";
  const entries: MetadataRoute.Sitemap = [];

  for (const lang of languages) {
    for (const path of staticPaths) {
      entries.push({ url: `${origin}/${lang}${path}`, changeFrequency: "weekly" });
    }
  }

  // Dynamic sections are fetched per language independently; one failing shouldn't drop the rest.
  for (const lang of languages) {
    try {
      // Page through every published post, not just the first page. Stops only on a genuinely
      // empty page — not a page smaller than some assumed size, since blog's real page size (12)
      // is smaller than the constant this used to compare against, which caused it to stop after
      // page 1 on essentially every real deployment. Page-size-agnostic is the actual fix.
      for (let page = 1; page <= 50; page++) {
        const posts = await blogApi<PublicBlog[]>(`${lang}?page=${page}`);
        if (!posts?.length) break;
        for (const post of posts) {
          entries.push({
            url: `${origin}/${lang}/blog/${encodeURIComponent(post.document.slug)}`,
            lastModified: post.publishedAtUtc,
            changeFrequency: "monthly",
          });
        }
      }
    } catch {
      // Backend unreachable at build time: sitemap still ships with the static paths.
    }

    try {
      const { pages: orgPages } = await listPagesByTag(lang, "organization");
      for (const page of orgPages) {
        entries.push({
          url: `${origin}/${lang}/organization/${encodeURIComponent(page.document.content.slug)}`,
          lastModified: page.publishedAtUtc,
          changeFrequency: "monthly",
        });
      }
    } catch {
      // Same as above.
    }
  }

  return entries;
}
