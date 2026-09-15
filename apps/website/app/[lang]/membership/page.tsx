import { notFound } from "next/navigation";
import MembershipForm from "../../../components/MembershipForm";
export function generateStaticParams() {
  return [{ lang: "bn" }, { lang: "en" }];
}
export const dynamicParams = false;
export default async function Page({
  params,
}: {
  params: Promise<{ lang: string }>;
}) {
  const { lang } = await params;
  if (lang !== "bn" && lang !== "en") notFound();
  return <MembershipForm language={lang} />;
}
