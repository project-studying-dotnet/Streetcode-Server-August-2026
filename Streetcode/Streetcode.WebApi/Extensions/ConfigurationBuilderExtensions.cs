namespace Streetcode.WebApi.Extensions
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder ConfigureCustom(this IConfigurationBuilder builder, string environment)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("STREETCODE_");

            return builder;
        }

        public static string GetRequiredConnectionString(this IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "The connection string 'ConnectionStrings:DefaultConnection' is missing. " +
                    "Set 'STREETCODE_ConnectionStrings__DefaultConnection' in the environment or .env file.");
            }

            return connectionString;
        }
    }
}
