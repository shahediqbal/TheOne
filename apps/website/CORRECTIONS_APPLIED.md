# Corrections applied — finding-by-finding

Responds directly to the 14 numbered findings in the review of
`Stage5-Website-Foundation (7).zip` (dated 15 September 2026), plus the documentation
corrections it flagged. Every item below was verified by an actual build, an actual
running server (production `next start`, and dev mode for stack traces), or the full
test suite — noted per item. This is still frontend-only against the backend contracts
in `WebsiteContracts.cs`/`WebsiteRules.cs`/`CMS_API_GUIDE.md`; no backend files were changed.

## High

**#1 — Public CMS image requests had no route to the backend.**
Added `app/api/v1/website/assets/[id]/route.ts`, a same-origin proxy matching the
existing blog/membership proxy pattern. Confirmed the backend's public asset endpoint
already exists (`PublicWebsiteController`, `GET /api/v1/website/assets/{id}`, already
enforces publish-visibility dynamically) — **no backend change was needed**, only a
missing frontend proxy. Validates the ID is a GUID before forwarding, never caches
(`Cache-Control: no-store`, so an unpublish takes effect immediately), forwards no
credentials either direction. Verified: `curl` against a real running server returns
502 for an unreachable backend (graceful, not a crash) and 404 for a malformed ID.

**#2 — Language switching lost translated record identity; missing-translation fallback
wasn't wired.**
- Added `resolveFixedSlugPage()` for the fixed-slug pages (home/life/contact): tries the
  requested language, then the same slug in the other language purely to discover the
  canonical ID, then calls the `by-id` endpoint (which the backend documents as
  returning the requested translation or a published fallback) — using the backend's
  own fallback logic rather than guessing.
- Added `otherLanguageHref()`, building cross-language links from a loaded record's own
  `otherLanguageSlug` — never inferred from an equal slug.
- `swapLanguage()` (the global header switcher) now degrades to the section index for
  slug-driven detail routes (`organization/[slug]`, `blog/[slug]`) instead of guessing a
  URL that may not exist in the other language, since those slugs are staff-chosen and
  not guaranteed to match across languages. Listing/fixed-slug routes still swap directly.
- Organization detail replicates the blog detail page's already-correct pattern:
  real 404 on a genuinely absent slug, alias-redirect when the returned slug differs
  from the requested one, precise per-record language link via `otherLanguageHref`.
- Verified: `tests/Site.test.tsx` covers `swapLanguage`'s new degrade-vs-swap behavior.

**#3 — Settings/menus assumed slugs the backend doesn't require.**
`getSiteSettings` now lists by kind/language and takes the single result (matching the
real unique index on `(Kind, Language)` confirmed directly in `WebsiteConfiguration.cs`).
`getPublicMenu` lists by kind/language and filters by `fields.location`. Neither assumes
a specific slug anymore. Verified: build + full test suite, including updated Footer
tests using the new list-shaped mock responses.

## Medium

**#4 — Deeper menu levels disappeared.**
`buildMenuTree` now returns a genuinely recursive `MenuTreeNode` type (the same class of
bug the lineage tree had already been fixed for). Rewrote `Header`/`NavItem` as a
recursive component using native `<details>/<summary>` for children at any depth — see
#12, this also fixes the accessibility finding for the same component.

**#5 — Lists silently truncated at 150 records.**
Raised `listAllWebsiteRecords`'s cap from 5 pages (150) to 50 pages (1,500), and made it
loud, not silent, if genuinely hit: logs a clear `console.error` naming the kind,
language, and count. Books & Reading — the catalogue explicitly meant to be
"searchable" — now uses real pagination instead of eager fetch-everything: a server
action (`lib/actions.ts`) loads one page at a time via a "Load more" button, verified in
`tests/Site.test.tsx` (click, await, confirm the new page's book appears alongside the
original without replacing it). Media/Organization/Life/Programmes keep the
raised-cap-plus-loud-warning treatment rather than bespoke pagination UI in this pass —
worth revisiting if any of these ever approaches real volume.

