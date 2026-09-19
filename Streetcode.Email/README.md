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
6. Persist `Sending` before contacting SMTP, then persist `Sent` after SMTP
   acceptance.
7. Retry explicit SMTP failures through Hangfire and persist `Failed` after
   retries are exhausted.
8. Mark an interrupted, ambiguous SMTP attempt as `DeliveryUncertain` instead
   of automatically sending the same message again.
9. Move invalid or terminally unprocessable Kafka records to
   `email.requested.v1.dlq` before committing their source offset.

`IsJobScheduled` prevents a repeated Kafka event from normally creating a
second Hangfire job. If enqueueing fails, the flag remains `false`, so Kafka
redelivery can safely retry scheduling.

## Delivery guarantee

Kafka consumption, database persistence, and Hangfire scheduling are
at-least-once and idempotent by `MessageId` during normal operation.

SMTP does not support a transaction shared with the application database.
There is an unavoidable crash window after the SMTP server accepts a message
and before the `Sent` state is committed. The service persists `Sending`
before SMTP and does not automatically resend a delivery that remains in that
state after interruption. It records `DeliveryUncertain` for manual inspection
and operator-controlled recovery. This favors avoiding duplicate recipient
messages over automatic recovery from an ambiguous attempt and is still not an
exactly-once guarantee.

Connection and authentication failures, plus explicit SMTP command rejection,
are known to occur before acceptance and remain retryable. I/O, protocol, and
other failures raised during `SendAsync` have an ambiguous acceptance outcome
and are recorded as `DeliveryUncertain` without automatic resend.

EF Core retries transient SQL failures inside the current operation. Every
SMTP attempt also uses the same deterministic MIME `Message-Id` derived from
`MessageId`, which improves traceability and allows an SMTP provider to
deduplicate when it supports that behavior.

### Recovering `DeliveryUncertain`

There is intentionally no public recovery endpoint. An operator must recover
an uncertain delivery under change control:

1. Find the SMTP provider record by the deterministic MIME `Message-Id`
   `<message-id>@email.streetcode`.
2. If the provider confirms acceptance, change the delivery status from
   `DeliveryUncertain` to `Sent`.
3. If the provider confirms that the message was not accepted, atomically
   change the status to `Pending` and set `IsJobScheduled` to `false`.
4. Replay the original `EmailRequestedV1` event with the same Kafka key,
   `MessageId`, correlation data, template, and template data. The consumer
   will schedule a new Hangfire job.

Do not replay while SMTP acceptance is unknown. Record every manual database
change and keep a backup before recovery.

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
