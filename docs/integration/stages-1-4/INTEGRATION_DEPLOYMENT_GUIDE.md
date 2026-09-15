# Manual integration and deployment guide — Stages 1–4

This guide accompanies the cumulative source ZIP. Actual integration into your local project, database migration execution, WordPress migration and production deployment remain Stage 6. Nothing has been pushed to GitHub or changed on sadarmawla.org.

## 1. Extract and choose your working copy

You have not applied the previous ZIPs. **Use only this cumulative ZIP**, extract it into a new folder such as `C:\Projects\Sadarmawla-Stages1-4`, and keep your old project separately. Do not unzip the older packages on top of this one.

The extracted folder contains this guide and `TheOne`. In Visual Studio, open `TheOne\src\TheOne\TheOne.slnx`. The API project is `src\TheOne\TheOne.API.csproj`; the other class libraries and integration tests are under `src`. Staff frontend: `apps\management`. Existing public membership/blog frontend: `apps\website`. Both use sibling source under `packages`; do not copy an app folder by itself.

This is based on your uploaded code, not subsequent unshared local changes. If your original project has changed, compare it with this package before merging. Preserve your private configuration, connection strings, identity data and Data Protection keys. The ZIP intentionally excludes dependencies, generated builds, certificates and live secrets. Earlier stage notes are historical; this guide supersedes their old migration commands and completion statements.

## 2. Prerequisites

Use Windows PowerShell/Windows Terminal, a Visual Studio installation that supports the solution's .NET 10 target, the **.NET 10 SDK**, PostgreSQL with command-line tools, and Node/npm compatible with the included lockfiles. Frontend builds in this workspace used Node 24.19.0. Use `npm ci` with each app's checked-in lockfile; do not run dependency upgrades as part of installation.

In a terminal:

```powershell
dotnet --list-sdks
node --version
npm --version
psql --version
```

If PostgreSQL tools are not on PATH, run them from your installed PostgreSQL `bin` directory. Keep PostgreSQL's installed version and matching tooling; no database server upgrade is required merely to try the core CMS. pgvector is optional and covered separately below.

## 3. Back up before integration

Make a copy of your existing source and private settings. Export your current development database before changing it; substitute its actual name and user:

```powershell
pg_dump -h localhost -U YOUR_DB_USER -d YOUR_EXISTING_DATABASE -Fc -f before-stages1-4.dump
pg_restore --list before-stages1-4.dump
```

Use PostgreSQL's password prompt or your existing credential configuration, not a checked-in password. Check the command's exit status/output. A database dump does not include your ASP.NET Data Protection key ring, external files or server-level roles: preserve those separately. Restore-test into a NEW database before relying on the backup:

```powershell
createdb -h localhost -U YOUR_DB_USER theone_restore_check
pg_restore -h localhost -U YOUR_DB_USER -d theone_restore_check --no-owner before-stages1-4.dump
```

Do not use `--clean` against your working database. For production, retain the established full backup/PITR process as well as the pre-change export.

## 4. Configure the API locally

From the extracted `TheOne` folder:

```powershell
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=theone_dev;Username=YOUR_USER;Password=YOUR_PASSWORD" --project src/TheOne
dotnet user-secrets set "Jwt:SecretKey" "YOUR_RANDOM_SECRET_OF_AT_LEAST_32_BYTES" --project src/TheOne
dotnet dev-certs https --trust
```

Use your actual local connection string. Retain your existing Jwt issuer/audience and other authentication/SMS secrets where already configured. For an existing installation, preserve signing and Data Protection keys rather than replacing them casually. Development user-secrets stay outside source control; production needs environment variables or a secret store.

`appsettings.Development.json` permits the staff browser origin `https://localhost:5173`. If you use another host/port, set the exact origin:

```powershell
dotnet user-secrets set "Browser:AllowedOrigins:0" "https://localhost:5173" --project src/TheOne
```

Membership translation and blog semantic search default to disabled. Membership registration does not require Google login or OTP and does not send resume tokens by SMS. Existing staff SMS/authenticator features still need their existing configuration.

## 5. Run the required backend gate on a disposable database

The backend and migrations have **not compiled/run in this delivery environment**. A source delimiter check cannot establish that EF mappings, queries or database constraints work. Run this gate before updating any working database.

Create a fresh database reserved for tests, with a name ending `_test` or `_tests`. Never reuse a database containing your real accounts or applications:

```powershell
createdb -h localhost -U YOUR_TEST_USER theone_cms_test
$env:THEONE_AUTH_TEST_CONNECTION = "Host=localhost;Database=theone_cms_test;Username=YOUR_TEST_USER;Password=YOUR_TEST_PASSWORD"
./scripts/Verify-Cms.ps1
```

