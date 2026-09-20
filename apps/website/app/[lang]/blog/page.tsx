import { notFound } from "next/navigation";
import { blogApi, type PublicBlog } from "../../../components/blog-api";
export const dynamic = "force-dynamic";
export async function generateMetadata({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  return {
    title: lang === "bn" ? "পাঠ ও ভাবনা | সাদরিয়া সোসাইটি" : "Reading & reflection | Sadria Society",
    description:
      lang === "bn" ? "জ্ঞান, আত্মদর্শন ও মানবতার পথে।" : "On knowledge, self-reflection and humanity.",
  };
}
export default async function Page({
  params,
  searchParams,
}: {
  params: Promise<{ lang: string }>;
  searchParams: Promise<{ page?: string }>;
}) {
  const { lang } = await params;
  if (lang !== "bn" && lang !== "en") notFound();
  const query = await searchParams;
  const page = Number(query.page || 1);
  if (!Number.isInteger(page) || page < 1 || page > 10000) notFound();
  const posts = (await blogApi<PublicBlog[]>(`${lang}?page=${page}`)) || [];
  return (
    <div className="blog-shell">
      <h1>{lang === "bn" ? "পাঠ ও ভাবনা" : "Reading & reflection"}</h1>
      <p className="blog-summary">
        {lang === "bn"
          ? "জ্ঞান, আত্মদর্শন ও মানবতার পথে।"
          : "On knowledge, self-reflection and humanity."}
      </p>
      <div className="blog-index">
        {posts.map((p) => (
          <article key={p.postId}>
            <p className="blog-date">{p.publishedAtUtc.slice(0, 10)}</p>
            <h2>
              <a href={`/${lang}/blog/${encodeURIComponent(p.document.slug)}`}>
                {p.document.title}
              </a>
            </h2>
            <p>{p.document.summary}</p>
          </article>
        ))}
      </div>
      {posts.length === 0 && (
        <p>
          {lang === "bn"
            ? "এখানে এখনও কোনো লেখা প্রকাশিত হয়নি।"
            : "No articles have been published here yet."}
        </p>
      )}
      <nav className="blog-header">
        {page > 1 && (
          <a href={`?page=${page - 1}`}>
            {lang === "bn" ? "আগের পাতা" : "Previous"}
          </a>
        )}
        {posts.length === 12 && (
          <a href={`?page=${page + 1}`}>
            {lang === "bn" ? "পরের পাতা" : "Next"}
          </a>
        )}
      </nav>
    </div>
  );
}
