import type { NextConfig } from "next";
import path from "node:path";
const config: NextConfig = {
  poweredByHeader: false,
  output: "standalone",
  outputFileTracingRoot: path.resolve(process.cwd(), "../.."),
  turbopack: { root: path.resolve(process.cwd(), "../..") },
  async redirects() {
    // Replaces the old root app/page.tsx redirect; no page needs to exist outside the [lang]
    // tree now, which is what lets [lang]/layout.tsx be the true root and set <html lang>
    // correctly during server rendering (see that file's comment for why this matters).
    return [{ source: "/", destination: "/bn", permanent: false }];
  },
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [
          { key: "Referrer-Policy", value: "no-referrer" },
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "X-Frame-Options", value: "DENY" },
        ],
      },
    ];
  },
};
export default config;
