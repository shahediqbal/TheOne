import type { PublicBlog } from "../../../packages/blog/types";
export async function blogApi<T>(path: string): Promise<T | null> {
  const origin = process.env.MEMBERSHIP_API_ORIGIN || "https://localhost:7198";
  const response = await fetch(new URL("/api/v1/blog/" + path, origin), {
    cache: "no-store",
  });
  if (response.status === 404) return null;
  if (!response.ok)
    throw new Error("The reading service is unavailable. Please try again.");
  return (await response.json()).data;
}
export type { PublicBlog };
