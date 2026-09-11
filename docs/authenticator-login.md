# Authenticator and optional SMS login

## Policy

- Ordinary users can sign in with a password or an SMS code sent to their verified mobile.
- Any user who enrolls an authenticator must complete password plus authenticator (or a single-use recovery code). SMS login and SMS password recovery cannot bypass enrolled MFA.
- Admin and SuperAdmin always require password plus authenticator/recovery code. Accounts assigned these roles without completed enrollment cannot sign in.
- Registration never grants administrator roles. Promotion requires a current SuperAdmin MFA session and an active, contact-verified, authenticator-enrolled target.

## Enrollment

All routes below start with `/api/v1/auth`. Use HTTPS. Authenticator setup material and recovery codes are credentials: do not log or share them.

1. Sign in normally and authorize the access token. Verify the account's mobile (or email).
2. POST `authenticator/begin-enrollment` with `{ "password": "..." }`.
3. POST `authenticator/setup` with the returned `{ "challengeId": "..." }`. It returns a shared key, `otpauth://` URI, and a PNG QR data URI for the client to display locally.
4. Add the account to an authenticator app. POST `authenticator/confirm-setup` with `{ "challengeId": "...", "code": "123456" }`.
5. Store the ten returned recovery codes privately. They are displayed once. Enrollment revokes existing sessions; sign in again.

Challenges expire after five minutes. Beginning a new challenge invalidates the previous one. A recently accepted authenticator code cannot be reused; wait for a new code when necessary.

## Login

POST `login` with the existing username/mobile and password request. For an enrolled account the response contains `requiresTwoFactor: true` and a `challengeId`, with no access or refresh tokens.

Complete with either:

- POST `login-with-authenticator`: `{ "challengeId": "...", "code": "123456" }`
- POST `login-with-recovery-code`: `{ "challengeId": "...", "recoveryCode": "..." }`

Successful completion returns the existing token response. The client must branch on `requiresTwoFactor` before trying to use tokens. Refresh preserves the verified login strength. Failed second factors contribute to account lockout; supplying the correct password does not clear those failures until login completes.

Ordinary users without MFA can POST `request-login-otp` with `{ "mobileNumber": "017..." }`, then POST `login-with-otp` with `{ "challengeId": "...", "code": "123456" }`. The mobile must already be verified. Requests return a generic response for ineligible/unknown accounts. Existing SMS delivery settings still apply; automated tests never send real SMS.

## Authenticator recovery

Using an authorized MFA session, POST `authenticator/reset` or `authenticator/regenerate-recovery-codes` with the current `password` and exactly one of `code` or `recoveryCode`.

Reset rotates the authenticator key, clears recovery codes, revokes sessions, and returns an enrollment challenge. Complete setup and confirmation immediately using that challenge. Administrators remain blocked until re-enrollment completes. An expired/lost reset challenge on an administrator account requires supervised operator recovery; there is no password-only or SMS fallback.

Regenerating recovery codes replaces the whole set and revokes sessions. Save the new codes and sign in again. Loss of both authenticator and recovery codes requires a separately reviewed operator recovery procedure; this increment does not expose an MFA bypass endpoint.

## Administrator provisioning

POST `/api/v1/admin/users/{userId}/roles` with `{ "role": "Admin" }` or `{ "role": "SuperAdmin" }` from an authorized SuperAdmin MFA session. Target sessions are revoked and the target must sign in again.

The migration creates role definitions only. The first SuperAdmin needs supervised provisioning of an explicitly selected, verified and authenticator-enrolled account, with security-stamp rotation and session revocation. No default privileged account, default password, or public bootstrap route is created.

## Storage and operations

Authenticator keys use ASP.NET Core Data Protection encryption. Recovery codes use SHA-256 hashes. Preserve and protect the Data Protection key ring across restarts/deployments and share it appropriately across API instances. Losing its keys prevents decryption of enrolled authenticators.

MFA and administrator requests recheck current roles, security stamp and active session state. Revocation therefore invalidates their access tokens immediately on subsequent requests. Ordinary users without MFA retain existing short-lived access-token behavior; revoked refresh tokens cannot renew them.

Migration `20260909051151_AddAuthenticatorLoginPolicy` adds MFA challenges, accepted-code replay records and refresh-session security metadata. Its rollback removes the added schema but deliberately retains role definitions. Existing administrator sessions lacking MFA metadata are rejected and must not be relied on after rollout.

The integration suite covers enrollment, encrypted/hashed storage, TOTP and recovery-code replay, concurrent completion, lockout, challenge expiry/purpose, protected reset, recovery-code regeneration, role promotion, and optional SMS login. It uses an isolated PostgreSQL database and a fake SMS sender.

Administrative permissions also require an MFA-authenticated session for custom roles. See [Administration review](administration-review.md).
