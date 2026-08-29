# CI/CD Pipeline Document

Expands `Hosting-Strategy.md`. Implements the flow referenced in `Git-Branching-Strategy.md`.

## Environments

| Environment | Trigger | Purpose |
|---|---|---|
| Preview (per-PR) | Pull Request opened/updated against `develop` | Reviewer sanity-check, no shared state |
| Staging | Merge to `develop` | Full integration testing, E2E, manual QA sign-off |
| Production | Tag `vX.Y.Z` pushed to `main` | Live system |

Matches the phased hosting approach: Phase 1 targets shared hosting (SmarterASP.NET/Hostinger per `Hosting-Strategy.md`), so early pipelines deploy via web-deploy/FTP-style publish; the Docker/container stages below activate once the project moves to the "Future: Cloud, Containers" phase noted in that same document.

## Pipeline Stages

### 1. Pull Request (into `develop`)

```yaml
on: pull_request
jobs:
  build-and-test:
    steps:
      - checkout
      - setup-dotnet (target framework per Software-Architecture-Document.md)
      - dotnet restore
      - dotnet build --configuration Release
      - dotnet test  # xUnit + Moq unit tests, WebApplicationFactory + TestContainers integration tests
      - setup-node
      - npm ci (frontend)
      - npm run lint
      - npm run test  # Vitest
      - npm run build # Next.js build check
```

Merge is blocked unless all steps pass — matches the CI gates already defined in `Testing-Strategy.md`.

### 2. Merge to `develop`

```yaml
on:
  push:
    branches: [develop]
jobs:
  deploy-staging:
    needs: [build-and-test]
    steps:
      - run full test suite again (guards against non-deterministic PR-time skips)
      - build artifact / Docker image, tag :staging
      - deploy to Staging environment
      - run EF Core migrations against Staging DB (with backup-before-migrate step)
      - run Playwright E2E suite against deployed Staging URL
      - notify team (Slack/Teams) with results
```

### 3. Release (tag `vX.Y.Z` on `main`)

```yaml
on:
  push:
    tags: ['v*.*.*']
jobs:
  deploy-production:
    steps:
      - build artifact / Docker image, tag :vX.Y.Z and :latest
      - require manual approval (GitHub Environment protection rule)
      - run EF Core migrations against Production DB
          # Production migration requires prior approval per Database-Design.md
      - deploy (blue-green where the hosting target supports it; sequential restart on shared hosting)
      - run health checks against production endpoints
      - automated rollback to previous tag on health-check failure
      - tag release in GitHub Releases with changelog notes
```

## Secrets Management

- No secrets committed to the repo, ever — `.env.example` documents required variables with placeholder values only.
- Connection strings, JWT signing keys, bKash API credentials, MFA encryption keys stored in the CI/CD platform's secret store (GitHub Actions Secrets or equivalent) and injected at deploy time.
- Secret rotation: JWT signing key and bKash credentials rotated on a defined schedule (owned by DevOps); rotation events logged to `AuditLogs`.

## Database Migrations in the Pipeline

- Migrations are generated locally during development, committed to the repo (EF Core Code First, per `Database-Design.md`), and applied by the pipeline — never run ad hoc against Production by a developer's machine.
- Staging migration failures block promotion to Production automatically.
- A migration that only adds (new column, nullable; new table) can run pipeline-automated. A migration that alters/drops requires the explicit human approval gate already noted in `Database-Design.md`.

## Rollback Strategy

| Scenario | Action |
|---|---|
| Health check fails post-deploy | Automated rollback to last known-good image/tag |
| Migration fails mid-deploy | Deploy halts; DB restored from pre-migration backup (see `Backup-Disaster-Recovery.md`); code rollback follows |
| Post-release bug found | `hotfix/*` branch per `Git-Branching-Strategy.md`, fast-tracked through the same pipeline with expedited (not skipped) review |

## Monitoring Post-Deploy

- Structured logs (Serilog) shipped to the monitoring stack from all environments.
- Deploy events themselves are logged/annotated on dashboards so a metric spike can be correlated to a specific release.
