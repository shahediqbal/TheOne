import { notFound, permanentRedirect } from "next/navigation";
import { blogApi, type PublicBlog } from "../../../../components/blog-api";
import BlogArticle from "../../../../components/BlogArticle";
export const dynamic = "force-dynamic";
type Props = { params: Promise<{ lang: string; slug: string }> };
async function read(params: Props["params"]) {
  const { lang, slug } = await params;
  if (lang !== "bn" && lang !== "en") notFound();
  const post = await blogApi<PublicBlog>(`${lang}/${encodeURIComponent(slug)}`);
  if (!post) notFound();
  return { lang, slug, post };
}
export async function generateMetadata({ params }: Props) {
  const { post } = await read(params);
  const origin = process.env.WEBSITE_PUBLIC_ORIGIN;
  return {
    title: post.document.seoTitle || post.document.title,
    description: post.document.seoDescription || post.document.summary,
    alternates: origin
      ? {
          canonical: new URL(
            `/${post.language}/blog/${encodeURIComponent(post.document.slug)}`,
            origin,
          ).href,
        }
      : undefined,
  };
}
export default async function Page({ params }: Props) {
  const { lang, slug, post } = await read(params);
  if (slug !== post.document.slug)
    permanentRedirect(
      `/${lang}/blog/${encodeURIComponent(post.document.slug)}`,
    );
  return <BlogArticle post={post} requestedLanguage={lang} />;
}
