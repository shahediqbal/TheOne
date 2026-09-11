# SMS OTP verification and password recovery

## Endpoints

All endpoints are under `/api/v1/auth` and use the existing response wrapper.

| POST route | Authorization | Request |
| --- | --- | --- |
| request-mobile-verification | JWT | No body; uses the registered mobile |
| verify-mobile | JWT | `challengeId`, `code` |
| forgot-password | Anonymous | `userNameOrMobile` |
| reset-password | Anonymous | `challengeId`, `code`, `newPassword`, `confirmPassword` |

Code-request responses include a challenge identifier, 300-second lifetime, and 60-second resend interval.
They never include the OTP. Codes contain six digits and can begin with zero; send them as strings.

Password recovery requires an active account with a previously verified mobile number.
Unknown, unverified, inactive, two-factor-enabled, and request-limited accounts receive the same
generic recovery acknowledgement. Recovery does not automatically log in the user or unlock an
Identity lockout. Existing password login remains available before mobile verification.

## Local Swagger walkthrough

1. Register and log in. Authorize Swagger with the access token.
2. Set these two development settings in the API's User Secrets (merge them with existing settings):
   ```json
   "SmsProvider": {
     "Enabled": false,
     "DevelopmentOutboxEnabled": true
   }
   ```
3. Restart the API using the Development environment.
4. Call `request-mobile-verification` and copy `data.challengeId`.
5. Read the latest file in `%LOCALAPPDATA%\TheOne\DevelopmentSmsOutbox`.
   It contains the local test message; no SMS was sent. Do not commit or share these files.
6. Call `verify-mobile` with the challenge ID and six-digit OTP.
7. Wait at least 60 seconds from the previous request. Call `forgot-password` with the account email
   or its registered mobile representation. Read the new outbox file.
8. Call `reset-password` with its challenge ID, code, new password, and confirmation.
9. Confirm the old refresh token is rejected and the new password works.

The outbox is explicitly opt-in and rejected outside Development. If real delivery and the
development outbox are both disabled, requests return 503 rather than pretending a code was sent.
Outbox files are local plaintext test messages; remove them when finished.

## BulkSMSBD configuration

Keep provider credentials in User Secrets or deployment secret storage:

```json
"SmsProvider": {
  "BaseUrl": "https://bulksmsbd.net/api/smsapi",
  "ApiKey": "<rotated key from secret storage>",
  "SenderId": "8809617613593",
  "Type": "text",
  "Enabled": false,
  "DevelopmentOutboxEnabled": false
}
```

Enable real SMS only when ready to send messages. The adapter requires HTTPS; the HTTP URL in
the supplied account documentation is rejected. Do not disable TLS certificate checks. The HTTPS
endpoint and delivery acceptance still need a controlled live check with the provider and a test
recipient; implementation tests do not contact BulkSMSBD.

The supplied provider documentation specifies POST fields `api_key`, `senderid`, `number`,
`type`, and `message`, success code `202`, and the wording `Your {Brand/Company Name} OTP is XXXX`.
The adapter uses `Your The One OTP is <six digits>`, URL-encoded POST fields, and converts valid
Bangladesh local `01...` / `+8801...` numbers to the provider's `8801...` format.
It currently expects JSON `response_code` equal to numeric or string `202`; malformed responses
and other codes fail closed. The exact response envelope still needs confirmation from a live
provider response or the account's C# example.

HTTP redirects and automatic retries are disabled to avoid leaking fields or duplicating sends.
Requests time out after 10 seconds. Provider keys, response bodies, and OTPs are not logged.
Success means provider acceptance, not confirmed handset delivery.

## Storage and transaction behavior

`AddOtpChallenges` creates the OTP table, account foreign key, and indexes; it does not modify
existing user or refresh-token data. If setting up another environment, apply migrations with:

```powershell
Update-Database -Project TheOne.Persistence -StartupProject TheOne.API
```

Codes are encrypted with ASP.NET Core Data Protection and bound to an unpredictable challenge ID.
They are also checked against the intended operation, owning user, phone snapshot, and Identity
security stamp. Persist and protect the Data Protection key ring in deployments; share the key
ring and application name `TheOne` between instances. Losing keys invalidates outstanding codes.

Each challenge expires after five minutes and permits five wrong attempts. Requests have a
60-second cooldown and a five-per-hour account limit, shared between both purposes. Resending
invalidates earlier challenges of the same purpose. The existing per-IP API limiter also applies.

Request history is committed before SMS submission, so canceled or failed deliveries still count
toward request limits. A challenge remains unusable until the sender accepts it. Failed/canceled
deliveries are invalidated. A crash between acceptance and recording acceptance leaves an unusable
challenge; request a new one after the cooldown. There is no automatic delivery retry or durable
delivery worker in this increment. Sending is synchronous, so generic acknowledgements are not a
guarantee of identical response timing.

Verification, reset, and failed-attempt updates use the same account row locks as existing
authentication operations. A reset uses Identity password validation and hashing, consumes all
outstanding challenges, rotates the security stamp, and revokes all refresh sessions atomically.
An intervening password change also invalidates outstanding challenges through the security stamp.
Already-issued access JWTs retain the existing short-lived expiry behavior.

## Tests

Set `THEONE_AUTH_TEST_CONNECTION` to an isolated disposable PostgreSQL database and run:

```powershell
dotnet test src/TheOne.IntegrationTests/TheOne.IntegrationTests.csproj
```

Database test classes share a collection to prevent test-host migration races. Explicit concurrent
refresh and reset tests still issue parallel requests. Recovery tests replace SMS with a recording
sender and use a controllable clock. Provider tests use an in-memory HTTP handler. No test sends SMS.

Coverage includes the original authentication flows plus mobile verification, password reset,
single-use consumption, concurrent reset, purpose/account binding, expiry, attempt and resend
limits, password-change invalidation, cancellation, delivery failure, protected-code tampering,
provider encoding/response handling, unsafe configuration rejection, and the production outbox guard.

Current-user and session-management APIs are covered in [session setup](user-sessions.md).
Email verification, OTP login, session-management screens, and durable security audit remain later increments.

## Authenticator policy update

See [authenticator login](authenticator-login.md) for mandatory administrator MFA, optional SMS login for ordinary accounts, and immediate session validation for enrolled accounts. These rules supersede earlier login and access-token descriptions for MFA/administrator accounts.
