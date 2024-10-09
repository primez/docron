using Docker.DotNet;
using Docker.DotNet.Models;

var connection = Environment.GetEnvironmentVariable("Connection");

Console.WriteLine($"Using the connection: {connection}");

var client = string.IsNullOrEmpty(connection)
    ? new DockerClientConfiguration().CreateClient()
    : new DockerClientConfiguration(new Uri(connection)).CreateClient();

var containers = await client.Containers
    .ListContainersAsync(new ContainersListParameters
    {
        All = true,
        Limit = 50,
    });

foreach (var container in containers)
{
    Console.WriteLine(string.Join("; ", container.Names));
}

