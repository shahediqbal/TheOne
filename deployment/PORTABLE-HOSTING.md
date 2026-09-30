# TheOne portable hosting

Status: application packaging/configuration prepared locally. No deployment or database migration is performed by these files. Linux container runtime and IIS provider validation remain release gates.

## Three independently deployable applications

| Application | Source | Linux package / internal port | Windows package |
|---|---|---|---|
| API | src/TheOne/TheOne.API.csproj | deployment/docker/api.Dockerfile / 8080 | dotnet publish, .NET 10 IIS hosting bundle |
| Public website | apps/website | deployment/docker/website.Dockerfile / 3000 | Next standalone + Node 24 + httpPlatformHandler |
| Staff | apps/management | deployment/docker/staff.Dockerfile / 80 | Static files + IIS URL Rewrite and ARR |

All Docker builds use the repository root as build context. Do not use a frontend folder as context: shared source lives in packages/. The API and website are separate processes; npm run dev continues to start only the public website locally.

## Step 1 — obtain host values

Record exact private PostgreSQL hostname, port and network, immediate API proxy addresses/networks, domain names, TLS routing, persistent key volume and permissions. Container IDs are not database hostnames. Keep these values in hosting environment settings; examples in deployment/env contain placeholders and must not be deployed unchanged.

Use separate databases, keys and resources for production and staging. Do not copy local administrator accounts or create default credentials. Use the existing bootstrap process only when initializing a genuinely new environment.

## Step 2 — secrets and proxy trust

API: inject ConnectionStrings__DefaultConnection, Jwt__SecretKey (at least 32 bytes), issuer/audience, AllowedHosts, Browser__AllowedOrigins__0 and persistent DataProtection__KeysPath. Production/Staging refuses to start without a key path. Keep keys outside the release folder. Do not commit secrets or put them in VITE_* variables; those are public build output.

Set ReverseProxy__Enabled=true behind Linux reverse proxies and configure KnownProxies or KnownNetworks plus ForwardLimit. Do not set ASPNETCORE_FORWARDEDHEADERS_ENABLED. Trust only the actual immediate proxies, including the staff nginx container/network for staff /api requests. Do not assume the immediate peer is Cloudflare. Avoid trusting a Docker network containing unrelated applications. Validate forwarded scheme, host and client IP in staging. Proxy middleware runs before HTTPS redirection and authentication. When explicit proxy handling is disabled, IIS integration retains its own defaults.

All external domains must enforce HTTPS. The staff nginx template assumes its edge terminates HTTPS and sends https to the API; do not expose it as a plain HTTP public service. The staff application calls /api on its own origin: preserve this reverse proxy route so secure authentication cookies continue to work. Include staff and API hostnames in AllowedHosts, and the HTTPS staff origin in Browser__AllowedOrigins. Do not cache auth/API traffic in Cloudflare or the host proxy.

Data Protection protects persisted authentication material, including MFA secrets. Back up the key ring alongside the database. Restrict volume permissions to the application account. Explicit filesystem persistence does not itself encrypt keys at rest; use encrypted storage or configure DataProtection__CertificatePath and CertificatePassword with a separately managed PFX. Existing DPAPI-encrypted Windows keys cannot simply be copied to Linux and assumed usable. Cross-host migration must prove old keys can decrypt existing material; retain old decrypting certificates when rotating. Never discard keys to make startup succeed.

## Step 3 — Linux / Coolify

Build commands, from the repository root:

```powershell
docker build -f deployment/docker/api.Dockerfile -t theone-api:review .
docker build -f deployment/docker/website.Dockerfile -t theone-website:review .
docker build -f deployment/docker/staff.Dockerfile --build-arg VITE_WEBSITE_PUBLIC_ORIGIN=https://theone.markerbd.com -t theone-staff:review .
```

