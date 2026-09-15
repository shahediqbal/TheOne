import { notFound, redirect } from "next/navigation";
import { blogApi, type PublicBlog } from "../../../../../components/blog-api";
import BlogArticle from "../../../../../components/BlogArticle";
export const dynamic = "force-dynamic";
export const metadata = { robots: { index: false, follow: true } };
export default async function Page({
  params,
}: {
  params: Promise<{ lang: string; id: string }>;
}) {
  const { lang, id } = await params;
  if ((lang !== "bn" && lang !== "en") || !/^[0-9a-f-]{36}$/i.test(id))
    notFound();
  const post = await blogApi<PublicBlog>(`${lang}/post/${id}`);
  if (!post) notFound();
  if (post.language === lang)
    redirect(`/${lang}/blog/${encodeURIComponent(post.document.slug)}`);
  return <BlogArticle post={post} requestedLanguage={lang} />;
}
