# Streetcode Email Microservice

The service consumes versioned email requests from Kafka, persists delivery
state, schedules durable Hangfire jobs, and sends allowlisted templates through
SMTP.

## Delivery flow

1. Consume `email.requested.v1` with manual offset management.
2. Validate the Kafka key and `EmailRequestedV1` payload.
3. Persist an `EmailDelivery` using `MessageId` as the primary idempotency key.
4. Enqueue a Hangfire job and persist `IsJobScheduled`.
5. Commit the Kafka offset only after Hangfire accepts the job.
6. Retry SMTP failures through Hangfire and persist `Sent` or `Failed`.
7. Move invalid or terminally unprocessable Kafka records to
   `email.requested.v1.dlq` before committing their source offset.

`IsJobScheduled` prevents a repeated Kafka event from normally creating a
second Hangfire job. If enqueueing fails, the flag remains `false`, so Kafka
redelivery can safely retry scheduling.

## Delivery guarantee

Kafka consumption, database persistence, and Hangfire scheduling are
at-least-once and idempotent by `MessageId` during normal operation.

SMTP does not support a transaction shared with the application database.
There is an unavoidable crash window after the SMTP server accepts a message
and before the `Sent` state is committed. A retry in that window can produce a
duplicate email. Every retry uses the same deterministic MIME `Message-Id`
derived from `MessageId`, which improves traceability and allows an SMTP
provider to deduplicate when it supports that behavior, but it is not an
exactly-once guarantee.

## Local development

Docker Compose supplies SQL Server, Kafka, Kafka topics, Mailpit, and all
non-production service settings:

```bash
docker compose up -d --build email-api
```

Mailpit is available at `http://localhost:8025` by default. Health endpoints
are available at `/health/live` and `/health/ready`.

When running the WebApi directly, keep the SQL password outside Git. Configure
the connection string through an environment variable or .NET user secrets:

```bash
dotnet user-secrets set \
  "ConnectionStrings:EmailDatabase" \
  "Server=localhost,1434;Database=StreetcodeEmail;User Id=sa;Password=YOUR_LOCAL_PASSWORD;TrustServerCertificate=True" \
  --project src/Streetcode.Email.WebApi/Streetcode.Email.WebApi.csproj
```

Kafka, SMTP, and feedback-recipient settings can be supplied with the same
configuration providers. Never commit real SMTP credentials.

## Database migrations

The WebApi applies EF Core migrations during startup. Deployment must ensure
that only trusted service instances can modify the Email database schema and
that the SQL account has the required migration permissions.
