namespace Streetcode.WebApi;

using Hangfire;
using Streetcode.BLL.Services.BlobStorageService;
using Streetcode.WebApi.Extensions;
using Streetcode.WebApi.ExceptionHandlers;
using Streetcode.WebApi.Utils;
using DotNetEnv;

public class Program
{
    public static async Task Main(string[] args)
    {
        Env.NoClobber().TraversePath().Load();
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.ConfigureApplication();

        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddSwaggerServices();
        builder.Services.AddCustomServices();
        builder.Services.ConfigureBlob(builder);
        builder.Services.ConfigurePayment(builder);
        builder.Services.ConfigureInstagram(builder);
        builder.Services.ConfigureSerilog(builder);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        var app = builder.Build();

        app.UseExceptionHandler();

        if (app.Environment.EnvironmentName == "Local")
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebAPIv5 v1"));
        }
        else
        {
            app.UseHsts();
        }

        await app.ApplyMigrations();

        // await app.SeedDataAsync(); // uncomment for seeding data in local
        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseCors();
        app.UseExceptionHandler();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHangfireDashboard("/dash");

        if (app.Environment.EnvironmentName != "Local")
        {
            BackgroundJob.Schedule<WebParsingUtils>(
            wp => wp.ParseZipFileFromWebAsync(), TimeSpan.FromMinutes(1));
            RecurringJob.AddOrUpdate<WebParsingUtils>(
                wp => wp.ParseZipFileFromWebAsync(), Cron.Monthly);
            RecurringJob.AddOrUpdate<BlobService>(
                b => b.CleanBlobStorage(), Cron.Monthly);
        }

        app.MapGet("/api/test-error", () =>
        {
            throw new InvalidOperationException("Sensitive manual test message");
        });

        app.MapControllers();

        await app.RunAsync();
    }
}
