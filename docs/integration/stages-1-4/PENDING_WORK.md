# Pending work after the corrective update

This is the outstanding list, not a claim that these features are already implemented. Stages 5 and 6 remain deferred until resumed. No additional provider, dues rule or administrator recovery mechanism was silently selected.

| Work | When / dependency | Planned outcome |
|---|---|---|
| Backend compilation and regression tests | Next local integration step, before applying working database migrations | Run Verify-Cms.ps1 against a disposable PostgreSQL database; resolve compile, runtime or EF snapshot differences; retain results. The new submission-policy cases are included by its Membership filter. |
| Actual migration-history reconciliation | Before changing the real development database | Compare EF history and existing tables with this package; test the reviewed migration SQL on a restored copy. If earlier manual tables exist, create a deliberate additive reconciliation/backfill migration. |
| Monthly-dues rule | Business decision before dues calculations or public promises | Choose fixed BDT 60/calendar month OR BDT 2/day. Specify commencement, partial periods, advance payments, waivers and arrears policy. Until agreed, MonthlyDue remains a manually recorded payment category with no calculation/arrears engine. |
| Usable semantic search | Before enabling/search UI; optional for initial Stage 5 | Choose a model/provider compatible with the current 384 dimensions or explicitly migrate dimensions. Add published-text extraction, provider adapter, durable indexing jobs/retries/backfill, revision checks, query-text embedding and integration tests. Install/test pgvector, then enable. Storage/query code alone is incomplete and remains unverified. |
| Stage 5 organization website | When Stage 5 is resumed | Build the agreed visitor navigation and public pages with Next.js/TypeScript/Tailwind: Home; Mawla's Life & Teachings; Organization; Books & Reading; Media; Programmes & Places; Membership; Contact. Present CMS translations, fallback banner/cross-link, author/series metadata, canonical slugs, SEO and accessible responsive layouts. No ecommerce. |
| SuperAdmin bootstrap | Before a fresh installation can be operated | Design controlled, one-time first-administrator provisioning through the existing identity system. Require explicit operator verification, MFA setup and audit records; disable bootstrap after completion. No public self-promotion or default password. Existing installations keep their current accounts. |
| Emergency administrator recovery | Before production operation | Agree who can authorize recovery and what offline proof is required. Implement an audited recovery procedure with session revocation and MFA/key recovery handling; rehearse it. Do not add an unaudited MFA bypass. This is separate from ordinary recovery-code login already present. |
| Stage 6 hosting/integration | After local gates and hosting choices | Configure TLS, origins, trusted proxy headers/client IPs, edge quotas, persistent Data Protection keys, process supervision, protected secrets, backups and monitoring. Verify secure cookies and actual end-to-end flows. Deploy only after staging acceptance. |
| Publication-driven regeneration | Stage 6, after Stage 5 caching design | Implement an idempotent consumer of publication events, update affected lists/details/aliases/translations and dependent pages, retry failures, and acknowledge only after success. No active ISR worker exists now. |
| WordPress content migration and redirects | Stage 6, before replacing the current public site | Inventory current content and old URLs, map translations/slugs, migrate and proofread content/media, establish redirects and verify links/SEO before switching traffic. New CMS slug aliases are not a WordPress migration. |
| Real-browser and live-system acceptance | Before public launch | Test actual API/DB integration, mobile/desktop, Bangla/English, keyboard access, editor roles, membership resume/photo/submission/payment decisions and publishing. Existing 19 DOM tests use mocks, not the live system. |
| Reading archive and advanced book reader | After archive preparation; separate agreed work | Finish book ingestion/proofreading, then enrich reading navigation/tools and reasonable copy deterrence. The current CMS provides publication metadata/reading links only. Flutter reader remains a separate later application. |
| Additional contribution/accounting features | Separate scope after business rules | Decide non-member donations, payment allocation/installments, receipts, reversals/refunds and cash duplicate handling. Current records are member-linked manual payments; contribution intention on the form is not a payment. |
| Automated AI translation/tag assistance | Separate provider integration | Select/configure providers and review controls. Existing provenance gates do not generate content. Membership translation stays disabled unless explicitly configured; blog/CMS automatic generation is not delivered. |

## Recommended order

1. Run the local backend gate and reconcile database history before applying code/schema together.
2. Record the dues rule when the organization can decide it; implementation remains deferred.
3. Resume Stage 5 with editorial related posts/series if semantic search is not required for launch. Do not expose a nonfunctional text-search feature.
4. Complete search integration if selected, and administrator provisioning/recovery before production operation.
5. Execute Stage 6: staging integration, migration/redirects, hosting, acceptance and deployment.
6. Enrich the reader and extend accounting/AI features under separately agreed scope.

## Completed in this corrective update

- Replaced the outdated payment/webhook SVG with the implemented draft/submission/verification/approval flow and separate manual membership-fee check.
- Replaced submission-time property-name reflection with typed required-field accessors and explicit stored-value mappings.
- Replaced the downstream reflective string-length loop with typed validator rules.
- Added regression cases for missing required values, oversized stored fields and invalid stored numeric/enum values, plus an executable static mapping-coverage check.
- Formatted WebsiteRules.cs and WebsiteEntities.cs; token comparison confirms no behaviour changes in those two files.
- Made incomplete semantic search, undecided dues and remaining local/production gates explicit.

No database migration, frontend feature, fee rule, provider activation or production change is introduced by this corrective update. The full cumulative ZIP includes previous Stages 1–4 source and installation instructions.
