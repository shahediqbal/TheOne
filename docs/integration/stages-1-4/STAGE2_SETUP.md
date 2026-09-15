# Stage 2 — Staff membership management

This ZIP is **cumulative**: it includes the Stage 1 backend corrections plus the Stage 2 backend additions and the complete React staff application. You can wait until the final combined delivery before applying it. Keep this package as a checkpoint; do not overlay the old Stage 1 ZIP on top of it.

## Included

- Staff membership queue: search by name, phone, application reference or membership number; status filter and pagination.
- Application details, decision history, entry channel, staff IDs and payment history. NID is concealed visually until explicitly revealed by an authorized staff reader; the read permission protects the API response itself.
- Verification, approval and rejection with explicit confirmation/reason, permission checks and Stage 1 workflow rules.
- Manual bKash/cash recording, clear payment categories, duplicate bKash reference errors and the BDT 100 approval fee gate.
- Paper-form entry with required basic fields, additional bilingual fields, optional enums/help categories, recorded applicant consent, review-before-submit and retry-safe request IDs. It creates a Submitted application and PendingApproval member; it does not approve membership or create an Identity account.
- Bangla/English membership interface labels.
- Browser sign-in endpoints needed by the supplied management frontend. These were missing from the supplied backend: refresh tokens are confined to an HTTPS Secure HttpOnly SameSite=Strict cookie and are omitted from browser JSON.

No public membership form, website CMS, blog or reading app is included in Stage 2. Those follow in later stages.

## Verification

- **Frontend TypeScript check and production build passed.**
- **Five frontend interaction tests passed** (simulated DOM and mocked API): fee-gated approval, approval confirmation, failed-payment field retention, NID reveal and paper-entry retry ID preservation.
- **Twelve Playwright cases discovered** (six scenarios × desktop/mobile); execution was blocked by unavailable browser binaries and failed browser downloads. No visual/browser QA is claimed.
- **Twenty backend regression tests supplied**: Stage 1's fourteen plus four staff-management tests and two browser-cookie/security tests. Not compiled or run here: the .NET SDK is unavailable and download attempts failed. Backend/real-database integration remains a required local gate.
- JSON, project references and targeted C# source consistency checks passed. These are not a substitute for compilation.

See `VERIFICATION.md` for the exact limits.

## Folder layout

- `TheOne/src/` — full ASP.NET Core backend, migration and test source.
- `TheOne/apps/management/` — full Vite + React + TypeScript + MUI staff frontend.
- `TheOne/scripts/Verify-Membership.ps1` — backend build/test gate against a disposable PostgreSQL database.
- `STAGE1_SETUP.md` — backend prerequisites, development secrets and initial migration instructions.
- `CHANGED_FILES.md` — differences from Stage 1; the supplied frontend is included as an added source tree.

Dependencies, compiled output, local HTTPS certificates, caches and real credentials are excluded. The frontend builds its own `dist/` directory after installation.

## When you are ready to install

Extract into a new folder and keep your current checkout intact. If you have local code newer than the uploaded ZIPs, merge the listed changes instead of blindly replacing that newer checkout.

### 1. Backend

Use .NET 10 and PostgreSQL. Follow `STAGE1_SETUP.md` for connection/JWT secrets and migration setup, using **this cumulative backend**. Stage 2 adds no new database migration; the existing Stage 1 migration creates the membership tables.

From `TheOne/`, set `THEONE_AUTH_TEST_CONNECTION` to a disposable database whose name ends in `_test` or `_tests`, then run:

```powershell
./scripts/Verify-Membership.ps1
```

This gate now includes all membership tests and the new browser-auth tests. Only after it passes should you apply the included Stage 1 migration to your development database and start the API:

```powershell
dotnet run --project src/TheOne --launch-profile https
```

The launch profile serves the API at `https://localhost:7198`.

### 2. Staff frontend

From `TheOne/apps/management/`:

```powershell
npm ci
npm run setup:https
npm run build
npm run test:unit
npm run dev
```

Open `https://localhost:5173`. The setup script uses your .NET development certificate. The API and frontend must both run. Browser tests default to installed Microsoft Edge:

