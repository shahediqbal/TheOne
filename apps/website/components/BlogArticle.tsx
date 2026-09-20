import BlogBlocks from "./BlogBlocks";
import type { PublicBlog } from "./blog-api";
export default function BlogArticle({
  post,
  requestedLanguage,
}: {
  post: PublicBlog;
  requestedLanguage: string;
}) {
  const d = post.document;
  const other = requestedLanguage === "bn" ? "en" : "bn";
  const base = process.env.WEBSITE_PUBLIC_ORIGIN;
  const canonical = base
    ? new URL(`/${post.language}/blog/${encodeURIComponent(d.slug)}`, base).href
    : undefined;
  const structured = {
    "@context": "https://schema.org",
    "@type": "BlogPosting",
    headline: d.title,
    description: d.seoDescription || d.summary,
    inLanguage: post.language,
    datePublished: post.publishedAtUtc,
    mainEntityOfPage: canonical,
  };
  return (
    <div className="blog-shell">
      <header className="blog-header">
        <a href={`/${requestedLanguage}/blog`}>
          {requestedLanguage === "bn"
            ? "সাদরিয়া পাঠ ও ভাবনা"
            : "Sadria · Reading & reflection"}
        </a>
        <a
          href={
            post.otherLanguageSlug
              ? `/${other}/blog/${encodeURIComponent(post.otherLanguageSlug)}`
              : `/${other}/blog/post/${post.postId}`
          }
        >
          {other === "bn" ? "বাংলা" : "English"}
        </a>
      </header>
      {post.language !== requestedLanguage && (
        <p className="notice">
          {requestedLanguage === "bn"
            ? "বাংলা অনুবাদ এখনও প্রকাশিত হয়নি। মূল লেখাটি পড়ুন।"
            : "The English translation is not published yet. You can read the available article below."}{" "}
          <a href={`/${post.language}/blog/${encodeURIComponent(d.slug)}`}>
            {post.language === "bn" ? "বাংলা" : "English"}
          </a>
        </p>
      )}
      <article lang={post.language}>
        <p className="blog-date">{post.publishedAtUtc.slice(0, 10)}</p>
        <h1>{d.title}</h1>
        <p className="blog-summary">{d.summary}</p>
        <BlogBlocks blocks={d.blocks} language={post.language} />
        {d.tags.length > 0 && <p className="blog-tags">{d.tags.join(" · ")}</p>}
      </article>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html: JSON.stringify(structured).replace(/</g, "\\u003c"),
        }}
      />
    </div>
  );
}
