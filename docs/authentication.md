# Authentication guide

## Identity and access are separate

Browser users establish identity by receiving a one-time code at their email address. The application does not keep a
password store or require an external identity-provider account. After verification, ASP.NET Core issues a nonpersistent,
HTTP-only authentication cookie. Role membership and cache-backed permission queries remain responsible for authorization;
receiving a valid code does not grant tenant, organization, or global administrative permissions by itself.

The public homepage is application content with a login entry point. Keep the login panel distinct so a fork can customize
that page without moving authentication into the authenticated workspace. API-key clients keep their separate authentication
path and do not exchange email codes for cookies.

## Managing API keys

The **API keys** page lists the signed-in user's active, unexpired keys. It shows each key's purpose note, granted
permissions, and last three characters. The full credential is available in the creation response, so users copy it
when they create the key.

`POST /api/users/apikey/create` accepts an optional `note` of up to 200 characters after trimming. A user can issue
multiple keys. `GET /api/users/apikey` lists their active keys, and `DELETE /api/users/apikey/{apiKeyId}` invalidates
one they own. Users manage keys through their browser session with antiforgery protection on creation and revocation.

Revocation deletes the credential and its scope assignments in one database transaction, then evicts the in-process
authentication and permission caches. Instances that share a database need shared cache invalidation for immediate
revocation across instances.

## Request and verify protocol

1. The client obtains an anonymous session response from `GET /api/session`, including a request-verification token.
2. When the user submits an email address, the client sends `POST /api/auth/email/request` with `emailAddress` and the
   tab's unpredictable UUID `sessionId`. The request carries the antiforgery token in `X-CSRF-TOKEN`.
3. The server normalizes the email address, generates a cryptographically secure six-digit decimal code, stores the
   challenge, and sends the code to that address. Its response contains `expiresAt`, never the code.
4. The UI disables the submitted email field and opens the code field. A code is a string so leading zeros are retained.
5. The client sends `POST /api/auth/email/verify` with `emailAddress`, the same `sessionId`, and `token`, again carrying
   the antiforgery token. Successful verification consumes the challenge and issues the authentication cookie.
6. The client refreshes its session, including its antiforgery token, before entering the authenticated workspace.
   Signing out posts to `/Logout` with antiforgery protection.

The code must be verified in the requesting tab or window within 15 minutes, and a successful code cannot be replayed.
Missing, mismatched, expired, consumed, or exhausted challenges fail without authenticating the user. A successful
verification can create the email user or attach to an existing email-based membership; inactive users remain unable to log in.

## Tab ownership

Generate a high-entropy session identifier in the browser and keep it in `sessionStorage`. Do not put it in persistent
`localStorage`, URLs, emails, or logs. `sessionStorage` allows a reload in the requesting tab without deliberately sharing
the identifier with other tabs. Clear completed or cancelled challenge state rather than reusing it for unrelated logins.

Browsers can copy session storage when duplicating a tab. Detect competing live owners and replace a duplicated tab's
identifier; a copied storage value must not silently authorize another window to finish the original challenge. The server
validates the submitted session identifier as part of the challenge rather than trusting the code alone.

This requirement concerns completing the code challenge. Once authentication succeeds, the browser manages the ordinary
same-origin authentication cookie; it is not a separate per-tab authorization system.

## Persistence and single use

SQL Server holds the authoritative challenge state so issuing and verifying requests can reach different application
instances. Persist hashes of the session and code material, the normalized email address, timestamps, failed attempts, and
consumption state. Do not persist or log the raw session identifier or code. The token hash binds the code to both the
session identifier and email address; comparisons use a constant-time operation.

Verification, challenge consumption, and the associated user update are transactional. Concurrent requests with the same
correct code must produce at most one successful verification. Failed attempts must be committed even though the request
returns an error, and consumption must not be undone by an HTTP caller disconnecting after verification.

Consumed challenges remain unavailable until expiry, preserving the request cooldown. Expired challenge rows can be removed
as part of issuance using their expiry index. This state is temporary authentication material, not a permanent activity log.
Database-backed data-protection keys support cookie validation across instances sharing the application database.

## Delivery and limits

Configure `EmailServiceOptions` with either a reachable SMTP relay (`ServerType: "Smtp"`, `SmtpServer`, and `SmtpPort`) or
Resend (`ServerType: "Resend"` and `ResendApiToken`). Configure an appropriate `FromEmailAddress`; the provider or relay
must accept that sender. The current SMTP adapter is for a relay and does not expose username/password settings. The
[README](../readme.md#configure-the-database-and-authentication) contains working configuration shapes.

Login emails always go to the submitted address, including during development. The ordinary application's development
email redirect to administrators does not apply to authentication messages. Disabled or failed delivery produces an explicit
unavailable response and leaves no usable challenge; neither the response nor a development console should expose the code.
For local development, use an SMTP inbox you can inspect or a configured mail provider.

The challenge has a limited number of failed attempts, and requesting another code is subject to a session/email cooldown.
Issuing a replacement invalidates the previous code for that session. The server also applies coarse IP limits to requesting
and verifying codes. `EmailLoginOptions:RequestsPerMinute` and `EmailLoginOptions:VerificationsPerMinute` configure those IP
limits; they do not replace the database-backed challenge checks. These middleware limits are per application instance,
so a deployment can also apply shared limits at its edge. Keep the server's source-address handling consistent with its
trusted proxy configuration when relying on IP limits.

## Test and extension boundaries

The email sender and token generator have test seams so tests can capture delivered messages, control codes, and exercise
expiry or concurrency without sending external mail. Keep deterministic token generation and captured-message access in
the test host. Production generation must use a cryptographically secure random source, and production callers must not
be able to request a known code or substitute a sender.

When changing the flow, test missing or mismatched sessions, wrong and exhausted codes, expiry, replacement, replay,
concurrent consumption, email-delivery failure, inactive users, and antiforgery enforcement. Frontend tests should exercise
email locking, code entry, tab-owned state, reload/cancellation behavior, and error recovery as user-visible interactions.
Keep the protocol and security boundaries synchronized with those tests and with the public configuration instructions.
