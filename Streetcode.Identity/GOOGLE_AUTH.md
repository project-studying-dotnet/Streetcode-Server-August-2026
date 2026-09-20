# Google sign-in

Google sign-in is implemented in the Identity service. It issues the same Streetcode
access and refresh tokens as password login. Existing refresh/logout endpoints apply.
No schema migration is needed: links use `AspNetUserLogins` (`Google`, Google `sub`).

## Configuration

Create a Google OAuth web client and configure its authorized JavaScript origins for
the frontend. Set `STREETCODE_IDENTITY_GoogleAuth__ClientId` to its Client ID in the
Identity process environment (or `.env` for Docker Compose). No client secret is
needed for this ID-token verification flow. An empty Client ID disables these
endpoints with HTTP 503 while leaving password authentication available.

Use Google Identity Services' JavaScript callback to obtain an ID token, then send
it to Identity using HTTPS and `Content-Type: application/json`. This API does not
accept Google's form-post redirect flow. Do not pass a Google access token, an email
from the browser, or a decoded-but-unverified JWT as proof of identity. Tokens are
verified against Google's HTTPS discovery/signing keys, issuer, audience, expiry,
RS256 signature and verified email. Discovery is cached and key refresh is requested
when an unknown signing key is encountered. Google tokens and passwords are not logged.

## Sign in

`POST /api/auth/google`

```json
{ "idToken": "<Google ID token>" }
```

- A known Google subject signs in to its linked local account, even if Google's email
  has changed. Inactive or locked accounts cannot sign in.
- A new subject with an unused email creates a passwordless user, the `User` role
  association, the external login and an outbox event in one transaction.
- An existing email without this Google link returns HTTP 409 and ProblemDetails
  `code: "Google.LinkRequired"`. Nothing is linked and no session is issued.
- Invalid/unverified tokens return 401; unavailable Google configuration returns 503.
- Success returns `accessToken`, `accessTokenExpiresAt`, `refreshToken`,
  `refreshTokenExpiresAt`, matching password login.

## Explicitly link an existing password account

After `Google.LinkRequired`, ask the user to sign in with their existing password.
Show a separate confirmation screen describing which Google account will be linked.
On confirmation, obtain a Google ID token and call:

`POST /api/auth/google/link`

`Authorization: Bearer <Streetcode access token>`

```json
{
  "idToken": "<Google ID token>",
  "password": "<current local password>",
  "confirmLink": true
}
```

The user ID and access version come exclusively from the authenticated Streetcode
JWT. The service checks account activity, current access version, password (with
Identity lockout), and matching normalized emails. A Google identity belonging to
someone else cannot be moved. This version permits one Google link per user and
does not change the password or silently merge accounts. Success returns 204;
invalid credentials return 401, link conflicts return 409, and missing confirmation
or invalid input returns 400. Repeating the same authorized link is idempotent.

The frontend must send this request only after the separate user confirmation.
Authorization uses a bearer header, not ambient cookies. Form content types are
rejected; if enabling cross-origin access, allow only your frontend origins. The
Identity service currently needs a same-origin proxy or explicit CORS configuration
for a separately hosted frontend. The new API does not set login cookies or put
credentials in redirect URLs. Gateway `/api/auth/**` routing can forward both routes.

## Verification

`dotnet test Streetcode.Identity/Streetcode.Identity.sln`

SQL integration tests use Docker by default. Alternatively set
`STREETCODE_IDENTITY_TEST_SQLSERVER` to a local test SQL Server connection string.
The fixture overrides its database name with a fresh GUID-based test database and
deletes that database after the run; the login needs database creation permission.
Never supply production credentials. Google
signature tests use local RSA keys and never contact Google. Service integration
tests cover first/repeated login, email collisions, explicit linking, preserved
passwords, disabled users, invalid passwords/versions/emails, and ownership conflicts.
Actual Google-browser sign-in requires a configured Google web client and frontend.
