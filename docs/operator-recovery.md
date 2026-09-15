# Operator bootstrap and administrator recovery

These commands close the first-SuperAdmin and complete-administrator-lockout gaps. They run locally on a trusted operations host. They do not expose an HTTP endpoint, start the API, create default passwords, or change accounts automatically during startup.

Project: `src/TheOne.Operations/TheOne.Operations.csproj`. Run from the repository root. Existing application migrations, including the append-only audit store, must already be applied. This increment requires no new migration.

## Trust and approval

The CLI has the authority of its operating-system identity, database credentials and Data Protection key access. Restrict those using infrastructure access controls. Do not distribute production credentials or this operational access to ordinary application administrators.

Before applying either operation:

1. Open an approval/incident ticket identifying environment, database host/port/name, target user ID and expected email.
2. Verify the account owner's identity through an independently established channel. Possession of an email message or claimed account name alone is insufficient. Follow your organization's identity-verification policy.
3. Obtain a second person's approval and record it in the ticket. In a single-person deployment, arrange a designated trusted approver before an incident.
4. Record the executing operator and approver identifiers. The CLI requires distinct names and an identity-verification attestation, but these are **recorded attestations, not authenticated second-person approval**. Enforce approvals through host access controls and your external ticket process.
5. Have the account owner ready to set the new password and complete authenticator enrollment. Stop terminal transcripts, screen recording, session output capture and clipboard sync before recovery.
6. Confirm backups and correct Data Protection configuration. Never remove administrator roles or set contact-verification flags manually as a shortcut.

The operator must supply both a user ID and matching expected email. Commands refuse inactive or contact-unverified accounts; recovery does not reactivate disabled users or change contact information. Such cases require separate investigation and approved account administration.

## Connection and Data Protection

Use `--secrets-id` to read `ConnectionStrings:DefaultConnection` from existing .NET User Secrets, or supply the connection through protected process environment `THEONE_OPS_CONNECTION` (which takes precedence). Never put a connection string/password on the command line or commit it.

Explicitly confirm `--expect-host`, `--expect-port`, and `--expect-database`. The tool compares them with the parsed connection before querying accounts. This is a destination check, not database-server identity verification; use normal TLS/network controls in production.

Use the API's Data Protection application name (`TheOne` by default). On the same development Windows account, the default key ring matches the API's existing default. For another host/service account, provide `--key-ring` pointing to the protected shared key directory and `--application-name` matching the API. If the API uses a custom key-encryption provider, configure the CLI equivalently before using it; this CLI provides the directory provider only. Do not copy plaintext keys into the repository.

Inspection reports whether the existing authenticator can be decrypted. Bootstrap requires a readable authenticator. Recovery replaces the authenticator and can recover an account after old key loss, but all participating API instances must be able to read the new key ring. Key-ring loss is a broader incident; this command only repairs the selected account.

## Inspect first — no account changes

Replace placeholders; the User Secrets ID below is the existing TheOne development configuration, not a credential.

```powershell
dotnet run --project src/TheOne.Operations -- inspect --user-id "<user-guid>" --email "<registered-email>" --expect-host localhost --expect-port 5432 --expect-database theone_dev --secrets-id 25a29d9a-f12c-446c-94b9-0c3e8c714a8d
```

Output includes active/contact/MFA/lockout state and existing SuperAdmin count, without passwords, authenticator keys or recovery codes. Both `bootstrap` and `recover` also default to inspection if `--apply` is absent. Inspection is not a guarantee that apply will succeed; all eligibility and approval checks run again inside the operation transaction.

Use `dotnet run --project src/TheOne.Operations -- --help` for the option list.

## First SuperAdmin bootstrap

1. On a fresh installation, register the explicitly selected account through normal registration.
2. Verify its contact, enroll an authenticator and save its recovery codes privately.
3. Obtain the approval described above.
4. Run the inspected command with `bootstrap` and explicit apply flags:

```powershell
dotnet run --project src/TheOne.Operations -- bootstrap --user-id "<user-guid>" --email "<registered-email>" --expect-host localhost --expect-port 5432 --expect-database theone_dev --secrets-id 25a29d9a-f12c-446c-94b9-0c3e8c714a8d --apply --operator "<operator-id>" --approved-by "<different-approver-id>" --ticket "<approval-reference>" --identity-verified
```

