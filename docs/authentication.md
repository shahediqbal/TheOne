# Authentication foundation

Implemented routes under `/api/v1/auth`:

| Method | Route | Purpose |
| --- | --- | --- |
| POST | register | Create an Identity account with the Member role |
| POST | login | Authenticate email/mobile and password; return JWT and refresh token |
| POST | refresh-token | Consume and replace a refresh token |
| POST | logout | Revoke the supplied session and its replacements |
| POST | change-password | Require JWT and current password; revoke all refresh sessions |

Responses use `ApiResponse<T>`. Validation returns 400, failed authentication returns 401,
and the per-IP authentication limit returns 429 (20 requests/minute per process).
Swagger includes XML endpoint descriptions and a Bearer authorization input.

## Architecture

- Domain retains the existing refresh-token entity.
- Application owns DTOs, validators, authentication use cases, and the persistence/token interfaces.
- Persistence implements Identity operations and PostgreSQL transactions. It references Application and Domain only.
- Infrastructure implements JWT generation and validation. JWT registration was moved here from Persistence.
- API composes the layers, translates failures, documents endpoints, and applies rate limits.

The authentication foundation uses the Identity and refresh-token migrations. The later
`AddOtpChallenges` migration adds mobile verification and recovery; see [SMS OTP setup](sms-otp.md).
Application and Domain have no EF Core or ASP.NET Identity dependency.

## Configuration and usage

Keep `ConnectionStrings:DefaultConnection` and `Jwt:SecretKey` in API user secrets or deployment
secret configuration. The signing secret requires at least 32 UTF-8 bytes. Access-token lifetime
must be 1–60 minutes and refresh-token lifetime 1–90 days. Existing defaults are 15 minutes / 30 days.

Registration accepts `fullName`, `email`, `mobileNumber`, `password`, and `confirmPassword`.
Mobile numbers must contain 7–15 digits with an optional leading +; use the same representation
when signing in. This increment does not normalize national numbers to E.164.
Login accepts `userNameOrMobile` and `password`. Registration does not automatically log in.
Refresh and logout accept `refreshToken`. Change password accepts `currentPassword`,
`newPassword`, and `confirmPassword`, with `Authorization: Bearer <accessToken>`.

Only refresh-token SHA-256 hashes are persisted. Rotation and user-session operations use
PostgreSQL row locks inside transactions. Reuse of a rotated token revokes all refresh sessions
for that account; clients must serialize refresh requests and must not retry a consumed token.
Logout follows replacement links to handle overlap with token rotation.
Registration uses a PostgreSQL transaction advisory lock for default-role creation and mobile
uniqueness through this adapter. Future account-import or profile-edit paths must enforce the same
uniqueness rules; the current schema has no unique mobile constraint.

Password failures count toward the configured five-attempt / fifteen-minute Identity lockout.
Inactive and locked accounts cannot obtain tokens. Two-factor-enabled accounts are rejected until
a second-factor flow is implemented. Password changes and logout revoke refresh tokens;
already-issued JWTs remain valid until expiry (plus the 30-second validation clock skew).
This is not immediate access-token revocation.

Authentication events log user identifiers, never passwords or raw tokens. These operational
logs are not yet a durable security-audit store. Rate limiting is per process; a multi-instance
deployment needs shared/edge limiting. Behind a proxy, configure trusted forwarded headers before
using client IPs for limiting.

## Verification

Integration tests use the real API, Identity, JWT validation, existing migrations, and PostgreSQL.
Set `THEONE_AUTH_TEST_CONNECTION` to a dedicated disposable PostgreSQL database, then run:

```powershell
dotnet test src/TheOne.IntegrationTests/TheOne.IntegrationTests.csproj
```

Tests apply migrations and create test accounts in that database. Never point them at development
or production data. No database connection or secrets are embedded in the test source.
Tests cover registration/login/logout, hash storage, rotation, replay rejection, simultaneous
refreshes, JWT protection, password-change revocation, lockout, validation, duplicate mobile
registration, expired refresh tokens, and inactive accounts.

Mobile verification and SMS password recovery are implemented in [SMS OTP setup](sms-otp.md).
Current-user profile and refresh-session management are implemented in [session setup](user-sessions.md).
Next increments: email verification, OTP login, durable security audit, and permissions/menu authorization.
Registration retains the existing active-account behavior; mobile verification is required for
SMS password recovery, but is not required for password login.

## Authenticator policy update

See [authenticator login](authenticator-login.md) for mandatory administrator MFA, optional SMS login for ordinary accounts, and immediate session validation for enrolled accounts. These rules supersede earlier login and access-token descriptions for MFA/administrator accounts.
