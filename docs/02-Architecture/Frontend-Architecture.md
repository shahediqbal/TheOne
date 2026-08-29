# Frontend Architecture Document

Covers the Next.js web app. Flutter mobile is covered separately in the mobile app plan.

## Stack

Next.js, TypeScript, Tailwind CSS, TanStack Query, Zustand.

## State Management Split

- **TanStack Query** owns all server state — books, articles, membership data, search results. Every API call goes through a typed query/mutation hook, never raw `fetch` in a component.
- **Zustand** owns client-only UI state — reader preferences (font size, theme), sidebar/menu open state, in-progress form drafts. Zustand never stores data that also lives on the server; that's TanStack Query's job, to avoid two sources of truth going stale against each other.

## Routing & Rendering

- Next.js App Router.
- Public routes (`/[lang]/page-slug`, articles, book previews) use Server-Side Rendering or Static Generation for SEO, per the CMS's SEO metadata requirements.
- Member/Admin routes (dashboard, reading, approvals) are client-rendered behind auth — no SEO need, faster to iterate.
- URL language prefix (`/bn/...`, `/en/...`) matches the Multi-Language Strategy; default locale `bn` with no prefix redirect handled at the middleware level.

## API Client & Auth

- Single typed API client (generated or hand-written from the OpenAPI spec — see `API-Standards.md`) wraps all requests.
- Access token held in memory (not localStorage, to reduce XSS token-theft surface); refresh token in an HttpOnly cookie.
- Client interceptor: on a 401 with an "expired" error code, silently calls `/auth/refresh` once, retries the original request, and only redirects to login if the refresh itself fails.
- TanStack Query's `onError` global handler distinguishes auth errors (trigger refresh/redirect) from validation errors (surface inline) from server errors (toast + report).

## Component Architecture

```
src/
  app/                     # Next.js App Router pages
    [lang]/
      (public)/            # CMS pages, articles, book previews
      (member)/            # reader, bookmarks, profile — auth required
      (admin)/             # CMS admin, review queue, permissions — auth + role required
  components/
    ui/                    # design-system primitives (Button, Input, Table...)
    features/              # feature-scoped composites (BookReader, DiffViewer, MembershipForm)
  hooks/                   # TanStack Query hooks, one file per resource (useBooks.ts, useMembers.ts)
  stores/                  # Zustand stores (readerPreferences, uiState)
  lib/
    api-client.ts
    auth.ts
  locales/
    bn/  en/               # CMS UI + shared strings (separate from library ContentVersion text)
```

- `features/` components own their data fetching via hooks from `hooks/`; `ui/` primitives are dumb/presentational and never import `hooks/`.
- Feature components map 1:1 to backend modules (Membership, Library, CMS, Payments) so a change to one module's API rarely touches unrelated component trees.

## Reading Experience (Digital Library specific)

- Reader component is code-split from the main bundle (`next/dynamic`) — it's a heavy feature (pagination, bookmarking, search-within-book) not needed on public/CMS pages.
- Reading position and bookmarks are optimistic-updated locally (Zustand) and synced to the server via a debounced TanStack Query mutation, so navigation feels instant even on slower connections.

## Content Review UI (Admin)

- The side-by-side diff viewer (from the Content Conversion workflow) is its own feature module, consuming `ContentVersion` pairs (original vs. converted) plus the QC score, with Approve/Reject/Edit actions mapped directly to the review workflow's state machine — no client-side reinterpretation of review status.

## Testing

- **Vitest** for component/hook unit tests — colocated with the component (`Component.test.tsx`).
- **Playwright** for E2E — critical paths only: login, membership registration + bKash flow, book reading, admin approval — not exhaustive UI coverage, per the CI gate defined in `Testing-Strategy.md`.

## Performance

- Images (book covers, media) served via `next/image` against the CDN-fronted object storage, meeting the `<2s` page-load / `<3s` book-load targets from the master doc's scale/performance section.
- Route-level code splitting keeps the public CMS bundle free of admin/reader-only code.
