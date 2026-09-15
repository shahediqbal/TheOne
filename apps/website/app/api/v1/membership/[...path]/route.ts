import { NextRequest } from "next/server";
export const dynamic = "force-dynamic";
async function forward(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> },
) {
  const { path } = await params;
  if (path.some((part) => !/^[A-Za-z0-9-]+$/.test(part)))
    return new Response("Invalid path", { status: 400 });
  if (Number(request.headers.get("content-length") || 0) > 16 * 1024 * 1024)
    return new Response("Upload too large", { status: 413 });
  const origin = process.env.MEMBERSHIP_API_ORIGIN || "https://localhost:7198";
  const url = new URL(
    "/api/v1/membership/" + path.map(encodeURIComponent).join("/"),
    origin,
  );
  const headers = new Headers();
  for (const key of ["content-type", "accept"]) {
    const v = request.headers.get(key);
    if (v) headers.set(key, v);
  }
  try {
    const response = await fetch(url, {
      method: request.method,
      headers,
      body: ["GET", "HEAD"].includes(request.method) ? undefined : request.body,
      duplex: "half",
      cache: "no-store",
      redirect: "manual",
    } as RequestInit & { duplex: string });
    if (response.status >= 300 && response.status < 400)
      throw new Error("API origin must be configured directly");
    return new Response(response.body, {
      status: response.status,
      headers: {
        "Content-Type":
          response.headers.get("content-type") || "application/json",
        "Cache-Control": "no-store",
        "X-Content-Type-Options": "nosniff",
      },
    });
  } catch {
    return Response.json(
      {
        success: false,
        message: "The application service is unavailable. Please try again.",
        errors: [],
      },
      { status: 503, headers: { "Cache-Control": "no-store" } },
    );
  }
}
export { forward as GET, forward as POST, forward as PATCH };
