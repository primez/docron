namespace Docron;

public static class ConfigurationExtensions
{
    public static string? GetDbConnection(this IConfiguration configuration)
    {
        return configuration.GetValue<string>("DB") ??
               configuration.GetConnectionString("Docron");
    }
    
    public static string? GetKeysPath(this IConfiguration configuration)
    {
        return configuration.GetValue<string>("KEY_PATH") ??
               configuration.GetValue<string>("KeysPath");
    }

    public static IReadOnlyCollection<string> GetContainerExclusions(this IConfiguration configuration)
    {
        var section = configuration.GetSection("EXCLUDED_CONTAINERS");

        var exclusions = section.Get<string>()?
            .Replace("[", string.Empty)
            .Replace("]", string.Empty)
            .Split([",", " "], StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Select(e => e.Trim('-'))
            .Where(e => e != " ")
            .ToArray();
        
        if (exclusions is null)
        {
            section = configuration.GetSection("ExcludedContainers");
            exclusions = section.Get<string[]>();
        }

        return exclusions ?? [];
    }
}