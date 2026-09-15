# Stages 1–4 — cumulative source delivery

Use this ZIP alone. It contains the uploaded project plus the membership corrections, staff membership application, public membership form, blog CMS, and the remaining website CMS backend/staff editors. You do not need to apply ZIPs 1–4 in sequence.

This includes the corrective update described in **CORRECTIVE_UPDATE.md**. Deferred work is listed in **PENDING_WORK.md**.

Start with **INTEGRATION_DEPLOYMENT_GUIDE.md**. Extract into a new folder and open `TheOne/src/TheOne/TheOne.slnx`. Preserve the complete `TheOne` directory, including `apps` and `packages`.

| Stage | Included scope |
|---|---|
| 1 | Membership state/transaction/numbering/validation corrections and audit fields |
| 2 | Staff membership queue, detail, decisions, contributions and assisted entry |
| 3 | Public Bangla/English membership form aligned with the supplied Google Form |
| 4 | Blog publishing, general website CMS APIs, staff editing and approval |
| 5 | Deferred: full organization website frontend/design and archive reader enrichment |
| 6 | Deferred: actual integration, WordPress migration, hosting configuration and deployment execution |

Stage 4 includes pages, site settings, public menus, places, lineage, events, media entries/images, publication metadata/reading links, author profiles, co-authors and ordered series. Translations have separate approval, publication, slugs, revision history, restore and signed preview. Public APIs expose the approved published revision while edits remain drafts. Publishing creates durable events for later regeneration integration.

**Semantic search is incomplete as an end-to-end feature.** Blog search has revision-bound 384-dimensional pgvector write/query source, unverified against PostgreSQL. It is disabled by default; extension installation, embedding generation/model configuration and public search UI are not supplied as an active service. AI-assisted content must be marked as reviewed by an administrator. No automatic AI content generation is enabled.

No ecommerce or book selling is included. Publication records can link to the reading archive; advanced reading/copy deterrence and archive preparation remain deferred. Existing public membership and basic blog pages are retained; this is not the completed Stage 5 public website.

**Verification:** both frontend production builds and 19 frontend DOM tests pass. Backend source checks pass, but the .NET backend, EF migrations and PostgreSQL tests have not run here because the environment has no .NET SDK. Run `TheOne/scripts/Verify-Cms.ps1` locally before applying migrations. See **VERIFICATION.md** for exact limits.

`PROJECT_DECISIONS.md` records the agreed scope. `CMS_API_GUIDE.md` describes the new contract and editorial operations. Earlier stage setup notes and original architecture documents are retained for history; this README and the cumulative integration guide supersede their old stage/status statements.
