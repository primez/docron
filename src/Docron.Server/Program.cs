using Docker.DotNet;
using Docron.Server;
using Microsoft.FluentUI.AspNetCore.Components;
using Docron.Server.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Quartz;
using Quartz.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(opt =>
    {
        opt.DisconnectedCircuitMaxRetained = 1;
        opt.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(1);
    });
builder.Services.AddFluentUIComponents();
builder.Services
    .AddDataProtection()
    .SetApplicationName("Docron")
    .UseCryptographicAlgorithms(
        new AuthenticatedEncryptorConfiguration
        {
            EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
            ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
        })
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration.GetKeysPath()!));

builder.Services.AddQuartz(opt =>
{
    opt.UsePersistentStore(b =>
    {
        var connectionString = builder.Configuration.GetDbConnection();

        b.UseMicrosoftSQLite(cOpt => { cOpt.ConnectionString = connectionString!; });
        b.UseProperties = true;
        b.UseSystemTextJsonSerializer();
        b.PerformSchemaValidation = true;
    });
    opt.MaxBatchSize = 2;
    opt.InterruptJobsOnShutdown = false;
    opt.CheckConfiguration = true;
});
builder.Services.AddQuartzServer(opt => { opt.AwaitApplicationStarted = true; });

builder.Services.AddScoped<IDockerClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var dockerConnection = configuration.GetValue<string>("DOCKER_CONNECTION");

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