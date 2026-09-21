using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

var ocelotFile = builder.Environment.IsEnvironment("Docker")
    ? "ocelot.Docker.json"
    : "ocelot.json";

builder.Configuration.AddJsonFile(
    ocelotFile,
    optional: false,
    reloadOnChange: true);

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

await app.UseOcelot();

await app.RunAsync();