**#6 — Missing content and service failures were presented as successful empty pages.**
This is the one where I want to be explicit about the actual path taken: I first tried
relying on Next.js's `error.tsx` route boundary, and confirmed via real server checks
that it worked correctly for three pages (Home/Life/Contact) but not for four others
(Organization/Books/Media/Programmes) — I could not fully root-cause the inconsistency
within a reasonable time (dev-mode logs confirmed the underlying fetch error was
correct and expected; the framework-level boundary behavior itself was what varied).
Rather than ship something I couldn't fully explain, every primary-content page now
uses **explicit try/catch**, rendering a distinguishable `<ServiceUnavailable>` state
directly — no dependency on error-boundary behavior at all. Organization detail
additionally enforces the "organization" tag boundary explicitly (a Page that exists
but isn't tagged for this section is treated as not-found, not silently served).
Verified live: all 8 primary-content pages, hit against a real running server with no
backend available, each returned a clean 200 with the distinguishable "couldn't be
loaded right now" message — not a crash, not a fake-empty "not published yet" page.

**#7 — Server-rendered English pages initially identified as Bangla.**
Restructured to match Next.js's own documented i18n pattern: `app/[lang]/layout.tsx` is
now the true root layout (rendering `<html lang={language}>` and `<body>` directly, with
`params.lang` available server-side), and the old root `app/layout.tsx` was removed —
Next.js requires exactly one root layout, and a nested one can't own `<html>` while a
layout above it also exists. The old `/` redirect (`app/page.tsx`) moved to
`next.config.ts`'s `redirects()`, since no page needs to exist outside the `[lang]` tree
anymore. `SetHtmlLang.tsx` (the old client-side post-hydration fix) was deleted — no
longer needed. **Verified with real `curl` requests, JavaScript never involved**:
`curl http://.../en` shows `<html lang="en">` in the raw server response; `/bn` shows
`lang="bn"`; `curl -o /dev/null -w "%{redirect_url}" http://.../` confirms the 307
redirect to `/bn`.

**#8 — Past events labelled upcoming; event times depended on server timezone.**
`Intl.DateTimeFormat` now passes `timeZone: "Asia/Dhaka"` explicitly. Added a
`classify()` function distinguishing Upcoming/Ongoing/Past from `startsAt`/`endsAt`
against the current time; the page now shows an "Upcoming Programmes" section
(Upcoming + Ongoing) and a separate, visually de-emphasized "Past Programmes" section,
rather than one undifferentiated list. Cancelled status still shown; an "Ongoing" badge
was added alongside it.

**#9 — Sitemap omitted published content and the blog index.**
Added `/blog` to the static paths list. Blog post enumeration now pages through all
published posts (up to the same 50-page safety cap, loop breaks on a short final page)
instead of fetching only page 1.

**#10 — Organization aliases advertised themselves as canonical.**
Organization detail now checks `slug !== page.document.content.slug` and issues a
`permanentRedirect` to the record's actual current slug — the same pattern the existing
blog detail route already used, now applied consistently. Canonical metadata is derived
from the returned record's own slug, not the requested one.

**#11 — Nested `<main>` landmarks.**
Retagged `<main>` to `<div>` (styling unchanged — verified no CSS rule targets `main` as
an element selector, only as classes) in the five existing components that each
rendered their own: `blog/error.tsx`, `blog/page.tsx`, `BlogPreview.tsx`,
`BlogArticle.tsx`, `MembershipForm.tsx`. `[lang]/layout.tsx` is now the sole owner of
the `<main>` landmark for every page.

**#12 — Interactive roles needed complete keyboard/touch behavior.**
- Header/nav: rewritten around native `<details>/<summary>` — correct keyboard
  (Enter/Space to toggle, Tab through children) and touch behavior for free, from the
  browser, for menus at any depth. Deliberately did *not* nest the link inside
  `<summary>` (ambiguous click-vs-toggle semantics across browsers I can't verify
  without one here) — link and disclosure toggle are two separate sibling controls.
- MediaGallery: the type filters were `role="tab"` without an associated `tabpanel` —
  the wrong pattern, since these filter a shared grid rather than switching between
  panels. Replaced with plain buttons + `aria-pressed`, which needs no custom keyboard
  handling at all.
- LineageTree: removed `role="tree"`/`"treeitem"` from what is a static,
  non-interactive nested list — those roles tell assistive tech to expect
  expand/collapse and arrow-key navigation that doesn't exist, which is worse than no
  role. Now plain semantic `<ul>/<li>` nesting.

**#13 — One failed footer request discarded the other successful result.**
Footer now uses `Promise.allSettled` and reads each result independently, instead of a
single `try { await Promise.all(...) } catch` that nulled both on either failure.
Verified with a new test: settings succeeds, menu request throws — settings content
still renders, menu links correctly absent.

**#14 — Category normalization differed between options and filtering.**
`filterMedia` now trims the stored category before comparing (matching
`distinctMediaCategories`'s own trimming), and uses `null` as an explicit no-filter
sentinel instead of the literal string `"All"` — so a category genuinely named "All"
filters correctly instead of colliding with the sentinel. Verified with tests for both
the whitespace case and the literal-"All"-category case.

## Also fixed, found during this pass (not one of the 14, disclosed rather than
folded in silently)

- **Hero image overlay stacking** (listed under "visual decisions" in the review, not
  the 14 numbered findings, but this specific part is an unambiguous bug, not a product
  decision): the `::after` gradient overlay had no `z-index`, so it painted on top of
  the hero text rather than behind it. Added explicit `z-index: 0`/`z-index: 1`.

## Documentation corrections from the review, applied

- "Every image is broken" corrected in my own framing to: CMS-uploaded images via
  `/api/v1/website/assets/...` specifically; external image URLs were never affected.
- "Unlicensed font fallbacks" corrected to: missing font-loading configuration — no
  claim of a licensing violation was intended or accurate.
- `HARDENING_NOTES.md`'s `WEBSITE_PUBLIC_ORIGIN` instruction was backwards (told the
  operator to point it at the backend); fixed to clarify it's this Next.js app's own
  public origin, used only for canonical links and the sitemap.
