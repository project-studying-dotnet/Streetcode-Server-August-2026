using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Streetcode.Email.Application;
using Streetcode.Email.Infrastructure;
using Streetcode.Email.Infrastructure.EmailSending;
using Streetcode.Email.Infrastructure.HealthChecks;
using Streetcode.Email.Infrastructure.Kafka;
using Streetcode.Email.Infrastructure.Persistence;
using Streetcode.Email.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration
        .AddJsonFile(
            "appsettings.DevelopmentDefaults.json",
            optional: false,
            reloadOnChange: true)
        .AddJsonFile(
            "appsettings.Development.json",
            optional: true,
            reloadOnChange: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables();
}

var connectionString = builder.Configuration.GetConnectionString("EmailDatabase")
                       ?? throw new InvalidOperationException(
                           "Connection string 'EmailDatabase' is not configured.");
builder.Services
    .AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options =>
            string.IsNullOrWhiteSpace(options.Username) ==
            string.IsNullOrWhiteSpace(options.Password),
        "SMTP username and password must either both be configured " +
        "or both be omitted.")
    .ValidateOnStart();

builder.Services
    .AddOptions<FeedbackEmailOptions>()
    .Bind(builder.Configuration.GetSection(
        FeedbackEmailOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<KafkaOptions>()
    .Bind(builder.Configuration.GetSection(KafkaOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddHealthChecks()
    .AddCheck<KafkaHealthCheck>(
        "kafka",
        tags: ["ready"])
    .AddDbContextCheck<EmailDbContext>(
        "email-database",
        tags: ["ready"]);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

await app.ApplyDatabaseMigrationsAsync();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = static _ => false,
    })
    .AllowAnonymous();

app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions
        {
            Predicate = static registration =>
                registration.Tags.Contains("ready"),
        })
    .AllowAnonymous();

app.MapControllers();

app.Run();
public partial class Program { }
