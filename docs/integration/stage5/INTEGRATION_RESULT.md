# Stage 5 website integration result

Verified 2026-09-20. Base: `57f03a6` on `integration/stages-1-4`.
Integration branch: `stage5-website-corrections`.
Input: `Stage5-Website-Corrections (3).zip` (Round 4).

## Integrated changes

- Bilingual public home, life, organization/detail, books, media, programmes and contact pages; shared public header/footer and CMS menus.
- Existing membership and blog pages retain their functionality inside the shared layout.
- Public CMS image proxy, robots/sitemap, metadata, translation links, catalogue search/pagination and retry behavior.
- Removed the previous root `app/layout.tsx` and `app/page.tsx`: the new language layout supplies the document root, and the root URL redirects to Bangla. Copying the archive alone would have left conflicting routes.
- Final independent integration review identified a backend contract mismatch: CMS SEO fields are flat `seoTitle` and `seoDescription`, not nested `seo.description`. Corrected the type/helper to use nonempty SEO values before title/summary fallbacks. Added two regression tests; the primary test failed before the fix and passed afterwards.

No backend, staff application, authentication/MFA, permission/menu, shared package, dependency manifest, lockfile, migration or local configuration changes were required.

## Verification results

| Check | Result |
| --- | --- |
| .NET Release solution build through `dotnet test` | Passed |
| Backend PostgreSQL suite | 169 passed, 0 failed, 0 skipped |
| Staff production build | Passed |
| Staff unit tests, sequential | 11 passed |
| Staff Playwright desktop/mobile suite | 20 passed |
| Public website production build after SEO correction | Passed; TypeScript passed; 24 generated entries |
| Public website unit tests after SEO correction | 38 passed (original 36 plus 2 SEO tests) |
| Real Edge browser against published disposable CMS data | 28 checks passed |
| Additional real-backend browser checks | 5 checks passed |

The first staff unit run had four timeouts while builds/tests were running concurrently (7 passed). Running all three test files with one worker passed all 11 without source or timeout changes. This suggests resource contention; it does not prove the parallel suite is stable. Existing nonblocking .NET documentation/xUnit warnings and a Vite config-loading warning remain.

`Verify-Cms.ps1` was inspected rather than rerun redundantly. Its CMS/blog/membership/browser-auth filtered tests are included in the successful full 169-test run against the corrected current backend. The full run includes 4 WebsiteTests, 4 BlogTests, 2 BrowserAuthTests, 14 MembershipTests, 64 MembershipSubmissionPolicyTests, 4 MembershipManagementTests and 2 MembershipFormTests, plus authentication/administration/MFA/session/provider tests.

### Real-browser coverage and limits

Both languages and all nine public sections returned successful pages with correct document language. Checked nested navigation with keyboard and mobile touch, no horizontal overflow at 390px, exact organization translation link, same-post blog fallback link, unpublished page rejection, public/private asset visibility, browser image decoding, hero background, later-page book search, past/future events, and sitemap pagination across 28 posts. Desktop/mobile screenshots were inspected for layout and readable hero text. Five additional checks verified actual SEO metadata, hero image configuration, blog language fallback, an actual backend outage during Load More, and successful retry after restart without duplicate books.

The browser data was synthetic content published in the isolated database, using actual backend entities/revisions. It was not the owner's production editorial content. Staff Playwright tests use mocked API responses; backend regressions and public browser checks use real PostgreSQL. This is Edge desktop/mobile emulation, not Safari/Firefox or a physical phone check. No claim of production launch readiness or full editorial acceptance is made.

Local evidence (ignored by Git):
- `TestResults/stage5/stage5-backend.trx`
- `TestResults/stage5/stage5-browser-smoke.json`
- `TestResults/stage5/stage5-browser-outage.json`
- `TestResults/stage5/stage5-home-desktop.png`
- `TestResults/stage5/stage5-home-mobile.png`

## Database and configuration safety

Only the disposable PostgreSQL cluster on `127.0.0.1:55439` was used:
- `theone_stage5_test`: full backend suite.
- `theone_stage5_browser_test`: migrated empty database and synthetic published browser fixtures.

The API used environment `Testing`, explicit test connection/JWT settings, disabled SMS and translation. No user-secrets or working connection settings were replaced. The working database on port 5432 was not used or migrated. No administrator accounts were created or changed. This frontend integration adds **no EF migration and requires no new migration SQL**. Existing Stage 1-4 database prerequisites still apply wherever those stages have not yet been installed.

## Decisions and disposition

- Ruling: use the existing checkout on the explicitly selected dedicated branch; no main-branch writes. Cost if wrong: user may need to switch branches to resume other work.
- Ruling: remove only the two obsolete root routes/layout files. This follows the incoming language-root design; the production route build and browser checks passed.
- Final review: one important SEO contract finding corrected with a failing-then-passing regression test. No other material integration blockers reported. No deferred minor findings were raised by this bounded final review.
- Retain dynamic server rendering/no-store behavior from the reviewed package; static revalidation and advanced SEO remain future work.
- Branch is retained locally for review. No main merge, push or deployment performed.

## Reproduce the automated checks

From the repository root, with an explicitly disposable PostgreSQL database available:

```powershell
$env:THEONE_AUTH_TEST_CONNECTION='Host=127.0.0.1;Port=55439;Database=theone_stage5_test;Username=theone_test'
dotnet test src/TheOne/TheOne.slnx -c Release
npm --prefix apps/management run build
npm --prefix apps/management run test:unit -- --maxWorkers=1
npm --prefix apps/management run test:e2e -- --workers=1
npm --prefix apps/website run build
npm --prefix apps/website test -- --maxWorkers=1
```

The task-owned servers/cluster are stopped after verification; start the disposable cluster before repeating database checks. Never substitute the working database connection.

## Next review steps

1. `git switch stage5-website-corrections`
2. `git --no-pager diff integration/stages-1-4..HEAD --stat`
3. `code docs/integration/stage5/INTEGRATION_RESULT.md`

Review local content/configuration before deciding whether to merge the branch. Configure `MEMBERSHIP_API_ORIGIN` to the intended API and `WEBSITE_PUBLIC_ORIGIN` to the site's real public URL when deploying; no production values are supplied here.
