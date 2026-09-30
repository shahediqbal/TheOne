# TheOne staging configuration handover

## Source of evidence

Values below were reported by the hosting operator through the user. They have not been independently checked from this workstation. No application deployment or migrations are reported performed.

| Item | Reported value / status |
|---|---|
| PostgreSQL host/alias | qsjcyvpeohtvxnsg5in2fyt3 |
| Port, database, user | 5432 / theone_staging / theone_staging_user |
| Network | coolify, 10.0.1.0/24 |
| Current database IP | 10.0.1.10; informational only, never use as connection hostname |
| Current Coolify proxy IP | 10.0.1.2; dynamic, IPAMConfig=null; not a permanent trusted peer |
| API persistent mount | /app/data-protection-keys; volume creation and ownership pending |
| Public / staff / API domains | staging.theone.markerbd.com / staff-staging.theone.markerbd.com / api-staging.theone.markerbd.com |
| Domain state | Names agreed; HTTPS routing not created |
| Restore pipeline | Disposable database creation, gzip readability and psql restore reported PASS |
| Schema/data restore | PENDING: current staging database has no application tables |

The DB alias is recorded as an explicitly confirmed hostname now, rather than inferred from a container ID. Keep it stable in Coolify and recheck resolution from the eventual API container.

## Resource mapping

All three builds use the repository root as context and an approved branch/commit, still pending.

| Resource | Dockerfile | Port | Configuration template |
|---|---|---|---|
| Staging API | deployment/docker/api.Dockerfile | 8080 | env/staging/api.env.example |
| Staging public website | deployment/docker/website.Dockerfile | 3000 | env/staging/website.env.example |
| Staging staff | deployment/docker/staff.Dockerfile | 80 | env/staging/staff.env.example |

Use VITE_WEBSITE_PUBLIC_ORIGIN as a staff Docker build argument. Other listed settings are runtime environment settings. Enter passwords/signing keys directly in Coolify. Use independent staging secrets and volumes. The Next.js NODE_ENV remains production for an optimized staging build; ASP.NET uses Staging. Database membership photos and CMS assets remain in PostgreSQL.

## Proxy trust decision

The latest hosting confirmation supersedes the earlier address-based template: 10.0.1.2 is dynamically assigned, and neither staging nor production should trust the shared 10.0.1.0/24 network. The template therefore contains no active trusted proxy address/subnet. ReverseProxy__Enabled remains true, so the API intentionally refuses startup until explicit trust is supplied; do not disable the guard to work around incomplete configuration.

After the approved branch/commit is supplied, hosting will create a dedicated TheOne staging proxy network for the Coolify proxy/staging routing layer, API and staff proxy path as required. Record the real CIDR, confirm which containers can join it, and set ReverseProxy__KnownNetworks__0 in Coolify. Do not guess the CIDR or internal API service alias now.

Verify that routing to the API actually uses the dedicated network: a multi-network container may otherwise connect over the shared coolify network. Record the immediate peers for both public API and staff /api requests. Database connectivity remains on its confirmed private network; network membership for DB access does not make that network trusted for forwarded headers. Ensure the dedicated network attachment survives resource/proxy recreation and that unrelated applications cannot join it.

ForwardLimit=1 is a starting bound, not proof of end-user IP correctness through the full Cloudflare/Coolify/staff chain. In staging verify HTTPS detection, forwarded host and the actual client IP used for rate limits/auditing on both API routes and staff /api routes. If processing more hops is necessary, trust and validate each intermediary and the edge header-overwrite behaviour before adjusting the bound. Never globally trust arbitrary forwarded headers.

## Remaining sequence

1. Review the existing uncommitted source changes, prepare the approved Git branch/commit and push only with user authorization. Configuration templates are prepared; no release commit is claimed.
2. Only after the approved branch/commit is provided, host creates the three staging resources with automatic deployment disabled, creates the dedicated staging proxy network, records its CIDR, API service alias and actual proxy peers, attaches the separate staging key volume and sets permissions for the API image user.
3. Complete template placeholders in Coolify settings; configure DNS/HTTPS routing and confirm private DB connectivity. Mount certificate material separately if using certificate-protected keys. Do not share production keys.
4. With deployment approval, deploy staging and separately review/apply the appropriate migration SQL to the empty staging database. No image or packaging script automatically migrates or seeds accounts.
5. Check /health, /health/ready and actual schema compatibility; verify staff login, MFA, refresh/logout, permission menus, membership, CMS and public assets over HTTPS. Restrict staging access appropriately at the edge and confirm it does not block required API access.
6. Add representative staging data. Repeat backup/restore into another disposable database, verify tables, data and usable key material, and record results. Empty-database restore is not application recovery validation.
7. Add application monitors. Production remains a separate release decision.

Only environment examples and this handover were added for the new infrastructure evidence. No secrets, production connection settings, runtime code, working databases or existing frontend edits were changed.

Release boundary: no resource creation yet; branch/commit approval is pending. Application deployment, database migrations and production release each remain subject to an explicit release decision.
