# Local validation — 2026-09-30

Initial validation used feature/public-editorial-design with local uncommitted changes. Existing frontend work was retained. The subsequent local commit preparation is recorded below.

| Check | Result |
|---|---|
| dotnet test src/TheOne.IntegrationTests --filter FullyQualifiedName~PortableHostingTests --verbosity quiet | 4 passed, 0 failed; no database used |
| Website npm test -- --run | 45 passed, 0 failed |
| Staff npm run test:unit | Final rerun: 12 passed, 0 failed |
| Publish-Windows.ps1 with planned production origins | Exit 0; Windows x64 .NET publish, Next standalone build, Vite build and all three package folders completed |
| Package entry files and IIS XML | Passed |
| Final packages: .env / certificates / environment-specific appsettings excluded | Passed; base connection/signing settings also checked empty |
| git diff --check | Passed; line-ending notices only |

First staff invocation mistakenly used npm test -- --run (Playwright rejects --run); corrected to test:unit. First unit run during concurrent builds passed 11/12 with a 15-second membership-test timeout. The unchanged suite passed 12/12 after builds finished. This does not establish why it timed out; no timeout increase or test change was made.

Generated local review packages: artifacts/windows-20260930-120318-960/{api,website,staff}. Packages are ignored by Git and contain the current working-tree frontend changes. They are review artifacts, not approved production releases.

Warnings: API XML-documentation warnings; website Vitest future config-loader warning. Neither blocked the successful checks.

Not run: Docker image builds/container execution (Docker unavailable on this PC), IIS runtime/ARR validation, real PostgreSQL-backed integration regressions, browser authentication/MFA against production proxy topology, backup restore. No application servers were started. No working database was accessed or migrated. No deploy, push, commit or main-branch merge performed.

Next: hosting operator supplies internal DB/network, proxy trust and persistent volume values; validate containers and HTTPS authentication in isolated staging, restore-test backups, then obtain production release approval.

## Staging commit preparation — 2026-09-30

Candidate branch: staging/portable-hosting, based on bdb2af625a86cade489a9632b739cf872de97da2. Includes the existing editorial UI, simple paternal/maternal family tabs, development launcher and the portable hosting preparation. No migration files changed. See git log on this branch for the resulting commit ID.

An independent read-only code review of tracked and untracked changes found no actionable serious issues. It noted remaining Linux/IIS runtime, forwarded host/client IP and multi-hop proxy, key persistence/restoration and responsive family-layout validation gaps. These remain staging checks, not claims of successful production testing.

Fresh checks before committing:
- Website: npm test -- --run — 45/45 passed.
- Staff: npm run test:unit — 12/12 passed, no timeout on this run.
- API: database-independent PortableHostingTests — 4/4 passed; test command also compiled the API.
- Website: npm run build — passed; robots.txt remains dynamic.
- Staff: npm run build with VITE_WEBSITE_PUBLIC_ORIGIN=https://staging.theone.markerbd.com — passed.
- Pending-file credential filename/common-token-marker scan — no findings; this is a limited automated check, not a comprehensive security audit.
- Git ignore checks — generated packages, local environment files and development certificate are excluded.
- Staging template has no active trusted proxy peers until the dedicated network is supplied; dynamic 10.0.1.2 and shared 10.0.1.0/24 are not trusted.

Only local branch/commit preparation is authorized at this point. Push, resource creation, deployment and migrations remain pending. No application servers were started for these checks.
