using Streetcode.Email.Application;
using Streetcode.Email.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("EmailDatabase")
                       ?? throw new InvalidOperationException(
                           "Connection string 'EmailDatabase' is not configured.");

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
