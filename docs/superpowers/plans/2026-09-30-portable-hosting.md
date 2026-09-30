# Portable hosting implementation plan

Goal: package the same application for Linux Docker/Coolify and Windows IIS hosting without coupling business logic to either provider.

Constraints: preserve current uncommitted frontend work. No deployments, pushes, database migrations or application-server starts. Do not infer internal hostnames or trusted network ranges. Do not export secrets into packages.

1. Add explicit proxy trust, persistent Data Protection and separate liveness/readiness checks. Verify with database-independent tests.
2. Package API, Next.js standalone server and Vite SPA using repository-root Docker builds. Keep resource names, domains and secrets configurable.
3. Provide Windows publish scripts and IIS templates; account for process.env.PORT and OS-specific Node dependencies.
4. Provide configuration examples and migration/backup/restore instructions. Current uploads are database byte arrays, not filesystem objects; retain that design.
5. Run .NET tests/publish and frontend tests/builds. Report Docker/IIS runtime checks as pending if unavailable locally.

Release gates: exact branch/commit published only after separate authorization; DB network/hostname, trusted proxy path, volumes and domain settings supplied by host operator; backup restore and staging authentication/MFA tests before production release.

## Local result

Steps 1–4 implemented. Step 5 local builds/package creation and tests completed; see deployment/VALIDATION-2026-09-30.md for exact results and hosting-runtime checks still pending. No release actions performed.
