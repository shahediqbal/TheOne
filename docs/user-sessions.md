# Current-user profile and session management

All routes below require a valid Bearer access token and use the existing response wrapper.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | /api/v1/auth/me | Read the signed-in user's profile and current database roles |
| GET | /api/v1/auth/sessions?pageNumber=1&pageSize=20 | List active refresh sessions |
| DELETE | /api/v1/auth/sessions/{sessionId} | Revoke one owned session |
| POST | /api/v1/auth/logout-all | Revoke all existing refresh sessions, including the caller |

## Swagger test

1. Restart the API and log in. Use its access token in Authorize.
2. Call `GET /api/v1/auth/me` to view your profile.
3. Call `GET /api/v1/auth/sessions`. The current login has `isCurrent: true`.
4. Log in a second time to create another session. Keep the first access token authorized.
5. Call sessions again; it should list both logins.
6. Copy the second session ID into `DELETE /api/v1/auth/sessions/{sessionId}`.
7. Verify that its refresh token is rejected and the first session can still refresh.
8. Call `POST /api/v1/auth/logout-all`. All existing refresh sessions should disappear;
   their refresh tokens should be rejected. A new password login can create a new session.

A session represents a login, not an identified physical device. Device names and IP metadata are
not inferred or exposed in this increment.

## Behavior and security

The profile includes user ID, full name, email, mobile, verification flags, creation time,
and current role assignments. It never includes password hashes, security stamps, or tokens.

Sessions expose a stable `sessionId`, original creation time, latest token issuance time
(`lastRefreshedAtUtc`), refresh-token expiry, and `isCurrent`. Initial issuance is also the initial
last-refreshed time. Session IDs are retained through rotation and included in new JWTs as `sid`.
Pagination defaults to 20 rows, caps at 100, and orders by session creation time descending.

Only unrevoked, unexpired refresh sessions are listed. Counts and rows are read under the same
account transaction lock used by login, rotation, password changes, and revocation.
A revoke request racing with refresh therefore revokes the replacement too.

All queries and updates derive the account ID from the validated access token, never a request
body or query parameter. Revoking an unknown, already-revoked, or another account's session returns
the same success response without disclosing ownership. It cannot alter another account's sessions.
Inactive, locked, or deleted accounts cannot use these endpoints.

Revocation preserves the existing access-token behavior: issued JWTs remain valid until expiry
(plus the configured clock skew). These endpoints revoke refresh sessions, not access JWTs.
Consequently, a still-valid access JWT may still read a profile or manage sessions after logout-all.
Immediate access-token revocation would require an additional server-side check or denylist.

Roles shown by /me are current database roles; authorization claims in an existing JWT remain its
issuance-time snapshot until a new token is obtained.

## Migration

`AddRefreshSessionIdentity` adds `SessionId` and `SessionCreatedAtUtc` to refresh tokens and an
index on account/session. Existing token records are preserved and initialized from their record
IDs and creation times. Each pre-upgrade active token becomes its own session; the original login
time cannot be reconstructed reliably from every legacy chain.

Pre-upgrade JWTs lack `sid`; they can still call these endpoints, but no row is marked current until
the user logs in or refreshes again. Stop/restart older API processes during the schema/code upgrade
so they cannot keep creating tokens without the new session fields.

For another environment, apply the migration from Visual Studio Package Manager Console:

```powershell
Update-Database -Project TheOne.Persistence -StartupProject TheOne.API
```

## Verification

The integration suite exercises profile fields, current-session identification, session identity
across rotation, selective and all-session revocation, cross-account isolation, pagination, expiry,
authorization, inactive accounts, and concurrent refresh/revoke. The existing authentication and
OTP tests remain included. Tests use the dedicated database selected by `THEONE_AUTH_TEST_CONNECTION`
and never send real SMS.
## Authenticator policy update

See [authenticator login](authenticator-login.md) for mandatory administrator MFA, optional SMS login for ordinary accounts, and immediate session validation for enrolled accounts. These rules supersede earlier login and access-token descriptions for MFA/administrator accounts.
