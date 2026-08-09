using DbUp;
using DotNetEnv;
using Microsoft.Extensions.Configuration;

public class Program
{
    static int Main(string[] args)
    {
        Env.TraversePath().Load();

        string migrationPath = Path.Combine(Directory.GetCurrentDirectory(),
            "Streetcode.DAL", "Persistence", "ScriptsMigration");

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "Streetcode.WebApi"))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables("STREETCODE_")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The connection string 'ConnectionStrings:DefaultConnection' is missing. " +
                "Set 'STREETCODE_ConnectionStrings__DefaultConnection' in the environment or .env file.");
        }

        string pathToScript = "";

        Console.WriteLine("Enter '-m' to MIGRATE or '-s' to SEED db:");
        pathToScript = Console.ReadLine();

        pathToScript = migrationPath;
        
        var upgrader =
            DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsFromFileSystem(pathToScript)
                .LogToConsole()
                .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Error);
            Console.ResetColor();
#if DEBUG
            Console.ReadLine();
#endif
            return -1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success!");
        Console.ResetColor();
        return 0;
    }
}