The command requires an active, contact-verified, unlocked, authenticator-enrolled account. It refuses if **any** SuperAdmin membership exists, including inactive/locked accounts and the legacy spaced alias. Concurrent bootstrap and role-configuration operations share the same lock.

Successful bootstrap grants SuperAdmin, rotates the security stamp, invalidates old refresh sessions and pending OTP/MFA challenges, and commits an `Operator.SuperAdminBootstrapped` event atomically. It does not change the password, contact data or existing authenticator. The account signs in again using password plus authenticator.

Your existing local SuperAdmin was provisioned earlier. Do not rerun bootstrap there: it will refuse. This supported command is for fresh deployments.

## Full administrator recovery

Use this when an Admin or SuperAdmin has lost the password and second factor, or cannot resume an expired protected-reset enrollment. It is not a public password-reset endpoint and does not grant roles.

```powershell
dotnet run --project src/TheOne.Operations -- recover --user-id "<user-guid>" --email "<registered-email>" --expect-host localhost --expect-port 5432 --expect-database theone_dev --secrets-id 25a29d9a-f12c-446c-94b9-0c3e8c714a8d --apply --operator "<operator-id>" --approved-by "<different-approver-id>" --ticket "<incident-reference>" --identity-verified
```

Apply requires an interactive input/output terminal. The command prompts for the new password twice without echo. Password flags are rejected. The replacement must be 12–128 characters and satisfy the current Identity password policy.

Within one transaction, recovery:

- Resets the password through Identity's password-reset API.
- Clears lockout/failure counters for the selected active administrator.
- Rotates the authenticator secret and removes all old recovery codes.
- Disables completed MFA enrollment temporarily while retaining administrator roles; this keeps password-only administrator login blocked.
- Rotates the security stamp, revokes refresh sessions and consumes existing OTP/MFA challenges.
- Creates a new five-minute enrollment challenge.
- Records `Operator.AdminRecoveryStarted`, the target ID, operator, approver, ticket and attestation without recording passwords or the enrollment capability.

The command prints the confidential **enrollment challenge ID once** after commit. It grants setup access for the selected account. It is not an access token, but must be handled as a short-lived secret. Do not put it in tickets, logs or chats. Transfer it only to the verified owner through the approved secure session.

The owner immediately completes:

1. POST `/api/v1/auth/authenticator/setup` with `{ "challengeId": "<returned-id>" }`.
2. Add the returned new key/QR to their authenticator app.
3. POST `/api/v1/auth/authenticator/confirm-setup` with the same challenge and a current six-digit `code`.
4. Save the new recovery codes privately. The old authenticator/codes no longer work.
5. Sign in with the new password and authenticator. No access token is issued by the operator command.

Setup confirmation records `Authentication.AuthenticatorEnrolled`. An operator can correlate it by target account and time with the incident audit.

If the challenge expires, is lost, or terminal output fails after commit, the account remains blocked from administrator login. Inspect state and repeat the approved recovery command to rotate credentials again and issue a fresh challenge. The previous challenge stays invalid. Never assume a retry rolled back a committed recovery. An invalid password or transactional failure rolls back the credential changes; verify state following any uncertain result.

## Completion and incident evidence

- Verify the owner can sign in with password plus new authenticator.
- Confirm old access/refresh sessions fail and new recovery codes are stored privately.
- Review the operator event and enrollment/login events; record only event IDs and outcomes in the ticket.
- Notify the owner through the independently verified contact according to your incident process.
- End temporary operational access and close the approval ticket.
- Do not roll back the database to restore old authentication credentials as a recovery shortcut.

Operator audit entries use a null application actor ID because the operator is not authenticated through the application. Operator/approver/ticket are in safe audit metadata. Rejected CLI attempts do not create a successful-operation event; retain the external ticket/host access evidence. Existing audit immutability protection applies to committed entries.

## Verification

Automated tests use isolated PostgreSQL databases and cover empty-install bootstrap, concurrent bootstrap, approval/target checks, full recovery and API re-enrollment, old credentials/session invalidation, expiry/reissue, password-policy rollback, and strict CLI argument parsing. No live administrator is reset during testing.
