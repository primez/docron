using System.Text.Json;

namespace Docron.UI.Api;

public static class ApiClientJsonSerializerSettings
{
    public static JsonSerializerOptions Instance { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}