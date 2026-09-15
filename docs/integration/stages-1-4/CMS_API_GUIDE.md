# Website CMS contract and editorial operations

All paths below start with `/api/v1`. JSON successes use the existing `{ success, data, ... }` API envelope. Staff routes require an active MFA session and the relevant current permission. Approval, AI review, publication, archiving and event acknowledgement also require current Admin/SuperAdmin role membership.

## Staff CMS routes

| Method / route | Purpose |
|---|---|
| GET `/admin/website/schema` | Allowed kinds, field labels, types, relationships and required-to-review rules |
| GET `/admin/website/records?kind=Page&page=1&search=` | Queue; 30 per page; title search |
| POST `/admin/website/records` | `{kind,language,canonicalId?,staffId?}`; creates a draft |
| GET/PUT `/admin/website/records/{id}` | Read; save `{expectedVersion,document}` |
| POST `/admin/website/records/{id}/actions/{action}` | `{expectedVersion,revisionId?}` |
| GET `/admin/website/records/{id}/revisions` | Latest 100 revision snapshots |
| POST `/admin/website/records/{id}/preview` | Signed preview token; expires after 15 minutes and invalidated by a new version |
| GET `/admin/website/lookup?kind=Place&page=1` | Relationship choices, 50 per page; `BlogPost` is also supported |
| GET `/admin/website/staff?page=1` | Existing eligible staff identities for author creation |
| GET/POST `/admin/website/assets` | Paged image inventory; multipart upload field `image` |
| GET `/admin/website/assets/{id}` | Private staff image preview |
| GET `/admin/website/publish-events` | Oldest 100 unacknowledged publication events |
| POST `/admin/website/publish-events/{id}/acknowledge` | Idempotent acknowledgement after downstream processing |

Actions: `review`, `return`, `review-ai`, `approve`, `publish`, `archive`, `restore`. Only restore needs `revisionId`. A stale version returns 409. A restore produces a draft; it never directly publishes. Rejected editorial submissions use Return to draft, not membership's Rejected state.

A CMS document has `content` (title, slug, summary, SEO, blocks, tags, provenance) and `fields` (only keys returned by the kind's schema). Unknown keys are rejected. Body blocks use the same allowlist as blogs; free HTML is rejected. Reference fields store canonical record IDs, image fields store upload IDs, and series store an ordered list of canonical blog post IDs. The UI provides choices instead of requiring IDs for these fields.

`Page` holds biography, teachings, organization background and other editorial pages. `LineageNode` provides an acyclic parent tree. Events use timezone-qualified timestamps and optional reviewed Hijri text; automated religious-calendar calculation is not implemented. `Publication` contains bibliographic details, cover and reading availability/link, with no price or checkout. Media uploads support JPEG/PNG, maximum 10 MB with existing header/dimension validation. Audio/video use hosted HTTPS links; this is not a transcoding platform.

## Editorial sequence

1. Grant staff `website.read` + `website.edit`; grant administrators `website.approve` as well. Blog access separately uses `blog.read`, `blog.edit`, `blog.approve`.
2. Add core Page/Place content and relevant image uploads. Save drafts. The form enforces completeness when sending for review.
3. For authors, choose AuthorProfile, load existing staff and select the person. The identity must already have blog access. Create one canonical profile, then use Add translation for the other language. Publish profiles before publishing credited articles.
4. In Blog, load published author profiles and select co-authors. Existing authors can be removed. Co-author associations are saved per revision, so draft credit changes cannot alter live articles.
5. Save → Send for review → administrator Approve → administrator Publish. AI-assisted drafts first require the administrator's human-review confirmation. Editing reviewed AI content requires renewed review.
6. Create series and select/order published articles. Referenced articles must be published in the series language. Publish places before events referring to them. Archive checks prevent breaking live references; update or archive dependent content first.
7. Public menus have main/footer locations, ordered entries and optional parent keys, maximum depth four. These records do not grant staff permissions. Edit existing published menus to change them; a second published menu in the same language/location is blocked.
8. Public media URLs use `/api/v1/website/assets/{id}` on the API host (or a correctly proxied same-origin route). An image becomes public only while a published CMS record explicitly references its asset ID. Upload alone does not publish it. A MediaItem can publish a blog image for later use in an Image block. Membership photos remain in their separate private endpoints.

The saved CMS preview displays content, not the future organization website's final layout. Signed tokens are bearer access to an unpublished preview; share only with intended reviewers and never put them into analytics or server query logs. A future share page should receive tokens through a URL fragment and POST to the preview API. The staff editor's existing preview works directly now.

## Public read APIs

| Method / route | Response |
|---|---|
| GET `/website/{kind}/{bn-or-en}?page=1` | Published records in that language, 30/page |
| GET `/website/{kind}/{language}/{slug}` | Current live document, including when requested through an old reserved slug |
| GET `/website/{kind}/{language}/by-id/{canonicalId}` | Requested translation, or published other-language fallback |
| POST `/website/preview` | `{token}` → saved preview document |
| GET `/website/assets/{id}` | Approved public image or 404 |

Public objects contain canonicalId, kind, language, document, publishedAtUtc, otherLanguageSlug. They do not include linked staff identities, revision history or approval actors. Compare returned language with requested language for the Stage 5 missing-translation banner; use the canonical ID fallback endpoint when no translated slug exists. List endpoints do not silently mix languages. Compare returned content.slug with the requested slug to issue the appropriate redirect in Stage 5.

Existing `/blog` APIs retain their routes. Public blog responses additionally return public author details and associated series. The existing minimal blog frontend does not yet render every new metadata feature; that presentation belongs to Stage 5.

## Optional semantic search

- Administrator PUT `/admin/blog/{translationId}/embedding`: `{publishedRevisionId,model,vector}`.
- Public POST `/blog/search/semantic`: `{language,model,vector,limit}` (limit 1–20).
- Vectors must be finite, non-zero, exactly 384 dimensions, from the configured model. The caller/provider integration generates vectors; these endpoints do not call an external AI provider or accept ordinary text as a search query.
- Disabled configuration returns 503. Follow the integration guide before enabling. Use the same model/version for indexing and queries. Model changes require re-indexing.
- Publication removes the previous embedding in the same transaction. A late indexing job for an older published revision returns 409. Search joins only the currently published revision, preventing archived/unapproved drafts from appearing.

## Durable regeneration events

Events contain entity, recordId, canonicalId, language, action, version, current slug and aliases. Reads can repeat until acknowledged. A later consumer must be idempotent, refresh both language/fallback routes, list/detail/alias paths and dependent pages, and acknowledge only after success. Multiple consumers must coordinate externally; no leasing/worker is supplied. Do not acknowledge manually just to clear the queue. Current public reads are uncached, so the package does not depend on a consumer for freshness.

Original WordPress URL mappings, redirects from those URLs, scheduled publishing, automatic embedding/translation generation and the public organization's final layout are not implemented in this contract.
