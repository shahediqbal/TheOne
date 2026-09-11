# User Management

This increment provides the User Management API and Swagger operations. The application UI is not built yet; its planned location is **Administration > Users**. Membership registration remains a separate future module.

## Operations

All operations require a current Admin or SuperAdmin session authenticated with password plus authenticator/recovery code.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/v1/admin/users` | Paginated account list |
| GET | `/api/v1/admin/users/{userId}` | Account details and role names |
| PATCH | `/api/v1/admin/users/{userId}/status` | Activate/deactivate an eligible account |

List query parameters: `page` (default 1), `pageSize` (default 20, maximum 100), `search` (name/email/phone substring), `isActive`, and `role` (exact name, case insensitive). Search treats wildcard characters literally. Results are ordered by creation time descending with user ID as a stable tie-breaker. Total count and page results can reflect concurrent account changes.

Status request:

```json
{
  "isActive": false
}
```

Set `isActive` to `true` to reactivate. Omitting the value is rejected. Repeating the current status succeeds without changing credentials or sessions again. Missing accounts return 404; invalid requests return 400; prohibited status changes return 403.

## Access rules

- Admin and SuperAdmin can list and inspect accounts.
- Admin can change the status of ordinary accounts only.
- SuperAdmin can also change Admin account status.
- Neither role can change its own account status.
- SuperAdmin accounts cannot be activated/deactivated through this module. A separately reviewed privileged-account lifecycle will be needed if that capability is required.
- No account deletion, password retrieval, authenticator-key access, or automatic role changes are provided.

The status operation rechecks the actor's current role, MFA session, lockout and security stamp while locking actor and target accounts in a consistent order. It records actor ID, target ID and new status in the application log. This is not the future durable audit-history module.

## Status and sessions

A status change rotates the security stamp, revokes refresh sessions and consumes pending MFA challenges. Security-stamp checks invalidate outstanding SMS challenges. Inactive accounts are rejected by access-token validation and login.

The nullable `AspNetUsers.StatusChangedAtUtc` field prevents pre-change sessions from being accepted after reactivation. Reactivated users must sign in again. Reactivation does not clear password lockout or disable MFA. Existing users who have never had an administrative status change retain their previous session behavior.

The additive `AddUserStatusSessionBoundary` migration must accompany deployment. No account status changes are made by this migration.

## Validation

Integration tests cover anonymous/member denial, account search and filters, pagination and bounds, safe response fields, missing accounts, omitted status, role boundaries, self/SuperAdmin protection, revoked administrator credentials, deactivate/reactivate behavior, and concurrent status requests. Tests use an isolated PostgreSQL database and do not send SMS.

## Roles and audit update

See [Administration review](administration-review.md) for live permission enforcement, delegated managers and durable audit records. These replace the initial role-only authorization and application-log-only descriptions above.
