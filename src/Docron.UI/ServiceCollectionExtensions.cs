using System.Net.Http.Headers;
using Docron.UI.Api;
using Refit;

namespace Docron.UI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiClient(
        this IServiceCollection services,
        Action<ApiClientOptions> configureOptions)
    {
        var options = new ApiClientOptions();
        configureOptions(options);

        services
            .AddRefitClient<IApiClient>()
            .ConfigureHttpClient(config =>
            {
                config.BaseAddress = new Uri(options.Host);
                config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

        return services;
    }
}