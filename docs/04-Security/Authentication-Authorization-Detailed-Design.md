# Authentication & Authorization — Detailed Design

Expands `Authentication-Authorization.md` and `Security-Checklist.md`.

## Authentication Flow

1. `POST /api/v1/auth/login` — email + password.
2. Identity validates credentials; on success, checks `MfaEnabled`.
3. If MFA required: return `mfaRequired: true` + short-lived `mfaChallengeToken` (5 min expiry); client submits TOTP code to `POST /api/v1/auth/mfa/verify`.
4. On full success: issue **JWT access token** (15 min expiry) + **refresh token** (7 day expiry, single-use, rotated on refresh).
5. Failed attempts increment a per-account counter; account locks for 15 minutes after 5 consecutive failures (Identity `LockoutEnd`).

### JWT Claims

| Claim | Purpose |
|---|---|
| `sub` | UserId |
| `email` | |
| `roles` | array of role names (for coarse checks; fine-grained checks still hit the permission tables) |
| `mfa` | bool, whether this session completed MFA |
| `jti` | token id, checked against a revocation list on logout/security events |
| `exp` / `iat` | standard |

### Refresh Token Rotation

- Each refresh call invalidates the presented token and issues a new one — reuse of an already-rotated token revokes the entire token family (signals possible theft) and forces re-login.
- Refresh tokens stored hashed (never plaintext) in `RefreshTokens` table with `UserId`, `TokenHash`, `ExpiresAt`, `RevokedAt`, `ReplacedByTokenId`.

## MFA (TOTP)

- Standard TOTP (RFC 6238), 30-second window, compatible with Google/Microsoft Authenticator.
- Setup: server generates secret, shows QR code, user confirms with one valid code before `MfaEnabled` flips to true.
- **10 recovery codes** generated at setup, shown once, stored hashed. Each code is single-use.
- Recovery path (lost device + no codes left): Admin-initiated override, requires a second Admin's approval, writes an `AuditLog` entry, and enforces a **24-hour delay** before the account can log in MFA-free — closes the window for social-engineering a fast override.
- MFA is enforced at the API middleware level for any endpoint tagged `[RequiresElevatedAuth]` (all Admin-role endpoints), not just at login.

## Authorization — Permission Evaluation

Pseudocode, executed per request after JWT validation:

```
function CanAccess(user, menu, action):
    override = UserPermissionOverrides.find(user.Id, menu.Id)
    if override exists:
        return override[action]   # explicit allow or deny always wins

    roles = user.Roles
    rolePermissions = RoleMenuPermissions.find(roles, menu.Id)
    if any(rolePermissions[action] == true):
        return true

    return false   # default deny
```

- **Data Permission** (row-level) is applied *after* the above passes — e.g., a Member with `View` on Membership Applications only sees rows where `MembershipApplication.UserId == currentUser.Id`. Implemented as an EF Core global query filter, not left to each handler to remember.
- Permission checks are enforced via an ASP.NET Core authorization handler/attribute, never left to controller-body `if` statements — keeps the check consistent and auditable across all ~15 modules in the Role & Permission Matrix.

## Session & Token Revocation

- Logout revokes the current refresh token immediately; access tokens are short-lived enough (15 min) that no separate access-token blacklist is needed in the common case.
- Security-sensitive events (password change, MFA reset, role change) revoke **all** active refresh tokens for that user, forcing re-authentication everywhere.

## Rate Limiting Tie-In

Per `API-Standards.md` / security checklist: `/auth/login` and `/auth/mfa/verify` sit in the strict "Authentication APIs" tier (v5 §12) — separate, tighter limit than general Admin/Public tiers, since these are the brute-force target surface.

## Threat Notes

- Password hashing: ASP.NET Core Identity default (PBKDF2-based); no custom hashing.
- MFA secret and recovery codes: encrypted/hashed at rest, never logged, never returned in any API response after initial setup.
- All auth events (`Login`, `FailedLogin`, `PasswordChanged`, `MfaEvent`, `RoleChanged`, `PermissionChanged`) write to `AuditLogs` per the audit policy already defined.