The helper restores/builds the API/test project, then runs membership, browser authentication, blog and website regression tests against PostgreSQL. Tests apply migrations and create synthetic records. The existing model/snapshot consistency assertion is included. Keep the TRX output if a failure needs investigation.

A successful frontend build does not replace this gate. If restore fails, confirm package-feed access and the exact versions in the projects; do not silently downgrade packages. If compilation fails, fix the reported source error and rerun. If EF reports pending model changes, compare the supplied models/snapshot and regenerate the **new, unapplied** migration in a development checkout. Migrations in this package were authored without a running EF CLI; do not suppress the warning or edit already-applied migrations to conceal a mismatch. If a test fails, keep the working database unchanged until resolved.

## 6. Inspect database history and apply only after the gate passes

Check the target database's `__EFMigrationsHistory` in pgAdmin. The supplied baseline migration history and your database must agree. This source includes all previous membership/blog migrations plus `20260914150000_CompleteWebsiteCms`.

If you previously created membership/CMS tables manually without these EF history entries, a create-table migration will conflict. That situation needs an additive reconciliation/backfill migration. Do not drop those tables or fake migration-history rows. Your statement that ZIPs 1–4 were never applied makes a normal history-based upgrade likely, but inspect the database first.

From `TheOne`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef migrations list --project src/TheOne.Persistence --startup-project src/TheOne
dotnet ef migrations script --idempotent --project src/TheOne.Persistence --startup-project src/TheOne --output stages1-4-migrations.sql
```

Review the SQL and try it against a restored copy of your database. After it succeeds there, update your selected development database:

```powershell
dotnet ef database update --project src/TheOne.Persistence --startup-project src/TheOne
dotnet run --project src/TheOne --launch-profile https
```

The default API URL is `https://localhost:7198`; Swagger is `/swagger` in Development. Visual Studio can run the same HTTPS launch profile. Keep the API running while starting the frontends. Do not run two API instances on the same port.

## 7. Start the staff frontend

Open a second terminal in `TheOne\apps\management`:

```powershell
npm ci
npm run setup:https
npm run build
npm run test:unit
npm run dev
```

Open `https://localhost:5173`. The HTTPS setup exports the trusted development certificate into local `.certs`. For a different API port, set `$env:API_PROXY_TARGET = 'https://localhost:YOUR_PORT'` before starting Vite. Production cannot use Vite's development proxy/server.

Sign in with your existing administrator account and complete MFA. There is no packaged default password and this release does not automatically promote a user. An empty database therefore does not magically contain a usable SuperAdmin. Use your existing reviewed administrator-provisioning process or a protected development copy of the existing identity database.

SuperAdmin has all current permissions. Use Roles/Users to give staff the minimum required grants:

| Work | Permissions |
|---|---|
| View membership | `membership.read` |
| Review/verify applications | `membership.review` |
| Approve/reject membership | `membership.approve` (check existing role policy) |
| Assisted paper-form entry | `membership.read`, `membership.enter` |
| Record payments | `membership.contribute` |
| View/edit blog | `blog.read`, `blog.edit` |
| Approve/publish blog | `blog.read`, `blog.approve` and Admin/SuperAdmin role |
| View/edit CMS | `website.read`, `website.edit` |
| Approve/publish CMS | `website.read`, `website.approve` and Admin/SuperAdmin role |

Use `/membership`, `/blog`, `/website`. New permissions are in the permission catalog; older Admin grants are not silently widened. Reload/sign in again after role changes. PublicMenu records control future visitor navigation; staff menu/role settings are a separate system.

## 8. Start the existing public membership/blog frontend

In a third terminal at `TheOne\apps\website`:

```powershell
npm ci
Copy-Item .env.example .env.local
dotnet dev-certs https --format PEM --export-path dev-api.pem --no-password
$env:NODE_EXTRA_CA_CERTS = (Resolve-Path ./dev-api.pem).Path
$env:MEMBERSHIP_API_ORIGIN = "https://localhost:7198"
npm run build
npm test
npm run dev
```

Keep the exported certificate/key files private and out of Git. `NODE_EXTRA_CA_CERTS` trusts the development API certificate in this Node process; do not replace it with TLS-verification disabling. Re-export if your development certificate changes.

Open `http://localhost:3000/bn/membership` or `/en/membership`. Basic blog routes are `/bn/blog` and `/en/blog`. The package retains those earlier pages; it does not contain the completed organization homepage/navigation design from Stage 5.

