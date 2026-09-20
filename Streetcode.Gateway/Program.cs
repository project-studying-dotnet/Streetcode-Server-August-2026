var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var allowedOrigins = builder.Configuration
    .GetRequiredSection("CORS:AllowedOrigins")
    .Get<string[]>()
    ?? throw new InvalidOperationException("CORS allowed origins are missing.");
var allowedHeaders = builder.Configuration
    .GetRequiredSection("CORS:AllowedHeaders")
    .Get<string[]>()
    ?? throw new InvalidOperationException("CORS allowed headers are missing.");
var allowedMethods = builder.Configuration
    .GetRequiredSection("CORS:AllowedMethods")
    .Get<string[]>()
    ?? throw new InvalidOperationException("CORS allowed methods are missing.");

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(allowedOrigins)
            .WithHeaders(allowedHeaders)
            .WithMethods(allowedMethods)));

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseRouting();
app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapReverseProxy();

app.Run();

public partial class Program;
