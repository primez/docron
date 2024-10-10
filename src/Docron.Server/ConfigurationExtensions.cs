namespace Docron.Server;

public static class ConfigurationExtensions
{
    public static string? GetDbConnection(this IConfiguration configuration)
    {
        return configuration.GetValue<string>("DB") ??
               configuration.GetConnectionString("Docron");
    }
}