The Next server proxies its existing membership/blog requests to `MEMBERSHIP_API_ORIGIN`. Keep that variable server-only, without `NEXT_PUBLIC_`. For production metadata, set `WEBSITE_PUBLIC_ORIGIN` to the real HTTPS public hostname at build/run time. This app needs a Node runtime because it contains server routes; do not set `output: 'export'` or upload it as static HTML.

## 9. Local acceptance checks

- Create a synthetic application; keep its one-time private resume token. Save/reopen the draft, complete required fields/photo/three declarations, submit, and confirm incomplete submissions are blocked.
- In staff membership, verify it, record the BDT 100 membership fee, approve it and confirm its membership number stays stable when retrying. A donation/monthly record must not satisfy the fee gate. Test duplicate bKash references with synthetic values.
- In Website, save a Page; it must be absent from public APIs before approval/publication. Approve/publish, edit a draft and confirm the old live revision remains visible. Check a stale second tab cannot overwrite newer content. Restore must produce a draft.
- Create/publish an author profile for existing staff with blog access; select it in a blog. Publish the article only after administrator approval, then create an ordered series. Repeat for the other language. Check fallback language and old-slug behaviour through the APIs.
- Mark content AI-assisted and confirm it cannot proceed without administrator human review. An Editor must not obtain approval privileges simply through UI navigation.
- Test CMS images using real JPEG/PNG examples; unpublished uploads and membership photos must not be publicly accessible.

Staff Playwright tests can run locally with installed Microsoft Edge:

```powershell
# From apps/management
npm run test:e2e
```

These are mocked browser checks, not a live-system deployment test. Also manually check mobile widths, Bangla text, keyboard navigation, refresh/login/logout and actual API errors. No real-browser suite ran in this workspace.

## 10. Optional pgvector setup

Leave `BlogSearch:Enabled=false` unless you are intentionally configuring semantic search. Core membership/CMS migrations do not require pgvector.

1. Install the pgvector extension matching your PostgreSQL server installation using its official instructions. On Windows its native build requires the matching PostgreSQL development installation and Visual Studio C++ tools; this is distinct from the .NET workload. A compatible preconfigured database service is another hosting decision.
2. After the CMS EF migration, run `TheOne/deployment/sql/enable-blog-pgvector.sql` in your intended database using pgAdmin or `psql -v ON_ERROR_STOP=1 -f ...`. It creates the extension, adds `Embedding vector(384)` and the cosine HNSW index. No embedding provider is called.
3. Select one 384-dimensional model and configure its exact identifier as `BlogSearch:EmbeddingModel`; only then set `BlogSearch:Enabled=true` and restart the API.
4. A separately configured indexing provider/worker generates the current published revision's vector and uploads it through the administrator embedding API. Search callers must supply a vector from the same model. Plain-text query conversion and automatic re-indexing are Stage 6 provider integration work, not an active capability in this ZIP.

The optional vector column/index is deliberately outside the core EF model. Preserve it in database backups; do not regenerate the core migration just to absorb the optional feature without a deliberate migration decision. Publication invalidates stale embeddings. Configuration alone does not populate the index.

## 11. Prepare production artifacts — instructions only

Complete the local .NET/PostgreSQL and browser gates first. From `TheOne`:

```powershell
dotnet publish src/TheOne/TheOne.API.csproj -c Release -o artifacts/api
Set-Location apps/management
npm ci
npm run build
Set-Location ../website
npm ci
npm run build
```

Deploy API publish output, staff `dist`, and the Next app with its runtime dependencies and `.next` output (preserving shared package paths where needed). Use Node-compatible hosting for the public app. Do not upload local development certificates or `.env.local` to a publicly served directory.

Reference Windows hosting layout:

| Component | Hosting arrangement |
|---|---|
| ASP.NET API | IIS with .NET 10 Hosting Bundle, separate application pool and HTTPS binding |
| Staff SPA | Static `dist` under staff HTTPS hostname, `/api/*` proxied to API; all other non-file SPA routes fall back to index.html |
| Existing public app | `npm run start` under a supervised Node process, bound to loopback port 3000, HTTPS reverse proxy in front |
| PostgreSQL | Private database access from API, with backups and appropriate runtime/migration credentials |

In IIS, install the Hosting Bundle after enabling IIS, create the API application pool/site, point it at `artifacts/api`, set the pool to No Managed Code, configure its identity and certificate, and apply the environment/secret settings below. Follow Microsoft's instructions for persisting and protecting Data Protection keys under that application pool. Those keys protect existing authenticator material as well as previews; backing up only PostgreSQL is insufficient.

