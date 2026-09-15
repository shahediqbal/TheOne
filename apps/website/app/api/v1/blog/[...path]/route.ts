import type { NextRequest } from "next/server";
export const dynamic = "force-dynamic";
export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> },
) {
  const { path } = await params;
  if (path.join("/") !== "preview") return new Response(null, { status: 404 });
  if (Number(request.headers.get("content-length") || 0) > 8192)
    return new Response(null, { status: 413 });
  const reader = request.body?.getReader();
  const chunks: Uint8Array[] = [];
  let size = 0;
  if (reader) {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      size += value.byteLength;
      if (size > 8192) {
        await reader.cancel();
        return new Response(null, { status: 413 });
      }
      chunks.push(value);
    }
  }
  const bytes = new Uint8Array(size);
  let offset = 0;
  for (const chunk of chunks) {
    bytes.set(chunk, offset);
    offset += chunk.length;
  }
  const body = new TextDecoder().decode(bytes);
  try {
    const response = await fetch(
      new URL(
        "/api/v1/blog/preview",
        process.env.MEMBERSHIP_API_ORIGIN || "https://localhost:7198",
      ),
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body,
        cache: "no-store",
        redirect: "error",
      },
    );
    return new Response(response.body, {
      status: response.status,
      headers: {
        "Content-Type": "application/json",
        "Cache-Control": "no-store",
        "Referrer-Policy": "no-referrer",
      },
    });
  } catch {
    return Response.json(
      { success: false, message: "Preview service is unavailable." },
      { status: 503, headers: { "Cache-Control": "no-store" } },
    );
  }
}
