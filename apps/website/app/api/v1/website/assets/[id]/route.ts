import type { NextRequest } from "next/server";

export const dynamic = "force-dynamic";

const GUID_PATTERN = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

/**
 * Same-origin proxy for `/api/v1/website/assets/{id}`, mirroring the existing blog/membership
 * proxy pattern. Requires no backend change: the public asset endpoint already exists and
 * already enforces "visible only while a published record references it" dynamically on every
 * request, so this proxy must never cache a response — caching here would keep serving an
 * unpublished image after the backend itself would correctly start returning 404 for it.
 */
export async function GET(request: NextRequest, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  if (!GUID_PATTERN.test(id)) return new Response(null, { status: 404 });

  try {
    const response = await fetch(
      new URL(`/api/v1/website/assets/${id}`, process.env.MEMBERSHIP_API_ORIGIN || "https://localhost:7198"),
      { method: "GET", cache: "no-store", redirect: "error" },
    );
    if (response.status === 404) return new Response(null, { status: 404 });
    if (!response.ok) return new Response(null, { status: 502 });

    return new Response(response.body, {
      status: 200,
      headers: {
        "Content-Type": response.headers.get("content-type") || "application/octet-stream",
        "Cache-Control": "no-store",
        "X-Content-Type-Options": "nosniff",
        "Content-Security-Policy": "default-src 'none'; sandbox",
        "Referrer-Policy": "no-referrer",
      },
    });
  } catch {
    return new Response(null, { status: 502 });
  }
}
