# Database Detailed Design

Expands `Database-Design.md`. PostgreSQL, EF Core Code First, UTF-8 throughout.

## Conventions

- PK: `Id` (uuid, `gen_random_uuid()`)
- Audit columns on every table: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- Soft delete via `IsDeleted` + `DeletedAt` (no hard deletes on Members, Books, ContentBlocks, Payments)
- FK columns named `<Entity>Id`; indexed by default

## Identity Domain

### Users
| Column | Type | Notes |
|---|---|---|
| Id | uuid | PK |
| Email | varchar(256) | unique index |
| PasswordHash | text | ASP.NET Core Identity |
| PhoneNumber | varchar(20) | nullable, used for MFS/SMS verification |
| EmailConfirmed / PhoneConfirmed | bool | |
| MfaEnabled | bool | mandatory for Admin roles |
| MfaSecretKey | text | encrypted at rest |
| LockoutEnd | timestamptz | nullable |

### Roles / UserRole (M:N) / Permissions
| Table | Key columns |
|---|---|
| Roles | Id, Name, IsSystemRole |
| UserRoles | UserId (FK), RoleId (FK) — composite PK |
| Menus | Id, ParentMenuId (self-FK, nullable), Route, Icon, Module, SortOrder |
| RoleMenuPermissions | RoleId (FK), MenuId (FK), CanView, CanCreate, CanEdit, CanDelete, CanApprove, CanExport |
| UserPermissionOverrides | UserId (FK), MenuId (FK), same permission flags, `Reason` (text, required — every override must be justified) |

Evaluation order at query time: `UserPermissionOverrides` → `RoleMenuPermissions` (highest-privilege role wins if user has multiple roles) → deny by default.

## Membership Domain

### Members
| Column | Type | Notes |
|---|---|---|
| Id | uuid | PK |
| UserId | uuid | FK -> Users, unique (1:1) |
| Status | enum | Pending, Active, Suspended, Cancelled |
| JoinedAt | timestamptz | nullable until activated |
| SkillsJson | jsonb | member skills/contribution tags |

### MembershipApplications
| Id | UserId (FK) | Status (enum: Submitted, UnderReview, Approved, Rejected) | ReviewedBy (FK Users, nullable) | ReviewedAt |

### PaymentTransactions
| Column | Type | Notes |
|---|---|---|
| Id | uuid | PK |
| MembershipApplicationId | uuid | FK |
| BkashTransactionId | varchar(64) | **unique index — idempotency key** |
| Amount | numeric(10,2) | |
| Status | enum | Initiated, Verified, Failed, Refunded, Expired |
| WebhookReceivedAt | timestamptz | nullable |
| RawWebhookPayload | jsonb | stored for reconciliation/audit |

Stored in a separate schema/table space from `Members` per the payment-separation decision in the master doc.

## Library Domain

### Books / Volumes / Chapters
Standard parent-child chain, each with `Id`, `Title`, parent FK, `SortOrder`.

### ContentBlocks
| Column | Type | Notes |
|---|---|---|
| Id | uuid | PK |
| ChapterId | uuid | FK |
| CurrentVersionId | uuid | FK -> ContentVersions (nullable until first approval) |
| ReviewStatus | enum | Draft, InReview, Approved, Rejected |

### ContentVersions
| Column | Type | Notes |
|---|---|---|
| Id | uuid | PK |
| ContentBlockId | uuid | FK |
| VersionNumber | int | 1..N, capped at 5 retained per block (oldest pruned on approval of a 6th) |
| UnicodeText | text | |
| SearchText | text | generated `tsvector` column, GIN indexed |
| OriginalSourceRef | text | pointer to object storage key of the original scan |
| QualityScore | numeric(5,2) | from automatic QC |
| ApprovedBy | uuid | FK -> Users, nullable |
| ApprovedAt | timestamptz | nullable |

### OriginalScans
| Id | ContentBlockId (FK) | StorageKey | Resolution | RetainedUntil (date, per retention policy) | ArchivedToColdStorage (bool) |

## CMS Domain

### Pages / PageTranslations
`Pages` holds structure/slug; `PageTranslations` holds `(PageId FK, LanguageCode, Title, BodyHtml)` — one row per language, per the CMS-vs-Library Unicode separation already established.

### Media
`Id, StorageKey, MimeType, SizeBytes, UploadedBy`

## Audit Domain

### AuditLogs
| Column | Type | Notes |
|---|---|---|
| Id | uuid | PK |
| ActorUserId | uuid | FK, nullable (system actions) |
| Action | varchar(100) | e.g. `ContentBlock.Approved`, `Role.PermissionChanged` |
| EntityType / EntityId | varchar / uuid | polymorphic reference |
| Metadata | jsonb | before/after diff where applicable |
| CreatedAt | timestamptz | indexed, partitioned by month for retention pruning |

Retention enforced by a scheduled Worker job per the 2yr/5yr policy — partitioned tables make bulk pruning cheap.

## Indexing Notes

- `ContentVersions.SearchText` — GIN index for PostgreSQL FTS (Phase 1); migration path to Elasticsearch keeps this column as the source-of-truth text.
- `PaymentTransactions.BkashTransactionId` — unique index doubles as the idempotency constraint; a duplicate webhook insert fails at the DB level, not just the application level.
- `AuditLogs.CreatedAt` — monthly partitions, indexed per partition.
