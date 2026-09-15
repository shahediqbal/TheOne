# Verification — cumulative Stages 1–4

Recorded 14 September 2026. This file supersedes older stage verification claims.

| Check | Result |
|---|---|
| Staff TypeScript + Vite production build | Passed |
| Staff Vitest DOM tests | 11 passed (membership 5, blog 3, website CMS 3) |
| Existing public Next.js production build | Passed |
| Public Vitest DOM tests | 8 passed |
| JSON/XML/project-reference and selected C# delimiter consistency | Passed; 50 membership/blog/website/browser/snapshot source files checked |
| .NET API and test project compilation | Not run — no .NET SDK in this environment |
| EF migration application and model/snapshot assertion | Not run — requires .NET and disposable PostgreSQL |
| PostgreSQL concurrency/transaction/relationship regressions | Supplied; not executed here |
| pgvector optional extension SQL and semantic queries | Not executed here |
| Playwright/real browser and live API end-to-end tests | Not executed here |
| WordPress migration, production hosting and deployment | Not performed; Stage 6 deferred |

Frontend builds used Node 24.19.0 with the app lockfile dependencies already available in the workspace. The package excludes node_modules and build outputs; local installation uses npm ci. Tests use mocked requests/simulated DOMs and cannot establish real authentication, SQL, browser rendering or deployment correctness.

The new CMS DOM tests check administrator confirmation/version payload, hiding approval from a non-administrator even when a permission is present, and retaining unsaved text after a conflicting save. Existing tests cover membership and blog interactions. An initial CMS test used the wrong capitalization for the Approve button; corrected, then the full staff build and all 11 tests passed.

WebsiteTests adds backend cases for forbidden links/fields, invalid event times/coordinates/publication availability, cyclic menus and duplicate series posts, approved live revision preservation through draft/restore/conflict, and AI provenance/preview invalidation. Those tests have not run. Existing membership tests include the whole model/snapshot assertion. EF migrations were authored without executing the EF CLI, so local validation is a required gate, not an optional assurance.

Run from TheOne with THEONE_AUTH_TEST_CONNECTION pointing to a disposable database ending _test or _tests:

```powershell
./scripts/Verify-Cms.ps1
```

The script compiles the test project/API and executes the membership, browser-authentication, blog and website filter. Stop on failure; do not apply working/production migrations until resolved. See INTEGRATION_DEPLOYMENT_GUIDE.md for setup and migration-history checks.

The Python check at verification/check_sources.py verifies packaging/source consistency only. It is not a C# compiler, EF validator, security audit or substitute for the PostgreSQL test gate.

## Corrective update verification

See CORRECTIVE_UPDATE.md for this revision. Static mapping coverage passes for 41 stored fields, 34 typed length rules and 28 required accessors. The policy and downstream validator contain no runtime property reflection. The new MembershipSubmissionPolicyTests are supplied but have not run. Formatting-only token comparison passed for the two CMS files; the corrected SVG was rendered and visually inspected. Frontend source did not change, so the prior two builds and 19 passing DOM tests were not rerun.

Run `python verification/check_membership_mapping.py` from the extracted package root for the additional static check. This does not replace compilation or C# execution.