For the staff site, configure IIS URL Rewrite/ARR (or your chosen reverse proxy): evaluate `/api/*` before the SPA fallback, proxy to the real API HTTPS origin, preserve request method/body/Origin/cookies, and disable API caching. Explicitly allow the staff HTTPS origin in the API. Do not rewrite API failures to index.html. For the public hostname, proxy to the supervised Next process; its own API routes continue to proxy to the backend. Register Node with the hosting platform's process supervisor/service so closing a terminal does not stop the site.

Production API configuration uses keys such as:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<production database connection>
Jwt__SecretKey=<existing protected signing secret>
Jwt__Issuer=<configured issuer>
Jwt__Audience=<configured audience>
Browser__AllowedOrigins__0=https://staff.YOUR-DOMAIN
AllowedHosts=<actual API/proxy hosts separated by semicolons>
BlogSearch__Enabled=false
```

Public Node process:

```text
NODE_ENV=production
MEMBERSHIP_API_ORIGIN=https://YOUR-API-HOST
WEBSITE_PUBLIC_ORIGIN=https://YOUR-PUBLIC-HOST
```

Use a secret store or protected process configuration. No secret belongs in Vite environment variables or browser bundles. Preserve HTTPS between services, or explicitly configure trusted forwarding before terminating TLS elsewhere. The supplied API has not been configured for your proxy topology.

**Stage 6 hosting work still required:** trusted forwarded-host/scheme/client-IP handling, edge rate limits, persistent Data Protection, TLS/DNS bindings, service supervision, request/upload limits, backups/restore checks and monitoring. The Next-to-API proxy currently presents shared server IPs to API rate limits; do not assume per-applicant production quotas are solved. Configure trusted forwarding/edge enforcement for the chosen topology; never trust arbitrary client-supplied forwarding headers. Verify secure refresh cookies and real IP behaviour end-to-end before go-live.

These are deployment instructions and requirements, not a claim that a ready-to-run server configuration has been deployed. No DNS changes, uploads, production migrations or cache-invalidation worker execution have occurred here.

## 12. Publication, migration and recovery after hosting is chosen

Run the reviewed idempotent migration SQL against a restored production copy first. For the actual release, back up, stop/drain writes as appropriate, apply the reviewed SQL with the migration credential, deploy matching code and run the acceptance checks. Keep normal runtime database privileges separate from migration privileges.

CMS publication emits durable events; an eventual regeneration consumer must process idempotently and acknowledge only after success. There is no active ISR worker now. Stage 5 builds the new public pages; Stage 6 wires event processing and performs WordPress inventory/import and old-to-new URL mapping. Do not switch the current public website's DNS merely because the CMS builds.

Rollback does not mean executing EF Down on valuable data: the new migrations' Down methods drop membership/blog/CMS tables. Prefer a forward fix; if necessary restore the verified pre-release backup into a separate database and switch back with the matching prior code/configuration. Reconcile any writes made after the backup rather than silently losing them. Preserve Data Protection keys across rollouts. Use a maintenance window and a documented recovery point when production integration is actually scheduled.

## Troubleshooting

| Symptom | Check |
|---|---|
| Solution/target framework not supported | Installed .NET 10 SDK and Visual Studio support; try CLI build for the exact error |
| Database connection failure | Correct database/user/password/port and PostgreSQL service, without exposing credentials |
| Relation already exists / missing EF history | Stop; reconcile prior manual schema changes instead of dropping tables |
| Staff login 403 | HTTPS on both hops, exact Browser allowed origin, MFA/current grants |
| Authenticator fails after deployment | Original Data Protection key ring and application identity/name |
| Staff route 404 after refresh | SPA fallback after `/api` proxy rules |
| Public requests return 502/certificate error | API running, MEMBERSHIP_API_ORIGIN and Node certificate trust |
| All applicants share 429 responses | Proxy/client-IP and edge rate-limit integration remains incomplete |
| CMS record absent publicly | Administrator approval followed by Publish; correct language/kind; published references |
| Author cannot be selected | Existing staff blog permission, published AuthorProfile in the editor's language |
| Semantic search 503 | Expected while disabled; install extension/SQL and configure model before enabling |
| Semantic search fails after enabling | Optional SQL not applied, incompatible vector dimensions/model or no indexed published revisions |

## Primary hosting references

- Microsoft IIS/.NET hosting: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0
- Hosting Bundle: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/hosting-bundle?view=aspnetcore-10.0
- Next.js self-hosting: https://nextjs.org/docs/app/guides/self-hosting
- PostgreSQL dump/restore: https://www.postgresql.org/docs/current/app-pgdump.html and https://www.postgresql.org/docs/current/app-pgrestore.html
- pgvector installation/vector indexes: https://github.com/pgvector/pgvector

Hosting references were checked on 14 September 2026. Commands that refer to this package are grounded in its source; topology-specific production settings must be finalized in Stage 6.