```powershell
npm run test:e2e -- e2e/membership.spec.ts
```

Alternatively, after installing Playwright Chromium locally, set `$env:PLAYWRIGHT_CHANNEL="chromium"` for the browser-test command. Real browser tests use mocked API responses; run the backend regression gate separately.

### 3. Grant staff permissions

Sign in with your existing MFA-enabled SuperAdmin account. SuperAdmin receives all server-defined permissions. Grant appropriate role permissions through Roles and permissions:

| Permission | Purpose |
|---|---|
| `membership.read` | Open queue, application details, history and payment records |
| `membership.enter` | Enter a paper application; also requires `membership.read` |
| `membership.review` | Verify submitted applications |
| `membership.approve` | Approve or reject eligible applications |
| `membership.contribute` | Record payments |

Grant `membership.read` to staff who need to use the other actions through the UI. An ordinary Admin role does not automatically receive these permissions. Existing API permission checks require MFA.

A Membership navigation entry appears automatically for staff with read access. If using the existing menu editor, add `/membership` with `membership.read`; the fallback navigation item avoids a duplicate when that route is already configured. Role grants are fetched at sign-in/reload; staff should sign in again after their grants change. The backend checks current grants on every protected request.

## API additions

| Method | Route | Behaviour |
|---|---|---|
| GET | `/api/v1/admin/membership/applications` | Bounded page/search/status query |
| GET | `/api/v1/admin/membership/applications/{referenceCode}` | Details and decision/payment history; no resume secret/hash |
| POST | `/api/v1/admin/membership/operator` | Atomic staff-entered submission |
| POST | `/api/v1/browser/auth/login` | Password login/MFA challenge; refresh credential only in cookie |
| POST | `/api/v1/browser/auth/authenticator` | Complete authenticator challenge |
| POST | `/api/v1/browser/auth/recovery-code` | Complete recovery-code challenge |
| POST | `/api/v1/browser/auth/otp` | Existing eligible regular-account OTP flow; does not bypass staff MFA |
| POST | `/api/v1/browser/auth/refresh` | Rotate cookie-backed session |
| POST | `/api/v1/browser/auth/logout` | Revoke session and clear cookie |

The existing Stage 1 verify/approve/reject/contributions routes remain unchanged. Staff-entered requests use `{ requestId, fields, codeOfConductAccepted }`. Reuse the same UUID and same fields only when retrying the same submission: the server returns the committed record. Changed fields with a used UUID return 409. The ID is held in the open form; do not close/reopen a form after an uncertain response without checking the queue first.

## Deployment compatibility

Serve the staff frontend and its `/api` proxy under one HTTPS origin. Browser auth requires both a valid HTTPS Origin and `X-TheOne-Client: web`. Development explicitly allows `https://localhost:5173` through `appsettings.Development.json`; production does not inherit that allowlist. Any production origin exception must be an explicit exact value in `Browser:AllowedOrigins`, never a wildcard.

The cookie is `__Host-TheOneRefresh`, HttpOnly, Secure, SameSite=Strict, path `/`, with no Domain. Preserve HTTPS and the external host when proxying. This package does not add forwarded-header middleware: use an HTTPS upstream with the correct host, or separately configure trusted proxy forwarding before deployment. Do not log auth or membership request/response bodies. Browser-auth tests must pass before release.

## Deliberately pending

- Final business-required form fields remain configurable via `Membership:RequiredSubmissionFields`; the UI shows the initial provisional minimum. Server validation is authoritative if configuration adds requirements.
- No monthly arrears calculation until fixed-monthly vs daily dues is settled. Non-member donations remain undecided; recorded payments currently attach to members.
- No split payment allocations, cash receipt numbering, reversals/refunds, or automatic payment confirmation. Repeated cash requests can create duplicates; verify history before retrying an uncertain cash response.
- No editing of submitted applications, token recovery UI, file/photo upload, or staff-assisted draft workflow. Paper entry is submitted only after the staff review screen and recorded applicant consent.
- No ecommerce or book selling. Advanced book reading is deferred while archive work continues.

Next stage: the public Bangla/English membership application with secure save/resume, review and acknowledgement.
