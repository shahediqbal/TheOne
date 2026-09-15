# Agreed project scope

- Staff administration, membership management and research: Vite + React + TypeScript + MUI; authenticated SPA with MFA/permissions.
- Public membership application: no account or OTP; protected private resume credential.
- Shared backend: ASP.NET Core + EF Core + PostgreSQL.
- Organization website: Next.js + TypeScript + Tailwind; primarily public/static content. No ecommerce, cart, checkout or book selling. A separate future sales site may be linked.
- Blog: canonical post with per-language Bangla/English translation, slug, SEO, status and publication date; administrator approval required before public publication and before updates replace live content. Structured allowlisted JSON blocks, revisions, signed preview, author/co-author profiles, series, citation/SEO data and AI provenance gate. Public accounts/comments/reactions/subscriptions are not included.
- Reading archive: Next.js + TypeScript + Tailwind, SSG/selective regeneration; all founder's books intended for online reading. Book preparation is ongoing. Advanced reading tools and copy deterrence are deferred; downloads are not assumed.
- Future reading app: Flutter, separate later delivery.
- Books and website use shared content sources. ISR hosting must be chosen explicitly; pure static export requires rebuilds instead.
- Membership fee: BDT 100 one-time approval gate. Regular dues, event contributions and donations are separate manual records.
- Open decisions: fixed monthly vs daily dues; non-member donations. No arrears logic in Stage 1.
- Membership form fields now follow the supplied Bangla Google Form. English full name remains required; other English translations are optional and manually entered.
- Email is explicitly entered without Google sign-in. Conduct acceptance, truth declaration and oath are separately required, with consent version and server timestamp.
- Signature currently means typed full name; handwriting capture is not implemented. Applicant date and formal approval date are separate. Formal membership date comes from staff approval.
- Photos are private JPEG/PNG uploads (maximum 10 MB), accessible through credential/permission-checked endpoints. Resume tokens are shown once and can be explicitly downloaded; no SMS delivery exists.
- Numbered source ZIP deliveries with installation instructions, migrations and truthful test status.

## Agreed six-stage plan

1. Membership backend corrections.
2. Staff membership frontend and supporting APIs.
3. Public membership frontend.
4. Website CMS backend and staff editing frontend.
5. Public website frontend — deferred by the user.
6. Integration, migration and deployment execution — deferred by the user; instructions are included now.

## Stage 4 source delivered

Blog workflow and general CMS types share the administrator approval requirement. Pages, settings, public menus, lineage, places, events, media, publication links, staff-linked author profiles/co-authors and ordered series are included. Public menus are separate from permission-bearing staff menus. There is no author login system separate from existing staff identity.

New content follows Draft → InReview → Approved → Published → Archived per translation. Updating or restoring a live record creates a draft and does not replace the public revision. Saved revisions, approval actors, timestamps and expiring version-bound preview credentials are retained. Slug aliases are reserved; Stage 5 must redirect old aliases to the returned canonical slug and render the missing-translation banner/cross-link.

Publishing and archiving enqueue durable regeneration events in the same database transaction. A Stage 6 consumer must invalidate the correct paths, then acknowledge events; no automatic ISR claim is made now. WordPress URL inventory/import and redirects are Stage 6 migration work.

Semantic search storage/query contracts use optional pgvector(384), bound to the currently published translation revision and a configured model. Embedding provider integration remains disabled/deferred. Editing a draft preserves the old live index; publishing or archiving invalidates it until re-indexed. AI-assisted content provenance is enforced, but no automatic translation/tag-generation provider is added by Stage 4.

All Stage 1–4 source is packaged together. Frontend build/DOM checks run here; .NET/PostgreSQL and real-browser validation must run locally. The full new organization website design and advanced reading tools are outside this delivery.

## Corrective update and pending work

See CORRECTIVE_UPDATE.md for completed cleanup and PENDING_WORK.md for all deferred work. Monthly dues remain undecided; neither fixed monthly nor daily accrual is adopted. Semantic search remains disabled and incomplete until vector generation/indexing and query integration are implemented and tested. SuperAdmin bootstrap and emergency recovery remain explicit authentication backlog items before fresh-install operation/production, respectively.
