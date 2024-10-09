using Docker.DotNet;
using Docron.Server;
using Microsoft.FluentUI.AspNetCore.Components;
using Docron.Server.Components;
using Quartz;
using Quartz.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

builder.Services.AddQuartz(opt =>
{
    opt.UsePersistentStore(b =>
    {
        b.UseMicrosoftSQLite(cOpt => { cOpt.ConnectionStringName = "Docron"; });
        b.UseProperties = true;
        b.UseSystemTextJsonSerializer();
        b.PerformSchemaValidation = true;
    });
    opt.MaxBatchSize = 2;
    opt.InterruptJobsOnShutdown = false;
});
builder.Services.AddQuartzServer(opt => { opt.AwaitApplicationStarted = true; });

builder.Services.AddScoped<IDockerClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var dockerConnection = configuration.GetValue<string>("DockerConnection");

    return string.IsNullOrEmpty(dockerConnection)
        ? new DockerClientConfiguration().CreateClient()
        : new DockerClientConfiguration(new Uri(dockerConnection)).CreateClient();
});

builder.Services.AddSingleton<DbMigration>();

var app = builder.Build();

var dbMigration = app.Services.GetRequiredService<DbMigration>();

dbMigration.Migrate();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();
app.UseStaticFiles();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();