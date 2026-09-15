# The One management app

Vite + React + TypeScript + MUI, connected to the existing ASP.NET Core API.

## Start locally

1. Start the API with its HTTPS launch profile in Visual Studio (`https://localhost:7198`). Restart it after pulling backend changes.
2. Open a terminal in `apps/management` and run:

```powershell
npm ci
npm run setup:https
npm run dev
```

Open **https://localhost:5173**. The API and frontend must both remain running.
The certificate setup exports your ASP.NET development certificate into the ignored `.certs` directory. If the browser does not trust it, run `dotnet dev-certs https --trust` and reopen the browser.

Subsequent starts only need `npm run dev`. To use another API HTTPS port, set `$env:API_PROXY_TARGET='https://localhost:YOUR_PORT'` before starting Vite.

## Available screens

- Password login, required MFA, recovery-code login, first-time authenticator enrollment, registration, SMS login and password reset.
- Profile, mobile verification, password change, authenticator replacement, recovery-code regeneration and session revocation.
- User search, status and details; SuperAdmin role assignment.
- Roles and permissions, bilingual menu configuration, permission-filtered navigation and audit history.
- Responsive navigation and English/Bangla labels. Server messages and some explanatory text currently remain English.

Use your existing account. Admin and SuperAdmin require the authenticator flow. Menu visibility comes from the API; the API remains responsible for authorization. New menu routes must have corresponding frontend pages before they appear. Membership and research screens are deferred until their modules are implemented.

## Verification

```powershell
npm run build
npm run test:e2e
```

Browser tests use installed Microsoft Edge and mocked API responses; they do not send SMS or modify real users. They cover desktop/mobile sign-in, enrollment, route permissions, status confirmation and save-error handling. Backend browser-cookie tests run separately in the integration test project against the isolated PostgreSQL test database.

## Browser authentication and deployment

Access tokens live only in memory. Refresh tokens are set by `/api/v1/browser/auth` as Secure, HttpOnly, SameSite=Strict cookies and never appear in browser JSON. Cookie operations require HTTPS, an allowed Origin and the `X-TheOne-Client: web` header. Refresh operations are serialized within a tab and, where Web Locks is supported, across tabs. Existing Swagger/mobile token endpoints remain available.

Development uses Vite's `/api` proxy. Production must serve `dist` and proxy `/api` under one HTTPS origin, preserve the external host/scheme (or configure trusted proxy forwarding in the API), and fall back to `index.html` for SPA routes only. Never cache `/api` responses. Configure any explicit trusted origin through `Browser:AllowedOrigins`; do not use a wildcard. Do not deploy the Vite development server. Development proxy certificate validation is relaxed only for the local ASP.NET development certificate.

The app has its own package manifest and lockfile and can move to a separate repository. `src/api.ts`, `src/auth.tsx`, shared UI and theme form the reusable foundation; feature screens remain separate. Never place SMS keys, JWT signing keys, or other secrets in Vite environment variables.
