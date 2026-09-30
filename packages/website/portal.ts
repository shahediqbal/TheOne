/** Cross-application navigation only; never forwards tokens or session data. */
export function portalUrl(configured: string | undefined, fallback: string): string {
  if (!configured) return fallback;
  try {
    const url = new URL(configured);
    if (!["https:", "http:"].includes(url.protocol) || url.username || url.password) return fallback;
    return url.href;
  } catch { return fallback; }
}
