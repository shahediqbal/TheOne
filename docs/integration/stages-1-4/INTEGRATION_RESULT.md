# Stages 1–4 integration handover

## Branch and source

- Integration branch: `integration/stages-1-4`. No push, deployment or merge into main was performed.
- Local checkpoint preserving the original uncommitted work: `59a5f8c9b20b0d8162c59ea4bb9e8e5fd0533899`.
- The supplied `C:\Temp\TheOne-Update\Stages_1-4` directory did not exist. Source was taken from `Stages_1-4_Complete_Source (1).zip` in Downloads, extracted into a separate workspace, and compared file by file.
- Incoming status documents are retained under `docs/integration/stages-1-4`. They describe the incoming package, not the final reconciled state. This report takes precedence for integration results.

## Integrated features and resolved conflicts

- Membership backend, concurrency corrections, sequence-based references/member numbers, manual contributions, approval gate, staff queue/details and paper entry.
- Complete bilingual public membership form, resume/photo handling, shared field definitions, and typed persisted-submission validation.
- Blog and website CMS persistence, staff editors, publication/revision workflow and supplied public membership/blog routes. No full Stage 5 organization website was added.
- Reconciled DI, DbContext, permission catalog, exception handling, frontend helpers, routes, language labels and package scripts.
- Retained the existing browser-auth controller and its `__Secure-TheOneRefresh` cookie contract. Excluded the incoming duplicate `BrowserAuthController`, which would collide on routes. Incoming browser-cookie tests were adapted to the existing cookie name without dropping their security/rotation assertions.
- Preserved current authenticator state reporting, operator bootstrap/recovery, MFA recovery corrections and browser-request auditing. Existing authentication, Identity, Data Protection and JWT implementation files were not overwritten.
- Added the missing `/website` React route.
- Removed the incoming sidebar's hardcoded fallback links. Membership/blog/website entries are registered by an additive migration in the existing staff dynamic-menu tables; role assignment, enabled state and permissions control visibility. Public CMS `PublicMenu` records remain separate.
- New menus initially link to existing SuperAdmin roles. No existing staff roles were granted new permissions. SuperAdmins can assign new grants and menu visibility through the existing administration screens.
- Preserved existing appsettings, launch settings, User Secrets ID, connection strings, JWT configuration and local keys. No working-database account or data was accessed or changed.

## Exact verification results

| Check | Result |
|---|---|
| .NET solution, Release | Passed, 0 errors; 66 warnings: incoming XML documentation warnings and one xUnit analyzer warning |
| PostgreSQL backend regressions, final run | 169 passed, 0 failed, 0 skipped; duration 1m 9s |
| React management production build | Passed |
| Next.js public membership/blog production build | Passed |
| Management Vitest tests | 11 passed across 3 files |
| Public frontend Vitest tests | 8 passed across 2 files |
| Management Playwright, desktop + mobile | 20 passed, 0 failed; includes existing authentication and incoming membership checks |
| EF pending-model check | No pending changes |
| Existing snapshot comparison | All 24 original entity blocks retained unchanged before regeneration; only new module entities/sequences added |
| Upgrade SQL against seeded old schema | Passed; existing account/MFA fields, role grants, assignments, customized navigation and audit records preserved |
| Reapply reviewed upgrade SQL | Passed; no duplicate staff menus/links |

Backend tests used local PostgreSQL on `127.0.0.1:55439`, database `theone_stages14_test`; upgrade verification used `theone_stages14_upgrade_v2_test`. These are separate disposable databases, not the working database. Existing operator tests additionally create their own isolated databases under their built-in safety checks. The final TRX is local at `TestResults/stages14/stages14-final.trx` (ignored by Git).

Browser/DOM tests use mocked API responses; they do not prove a production browser-to-API deployment or live provider behavior. No real SMS, translation provider or semantic-search provider was enabled for these checks.

## Database implications

Existing historical migration files remain unchanged. The incoming four migrations follow the current source baseline `20260910100612_AddAdministrationAndAudit` without ID collisions:

1. `20260913090000_AddMembershipModule` — membership applications, members, contributions and number sequences.
2. `20260913120000_CompleteMembershipForm` — form columns and private photos.
3. `20260914100000_BlogPublishing` — blog publication/revision/slug tables.
4. `20260914150000_CompleteWebsiteCms` — CMS records/revisions/assets/public menus, publication events and disabled semantic-search storage.
5. `20260915061634_AddStageStaffNavigation` — three permission-filtered staff navigation entries; no automatic grants to existing ordinary staff roles.

The reviewed upgrade is `deployment/sql/stages-1-4/upgrade.sql`. It is idempotent and intended for the existing authentication/administration baseline. It does not install pgvector or enable search. The optional incoming pgvector SQL is not part of the upgrade and was not executed.

Two important reconciliation details:

- The incoming CompleteMembershipForm migration clears legacy draft PhotoUrl values. This file is absent from the original checkpoint, but its incoming history was retained intact. The reviewed SQL includes a guard that refuses a partially installed Stage 1 membership schema, so existing legacy membership data requires separate data-preserving reconciliation. In the tested authentication-only upgrade, that cleanup runs against an empty newly created table and deletes no pre-existing working data. Do not bypass the guard by running `database update` on an already partially installed membership database.
- Raw SQL in that migration lacks a terminal semicolon. EF executes it as a separate command successfully, but the generated idempotent PostgreSQL script failed until its statement terminator was corrected. The delivered script has been corrected and executed twice. Raw `dotnet ef migrations script` regeneration must retain this correction and the guard. An older baseline migration has the same script-generation issue; the old schema was therefore created through EF's normal migration runner for the upgrade test. No historical migration was edited.

The new navigation migration's automatic Down operation intentionally refuses to remove potentially customized menu data. Rollback needs an explicit reviewed procedure and backup; do not blindly downgrade the database.

## Remaining gates and limitations

- The working database's actual migration history and any manually created tables were deliberately not inspected through its credentials. Before approval, compare that history and rehearse the reviewed SQL on a restored copy. Never edit history entries to force a match.
- No migrations were applied to the working database. Do not use the new feature screens there until the reviewed schema upgrade is approved and applied.
- Production routing/TLS, proxy trust/rate limiting and real-browser end-to-end acceptance remain pending. For local Next.js-to-ASP.NET HTTPS, use the trusted development certificate and Node's system CA support (`NODE_USE_SYSTEM_CA=1` on the installed Node 24), or provide a dedicated trusted CA; do not disable TLS validation.
- Optional membership translation remains off when `Membership:TranslationEnabled` is unset/false. Enabling it requires provider URLs/credentials and separate validation. Existing configuration was not replaced with incoming provider examples.
- Semantic search remains disabled/incomplete; monthly-dues calculation is undecided; Stage 5, ecommerce/book sales and advanced reading are excluded.
- The 66 backend warnings and public Vitest config-loader warning are non-blocking quality follow-ups. No build/test failures remain in the verified integration.

## Next commands: review only

From PowerShell:

```powershell
cd C:\Users\Shahed\Documents\GitHub\TheOne
git switch integration/stages-1-4
git log -2 --oneline
git diff 59a5f8c9b20b0d8162c59ea4bb9e8e5fd0533899 HEAD --stat
code docs/integration/stages-1-4/INTEGRATION_RESULT.md deployment/sql/stages-1-4/upgrade.sql
```

After review, separately authorize the working-database upgrade. Back up and rehearse it on a restored copy first. Publication and pushing this branch require separate approval.
