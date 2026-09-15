import { notFound } from "next/navigation";
import BlogPreview from "../../../../components/BlogPreview";
export const metadata = {
  robots: { index: false, follow: false },
  referrer: "no-referrer",
};
export default async function Page({
  params,
}: {
  params: Promise<{ lang: string }>;
}) {
  const { lang } = await params;
  if (lang !== "bn" && lang !== "en") notFound();
  return <BlogPreview language={lang} />;
}
