# Stage 5 hardening pass — what changed and what's still open

## Done in this pass

- **Per-page SEO metadata** (`generateMetadata`) on every route: Home, Life & Teachings,
  Organization (index + detail), Books, Media, Programmes & Places, Contact, and the Blog index
  (Blog's detail page already had this from an earlier stage). Titles/descriptions come from the
  published CMS record when one exists, falling back to hardcoded copy otherwise — same pattern
  as every other fallback in this build.
- **`robots.txt` and `sitemap.xml`**, both as real Next.js routes (`app/robots.ts`,
  `app/sitemap.ts`), confirmed in the build output. The sitemap includes the fixed public paths
  plus published blog posts and tagged Organization pages, in both languages. Each dynamic
  section is wrapped in its own try/catch — one CMS kind being briefly unreachable doesn't take
  the whole sitemap down.
- **`PublicMenu`'s "footer" location is now actually used.** The Footer renders a Links column
  from it (e.g. Sitemap, Privacy) when staff publish one, alongside the existing SiteSettings
  contact block. Falls back to nothing (not a broken empty column) if unconfigured.
- **Removed a duplicate header** on the blog index page — it had its own wordmark + language
  toggle, which duplicated the site-wide Header/LanguageSwitcher once every page got wrapped in
  the shared `[lang]/layout.tsx`. The blog *detail* page's header was left alone — that one is a
  contextual back-to-index/other-translation link, not a duplicate of the global nav.
- **WCAG AA contrast, actually computed** (not eyeballed) for every text/background pairing in
  use: all pass, including the muted `#6b6b66` label color at 4.84:1. Gold (`#c9a227`) fails at
  2.18:1 but is never used as a text color, only as a border/accent — worth keeping that as a
  hard rule for any future component using this token.

## Deliberate decision: no analytics or cookie consent added

This site collects genuinely sensitive data elsewhere (NID numbers, addresses, in the Membership
module), so I didn't want to add a third-party analytics script or cookie-consent banner without
that being an explicit choice, not something bundled quietly into a "hardening pass." Current
state: **no analytics, no tracking cookies, nothing to consent to.** If/when you want visit
analytics, the two mainstream options differ in exactly this dimension:
- A privacy-focused, cookie-less option (Plausible, Fathom) needs no consent banner under most
  regimes, since there's no personal-data cookie being set.
- Google Analytics or similar needs a real cookie-consent flow first, which is a project of its
  own, not a one-line addition.

## Still open — not part of this pass

- **Interactive map** for Programmes & Places (needs a maps vendor decision first)
- **Video embedding** (currently an outbound "Watch" link, not an embedded player)
- **SSG → dynamic trade-off**: every `/[lang]/*` page server-renders on demand because
  Header/Footer fetch CMS data with `cache: "no-store"`. Fix is caching those fetches with a
  revalidation tag tied to the CMS's publish-event webhook — not done yet.
- **No real integration test against the running .NET backend.** Real server checks were run
  during the corrections pass (see `CORRECTIONS_APPLIED.md`) — the redirect, `<html lang>`,
  asset proxy, sitemap, and the new service-unavailable/error states were all verified against a
  running Next.js server. What's still not verified: an actual .NET backend serving real data.
  Do this once both sides are running together, before calling stage 5 launch-ready:
  1. Set `MEMBERSHIP_API_ORIGIN` to your running **backend's** URL (e.g. `https://localhost:7198`)
     — this is the only variable that should point at the .NET API. Set `WEBSITE_PUBLIC_ORIGIN`
     to this **Next.js app's own** public URL (e.g. `http://localhost:3000`), used only for
     building canonical links and the sitemap — pointing it at the backend by mistake sends
     canonical URLs and sitemap entries to the wrong host.
  2. `npm run dev`, then walk through every nav item with the backend actually serving data —
     including at least one page in a **Draft** state (to confirm the missing-translation/empty
     fallbacks look right) and one **Published** page per kind (to confirm the real shapes render).