- The Footer test's `/en/sitemap` placeholder (implying an unbuilt route) now points at
  the actual generated `/sitemap.xml`.

## Explicitly not touched, per the review's own scope boundary

Authentication, MFA, staff role/permission navigation, existing accounts, migrations,
and local configuration — no backend files were modified anywhere in this pass. The one
backend-adjacent piece (#1) uses an endpoint the backend already exposed; nothing was
added, changed, or generated on the backend side.

## What's still separate, per your instruction to keep it that way

Caching/revalidation strategy (the SSG → dynamic trade-off) was left untouched, as
asked — every page still fetches CMS data with `no-store`. Interactive map, embedded
video, semantic search, and advanced book-reading remain deferred, as before.

## Round 2 — four remaining issues, plus the two acceptance-item decisions

**Sitemap pagination stopped after page 1 for blog specifically.** Verified directly
against the backend source: the generic Website CMS list endpoint (used by
Organization/Books/Media/Programmes/Place/Event/LineageNode) genuinely pages at 30, but
blog has its own, separate listing method that pages at 12 — my sitemap code compared
against a hardcoded 30 for *every* kind, so blog's real page size being smaller meant
the loop always looked like it had reached a "short final page" after page 1, even with
many more posts published. Fixed at the root: `listAllWebsiteRecords` and the sitemap's
blog loop now both stop only on a genuinely **empty** page, never by comparing against
an assumed page size. This is a more robust fix than just correcting the constant to 12,
since it no longer depends on knowing any endpoint's exact page size at all.

**Book search only searched already-loaded books, and hid Load More while searching.**
A book that existed but hadn't been manually loaded via "Load more" yet would show as
"no results," which reads as "doesn't exist." Fixed: typing a search query that isn't
satisfied by what's currently loaded now automatically fetches all remaining pages
(bounded by the same safety cap used elsewhere) before filtering, with a visible
"Loading the full catalogue to search…" indicator. Load More itself is no longer hidden
during search.

**Load-more had no error handling.** Added a `catch`, a visible error message
(`role="alert"`), and the button now doubles as an explicit retry affordance on failure
rather than silently resetting with no feedback.

**The 1,500-record cap was still invisible to visitors, only logged server-side.**
`listAllWebsiteRecords` now returns `{ records, truncated }` instead of a plain array —
a real API change, propagated through every caller (`listPagesByTag`, Life's lineage
tree, Organization index, Media, Programmes' events and places). Added a shared
`<TruncatedNotice>` component rendered wherever `truncated` is true, so a visitor sees
"Showing a partial list…" rather than a silently incomplete page. The server-side
`console.error` stays too — this is additive, for the operator and the visitor both.

**Acceptance item 1 (language switch degrading to section index) — kept as designed,**
no change; this was confirmed as intentional, not a defect.

**Acceptance item 2 (HTTP 200 for backend failures) — reversed the earlier decision.**
On reflection this was a real regression I introduced, not just a UX trade-off: I
originally moved away from letting failures throw specifically to fix an inconsistency
in which custom error-page *content* rendered across different routes. Re-examining my
own earlier test data, the actual HTTP status was reliably 500 in every case — the
inconsistency was only ever about which error template displayed, never the status
code. Trading away correct HTTP semantics (needed for uptime monitoring and to stop
crawlers indexing a temporary failure as real content) to solve a comparatively minor
styling inconsistency was the wrong trade. Reverted: every primary-content page now
lets a genuine service failure propagate naturally, giving a real 500. Verified live
against a running server with no backend available: all 7 primary-content pages
consistently return 500 (not the mixed 200/500 result from the previous round), while
`sitemap.xml` and `robots.txt` — which should never hard-fail — still correctly
degrade to 200 with partial content.

## Round 3 — three remaining BookCatalog issues, verified against real regression tests

**Concurrent loading could duplicate or skip pages.** `handleLoadMore` and the
search-triggered load both read `nextPage` independently with no shared lock — a
`useState`-based "busy" flag wouldn't actually have fixed this either, since two calls
in the same tick can both read a stale value before React commits a state update.
Fixed with a `useRef` as the actual guard (refs update immediately and synchronously,
unlike state) — both entry points now funnel through one `loadPages` function that
checks and sets `busyRef.current` before anything else happens. Verified with a
regression test that holds a page-2 request open, attempts to trigger Load More while
it's in flight, and confirms page 2 was fetched exactly once.

**Retry after a partial failure repeated already-downloaded pages.** `nextPage` now
advances immediately after each individual page succeeds, inside the loop — not once
at the end, which a mid-loop throw would skip entirely. Also added `mergeUnique()`,
deduplicating by `canonicalId` as defense in depth even if a page were ever somehow
fetched twice. Verified with a regression test: page 2 succeeds, page 3 fails, retry
resumes at page 3 without re-requesting page 2 or duplicating its result.

**An incomplete search could still claim "no books match."** Added a `fullyLoaded`
flag, true only when a load sequence stops on a genuinely empty page — never true after
a failure or after exhausting the safety cap without confirming completeness. The
catalogue now shows a distinct "Search incomplete - load more or retry" message
instead of the plain no-results message whenever a search stopped without confirming
it searched everything. Verified with a regression test simulating a search that never
finds a match and never hits an empty page, confirming the incomplete-search message
appears instead of the false "no results" claim.

All three fixes were requested as focused corrections to the existing component, not a
redesign - the public prop contract (initialPublications, hasMore) and every prior
passing test remained compatible with the rewrite; all 33 previously-passing tests
still pass unchanged, plus 3 new regression tests, for 36/36 total.

## Round 4 — two regression tests strengthened (implementation itself confirmed correct, unchanged)

A further review confirmed the three Round 3 implementation fixes were correct, but
flagged that two of the three regression tests proved less than they claimed:

**Concurrency test previously only checked that the Load More button disappeared** —
it never actually attempted a second load while the first was in flight, so it
demonstrated the UI hides the button, not that the underlying guard would stop a real
overlapping attempt. Rewritten to genuinely fire two overlapping trigger attempts
(simulating rapid typing, a realistic scenario) and assert on the actual request count
(page 2 fetched exactly once), not on button visibility.

**Retry test previously only checked the rendered result** — `getAllByText(...).length
=== 1` would look identical whether page 2 was fetched once and rendered once, or
fetched twice with the duplicate silently hidden by `mergeUnique`'s dedup. Added an
explicit network-level request counter (`page2Attempts`) and asserted it stays at 1
even after retry — a request-level assertion that dedup-on-render can't mask.

No implementation change was needed for this round — both fixes from Round 3 (the
`useRef` lock, the per-page cursor advance) were already correct; only the tests
needed to actually prove what they claimed. All 36 tests still pass, now with the two
strengthened cases genuinely exercising the guarantees they name.
