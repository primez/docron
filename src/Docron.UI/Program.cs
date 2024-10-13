using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using Docron.UI;
using Microsoft.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddApiClient(opt =>
{
    var navigationManager = builder.Services.BuildServiceProvider().GetRequiredService<NavigationManager>();
    var apiBaseUrl = navigationManager.BaseUri;

    opt.Host = apiBaseUrl;
});

builder.Services.AddFluentUIComponents();

await builder.Build().RunAsync();