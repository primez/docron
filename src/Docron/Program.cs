using Docker.DotNet;
using Docron;
using Quartz;
using Quartz.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddQuartz(opt =>
{
    opt.UseTimeZoneConverter();
    opt.UsePersistentStore(b =>
    {
        var connectionString = builder.Configuration.GetDbConnection();

        b.UseMicrosoftSQLite(cOpt => { cOpt.ConnectionString = connectionString!; });
        b.UseProperties = true;
        b.UseSerializer<QuartzJsonSerializer>();
        b.PerformSchemaValidation = true;
    });
    opt.MaxBatchSize = 2;
    opt.InterruptJobsOnShutdown = false;
    opt.CheckConfiguration = true;
});
builder.Services.AddQuartzServer(opt => { opt.AwaitApplicationStarted = true; });
builder.Services.AddSingleton<DbMigration>();

builder.Services.AddScoped<IDockerClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var dockerConnection = configuration.GetValue<string>("DOCKER_CONNECTION");

    return string.IsNullOrEmpty(dockerConnection)
        ? new DockerClientConfiguration().CreateClient()
        : new DockerClientConfiguration(new Uri(dockerConnection)).CreateClient();
});

var app = builder.Build();

var dbMigration = app.Services.GetRequiredService<DbMigration>();
dbMigration.Migrate();

var options = new DefaultFilesOptions();
options.DefaultFileNames.Clear();
options.DefaultFileNames.Add("index.html");

app.UseDefaultFiles(options);
app.UseStaticFiles();

app.MapApi();

app.Run();