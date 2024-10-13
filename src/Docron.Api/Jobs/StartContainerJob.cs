using Docker.DotNet;
using Docker.DotNet.Models;
using Docron.Common.Domain;
using Quartz;

namespace Docron.Api.Jobs;

public sealed class StartContainerJob(IDockerClient dockerClient, ILogger<StartContainerJob> logger) : IJob
{
    public const string Description = "Starts a container on schedule";

    public async Task Execute(IJobExecutionContext context)
    {
        if (context.RefireCount > 10)
        {
            logger.LogWarning("This job has has re-fired {Number} times", context.RefireCount);
            return;
        }

        try 
        {
            var containerId = context.MergedJobDataMap.GetString(JobConstants.ContainerId);
            var containerName = context.MergedJobDataMap.GetString(JobConstants.ContainerName);

            logger.LogInformation("Starting a container \"{ContainerName}\"", containerName);

            await dockerClient.Containers.StartContainerAsync(
                containerId,
                new ContainerStartParameters(),
                cancellationToken: context.CancellationToken);
            
            logger.LogInformation("Container \"{ContainerName}\" has started", containerName);
        }
        catch (Exception ex)
        {
            // do you want the job to re-fire?
            throw new JobExecutionException(msg: "Cannot complete this job", refireImmediately: false, cause: ex);
        }
    }
}