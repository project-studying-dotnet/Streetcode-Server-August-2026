using Microsoft.EntityFrameworkCore;
using Streetcode.Identity.Application;
using Streetcode.Identity.Infrastructure;
using Streetcode.Identity.Infrastructure.Messaging.Kafka;
using Streetcode.Identity.Infrastructure.Persistence;
using Streetcode.Identity.Infrastructure.Security;
using Streetcode.Identity.WebApi.ExceptionHandling;
using Streetcode.Identity.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(
    prefix: "STREETCODE_IDENTITY_");

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured");
}

builder.Services.AddJwtServices(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddRefreshTokenServices(builder.Configuration);
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddProblemDetails();

builder.Services
    .AddExceptionHandler<ValidationExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<StreetcodeIdentityDbContext>();

    await dbContext.Database.MigrateAsync();

    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();
app.Run();

public partial class Program;