In Coolify create three resources for the exact approved branch/commit using these Dockerfiles, root context and ports above. API requires a writable persistent mount at /app/data-protection-keys, owned by the image app account (verify UID before assigning permissions). Website runs as node. Connect API and staff proxy through the appropriate private network. Set staff API_UPSTREAM to the real internal API HTTP origin, with no trailing slash. Set the public website MEMBERSHIP_API_ORIGIN to the reachable HTTPS API origin and configure WEBSITE_PUBLIC_ORIGIN and MANAGEMENT_PUBLIC_ORIGIN at runtime. robots.txt is dynamic so its sitemap origin follows runtime configuration. Staff VITE_WEBSITE_PUBLIC_ORIGIN is a build argument: changing it requires rebuilding staff.

Current membership photos and CMS assets are stored as database byte arrays. They are included in database backups; /app/data/uploads is only a proposed future volume, not a working storage setting. Do not migrate uploads to files as part of hosting preparation.

The website cache is disposable. Persistent keys and PostgreSQL data are not. Set restart policies and resource limits in the hosting platform. Pin tested image digests for releases after staging validation.

## Step 4 — Windows / IIS alternative

Before selecting a plan, obtain provider confirmation of .NET 10, compatible Node 24, a persistent Next.js server process (not merely npm build), httpPlatformHandler or an equivalent Node process manager, URL Rewrite + ARR proxy permission, PostgreSQL compatibility, persistent writable key storage, secrets, and restore/export access. React static hosting alone does not prove Next.js SSR support. These templates are conditional examples, not evidence that a particular SmarterASP.NET plan supports them.

On Windows, from the repository root:

```powershell
.\deployment\Publish-Windows.ps1 -WebsiteOrigin https://theone.markerbd.com -ApiOrigin https://api.theone.markerbd.com
```

The script builds three upload folders beneath a new artifacts/windows-* directory; no existing release is overwritten and nothing is uploaded. It excludes .env files, certificate files and environment-specific appsettings from final packages. Keep base appsettings.json secret-free and inspect the package before upload. A failed build may leave a partial local artifacts directory; never upload a partial package. Native Node dependencies must match the destination Windows architecture; these packages target x64.

API: upload api/, retain generated web.config and use an IIS app pool with the matching hosting bundle. Set secrets through provider settings. Give its identity access to a persistent key directory outside the site. Website: upload the entire website/ tree, not just apps/website; set the three runtime origins and use a provider-approved Node executable path in web.config if node is not on PATH. httpPlatformHandler supplies its assigned port. Staff: upload staff/ to its HTTPS site. ARR must proxy /api to the API without caching or stripping secure cookies. Validate Origin/forwarded-header behaviour under the provider's actual proxy setup. If provider cannot offer these capabilities, host the unsupported component elsewhere or choose a Linux VPS; do not silently convert Next.js into a static export.

## Step 5 — staging and migration gate

1. Review the exact branch/commit, package and environment settings.
2. Restore a backup to an isolated staging database first; verify counts and data. Use compatible PostgreSQL versions (current infrastructure is PostgreSQL 18). Do not restore over production.
3. Review existing EF history against the destination, generate the appropriate migration SQL, and approve it separately. These images/scripts never run migrations or seed accounts.
4. Restore/provision the correct staging key ring, then start staging resources through the host platform.
5. Verify API /health (process only) and /health/ready (database connectivity only, not schema compatibility). Both expose generic status, no credentials. Add website /en and staff / monitors.
6. Exercise real login, MFA, refresh/logout, permissions, staff navigation, membership, CMS publication, assets and website content over HTTPS. Verify application restart preserves keys and sessions as intended. Check origin/cookie/proxy behaviour and no-store responses.
7. Demonstrate restore of database and keys; record timings, restore commands and rollback plan. Provider backup success alone is not a restore test.
8. Only after approval, deploy production with separate secrets/volumes, reviewed migrations and monitoring. Keep previous artifacts and compatible database rollback procedures.

Host values still pending: private DB hostname/network, trusted proxy subnet, volume creation/ownership, final domains, TheOne restore test, and application monitoring. No production release is authorized by this document.
