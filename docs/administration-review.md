# Administration: review modules 1–3

This release implements the APIs for User Management, Roles & Permissions with Dynamic Menus, and Security Audit History. Review them in Swagger after restarting the API. Application screens are still a later UI increment; seeded menu routes describe their planned locations.

Sign in as SuperAdmin with password and authenticator, and authorize the returned access token. Do not paste passwords, tokens, authenticator secrets or recovery codes into review notes.

## 1. User Management

- GET `/api/v1/admin/users`: page, pageSize, search, isActive and role filters.
- GET `/api/v1/admin/users/{userId}`: safe account details.
- PATCH `/api/v1/admin/users/{userId}/status`: `{ "isActive": false }` or `true`.

Permissions now govern these endpoints. `users.read` grants listing/details; `users.manage` grants status changes. Every administrative permission requires an MFA-authenticated session. The Admin role starts with both grants; SuperAdmin always has all permissions.

Delegated managers can change ordinary accounts only. SuperAdmin can also manage Admin/delegated accounts. Self-status changes and SuperAdmin status changes remain prohibited. Deactivation blocks login and current access; reactivation requires a new login and never restores old sessions.

## 2. Roles & Permissions

Role/menu configuration remains SuperAdmin-only and cannot be delegated by adding a permission string.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/v1/admin/permissions` | Available permission catalog |
| GET | `/api/v1/admin/roles` | Roles and grants |
| POST | `/api/v1/admin/roles` | Create custom role |
| PUT | `/api/v1/admin/roles/{id}` | Update name and replace complete grant set |
| DELETE | `/api/v1/admin/roles/{id}` | Delete unused custom role |
| PUT | `/api/v1/admin/users/{userId}/roles/{roleId}` | Assign role |
| DELETE | `/api/v1/admin/users/{userId}/roles/{roleId}` | Remove eligible role assignment |
| GET | `/api/v1/me/permissions` | Current session's usable permissions |

Example role:

```json
{
  "name": "User Reviewer",
  "permissions": ["users.read"]
}
```

The initial catalog is `users.read`, `users.manage`, and `audit.read`. These are server-defined actions; adding a new business module requires adding its server permission and checks. Role grants are queried live, so additions/removals apply on subsequent requests without waiting for JWT expiry.

System rules:

- Member and SuperAdmin definitions are protected. Admin permissions can be changed, but Admin cannot be renamed or deleted.
- Custom names cannot impersonate Member, Admin or SuperAdmin (including the legacy spaced alias).
- Assigned roles cannot be renamed. Roles with user or menu assignments cannot be deleted.
- Users cannot edit their own role assignments. Member/SuperAdmin assignments cannot be removed through this increment.
- Assigning any administrative role requires an active, contact-verified, authenticator-enrolled target. All API permission checks independently require MFA, even if a role later gains administrative permissions.
- Role membership changes rotate the target security stamp and revoke refresh sessions. Sign in again afterward.
- The earlier POST `/api/v1/admin/users/{userId}/roles` endpoint remains compatible for Admin/SuperAdmin promotion and now writes audit history.

## Dynamic Menus

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/v1/admin/menus` | All menu definitions and assignments |
| POST | `/api/v1/admin/menus` | Create menu |
| PUT | `/api/v1/admin/menus/{id}` | Replace menu definition and role assignments |
| DELETE | `/api/v1/admin/menus/{id}` | Delete leaf menu |
| GET | `/api/v1/me/menus` | Current user's permitted navigation tree |

Example (replace the role identifier):

```json
{
  "labelEn": "Users",
  "labelBn": "ব্যবহারকারী",
  "icon": "users",
  "route": "/administration/users",
  "parentId": null,
  "sortOrder": 10,
  "enabled": true,
  "requiredPermission": "users.read",
  "roleIds": ["<role-guid>"]
}
```

Both role visibility and any required permission must allow display. Parent menus must also be enabled, assigned to the user and permitted; otherwise their whole subtree is hidden. Assign container menus as well as their children. Containers without visible children are omitted. Role assignment alone never grants API access.

Menus support English/Bangla labels, optional icon names, internal paths, ordering, up to five hierarchy levels, and a maximum of 500 definitions. Cycles, unknown roles/parents, external routes and deletion of parents with children are rejected. Render labels as text in future clients, never HTML.

The migration seeds Administration > Users / Roles and Permissions / Menu Management / Audit History. SuperAdmin is assigned all seeded entries. Admin is assigned Administration, Users and Audit History; permission checks still apply. No user-facing screen is created by a navigation record.

## 3. Security Audit History

GET `/api/v1/admin/audit` requires `audit.read` plus MFA.

Filters: `page` (default 1), `pageSize` (default 20, max 100), `actorId`, exact `action`, `from`, `to` (ISO-8601 timestamps). Records show timestamp, actor identifier, action, target and safe JSON metadata.

Events include:

- `Role.Created`, `Role.Updated`, `Role.Deleted`
- `User.RoleAssigned`, `User.RoleRemoved`, `User.StatusChanged`
- `Menu.Created`, `Menu.Updated`, `Menu.Deleted`
- `Authentication.Login`, `Session.Refreshed`
- `Authentication.Request`, `Access.Denied`

Administrative changes and successful token issuance store their event in the same database transaction. Failed/denied requests and completed authentication requests have a separate outcome record with HTTP status and route. Anonymous attempts do not have an actor ID; request bodies are intentionally not collected. Generic authentication-request outcomes include registration, MFA, recovery and logout POST routes; they do not infer success from HTTP 200 beyond that endpoint's own semantics (for example generic forgot-password acknowledgements).

Passwords, codes, secret keys, access/refresh tokens and complete HTTP payloads are excluded. Rejected changes do not generate a successful change event. No-op status/role assignments do not create duplicate change events.

The database rejects UPDATE, DELETE and TRUNCATE against audit history. No API to edit or delete events exists. A database owner can still alter database protections, so this is application-level append-only history, not protection against a compromised database administrator. Separate outcome-log failures are reported in application error logs; they cannot undo an already-committed response. Retention/archival and external monitoring remain deployment work.

## Deployment and verification

Migration: `20260910100612_AddAdministrationAndAudit`. It adds navigation and audit tables, seeds Admin permission claims and navigation, and installs audit immutability protection. It does not promote accounts or change their passwords/MFA settings. Apply code and schema together. Rollback drops menu and audit data and retains the seeded role claims; do not roll back a live audit store casually.

Automated checks cover the existing authentication/User Management flows plus delegated permissions, permission revocation, MFA enforcement, protected/assigned roles, concurrent duplicate creation, menu hierarchy/visibility, unsafe inputs, audit filtering, credential exclusion and database immutability. Tests use an isolated PostgreSQL database without real SMS.

Suggested review order: User Management list/details; custom role creation; role/menu assignment to an enrolled test account; permission removal and navigation refresh; then the matching audit entries. Use a separate test account for status changes.
