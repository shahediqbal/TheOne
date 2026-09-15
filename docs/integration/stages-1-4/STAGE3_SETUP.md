# Stage 3 — Public membership and complete reference form

This is a **cumulative source ZIP containing Stages 1–3**. Keep your existing checkout intact and extract this package into a new folder. You may wait for the remaining website stages before applying it. Do not overlay older ZIPs onto this one. Merge any newer local changes deliberately.

## What is included

- Next.js + TypeScript + Tailwind public membership at `/bn/membership` and `/en/membership`.
- Start a draft, download the private recovery details, resume with reference/phone/token, save sections, review, accept three statements, submit and download acknowledgement.
- All required questions from the supplied Google Form, with the intentional differences listed below.
- Staff paper entry uses the same field definitions and includes photo, signature, email and separate recorded consents. Staff detail shows the new data, consent audit and protected photo on request.
- Backend DTOs, persistence, full submission validation, private photo endpoints and the `CompleteMembershipForm` migration. Earlier migration/history is retained.
- Stage 1 state transitions, atomic member creation, number sequences and fee gate; Stage 2 staff queue, review, manual contribution records and browser authentication.
- A correction found during this pass: the shared transaction helper previously rejected the 45-character operator lock key. It now permits the exact `operator:<UUID>` format while retaining the public-reference length bound.

**Frontend builds and ten simulated-DOM interaction tests pass. The backend and migrations have not been compiled or executed here.** See `VERIFICATION.md` before installing.

## Differences from Google Forms

| Source item | This implementation |
|---|---|
| Google account email | Applicant enters email; no Google sign-in or OTP |
| Full name | Bangla and English full names required; other English fields optional |
| All starred personal/purpose/skills/about/date fields | Required at final submission; drafts can be incomplete |
| Areas of help / Other | Multiple selections; Other requires a description |
| Financial contribution intention | Yes/No/Later; this is not a payment or fee receipt |
| Code of conduct, truth declaration, oath | Three separate recorded acceptances, version and server timestamp |
| Applicant signature | Typed full name; no handwriting capture or signature verification |
| Formal membership date | Staff approval timestamp; applicants cannot self-approve or backdate membership |
| Applicant date | Separate applicant-entered date, no future dates |
| Photo, any image up to 10 MB | JPEG/PNG up to 10 MB; max 6000 pixels per side and 25 MP |

Photos are stored in PostgreSQL `bytea` and never under a public static URL. `PhotoUrl` in the application response is now only an `uploaded` marker. A user-supplied `PhotoUrl` is ignored. Images undergo signature/header/dimension checks, not full decoding or metadata removal; original EXIF metadata is retained inside private storage. The website previews the newly selected photo; staff can fetch it after authorization.

Recovery details remain in component memory and in a file only when the applicant explicitly downloads it. They are not put into URLs, cookies or localStorage. The secret is not sent by SMS and cannot be reissued through this interface. If the start response is lost, an unsubmitted draft may remain; applicants can start again. If a submit response is interrupted, reopen the saved application to check its state before trying again.

## Installation sequence

1. Use .NET 10, PostgreSQL, and a Node version supported by the checked-in frontend packages. Builds here used Node 22. Install dependencies with `npm ci`; each app includes its own lockfile. Keep the sibling `packages/membership-form` directory when copying an app.
2. Follow `STAGE1_SETUP.md` for development secrets and prerequisites, using this cumulative backend. Follow `STAGE2_SETUP.md` for staff accounts, MFA, permissions and the staff HTTPS frontend. Stage 3 supersedes its provisional form requirements and its old operator request shape.
3. From `TheOne/`, set `THEONE_AUTH_TEST_CONNECTION` to a disposable PostgreSQL database ending in `_test` or `_tests`, then run `./scripts/Verify-Membership.ps1`. This builds the API and executes all membership/browser-auth regression tests.
4. Review and test both migrations against a backup/disposable copy before applying them to your development database. From `TheOne/`:

```powershell
dotnet tool restore
dotnet ef migrations script --idempotent --project src/TheOne.Persistence --startup-project src/TheOne --output membership-migrations.sql
dotnet ef database update --project src/TheOne.Persistence --startup-project src/TheOne
dotnet run --project src/TheOne --launch-profile https
```

5. Start the public frontend from `TheOne/apps/website/`:

```powershell
npm ci
# Trust your development API certificate in Node without disabling TLS verification:
dotnet dev-certs https --format PEM --export-path dev-api.pem --no-password
$env:NODE_EXTRA_CA_CERTS = (Resolve-Path ./dev-api.pem).Path
$env:MEMBERSHIP_API_ORIGIN = "https://localhost:7198"
npm run build
npm test
npm run dev
```

Open `http://localhost:3000/bn/membership` or `/en/membership`. The localhost HTTP address is for development; deploy behind HTTPS. Keep the exported certificate/key local and excluded from commits. Start the staff app separately as described in `STAGE2_SETUP.md`.

## Deployment notes

The public pages are pre-rendered, but `/api/v1/membership/*` uses a Node route handler. This app needs a normal Next.js Node deployment; it is not a static export. Set `MEMBERSHIP_API_ORIGIN` server-side. The proxy forwards only public membership paths and content-type/accept; staff cookies and Authorization headers are not forwarded. API responses are not cached. Keep request bodies and responses out of proxy/application logs.

Configure production rate limiting with the deployment: the backend currently uses the connecting IP (30 membership requests/minute). With the built-in Next proxy, that is the shared Node server IP. Until a trusted reverse proxy/client-IP configuration is implemented and tested, all applicants share that quota. Do not trust arbitrary client-supplied forwarded-IP headers or increase limits blindly. Configure upload limits at the edge to match the API (11 MB multipart; 16 MB staff JSON). Do not expose private photo data through static hosting or public caches.

The package is ready for local integration review, not a claim of a tested production deployment. Browser/mobile visual checks, real API integration, migrations and deployment proxy behavior remain local release gates.

## Migration behavior

`20260913120000_CompleteMembershipForm` adds fields and the private-photo table. Existing submitted/approved applications are preserved; consent is not fabricated. New consent flags on old records default to false, with no consent version/timestamp. Existing draft `PhotoUrl` values are cleared because the older API accepted arbitrary URLs; drafts must upload a real photo and complete the new fields before submission. Downgrading this migration removes the new data/photos and does not restore cleared draft URLs; use a backup if rollback is required.

## Still scheduled separately

Organization pages/CMS and administrator-approved blog publishing; founder-book archive and advanced reading tools; Flutter reader. There is no ecommerce or book selling. Monthly arrears accounting waits for the fixed-monthly versus daily rule; non-member donations remain a separate decision. Existing manual member-linked payment records remain available.
