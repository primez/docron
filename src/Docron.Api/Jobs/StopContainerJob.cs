using Docker.DotNet;
using Docker.DotNet.Models;
using Docron.Common.Domain;
using Quartz;

namespace Docron.Api.Jobs;

public sealed class StopContainerJob(IDockerClient dockerClient, ILogger<StopContainerJob> logger) : IJob
{
    public const string Description = "Stops a container on schedule";

    public async Task Execute(IJobExecutionContext context)
    {
        if (context.RefireCount > 10)
        {
            logger.LogWarning("This job has has re-fired {Number} times", context.RefireCount);
            return;
        }

        try
        {
            var containerName = context.MergedJobDataMap.GetString(JobConstants.ContainerName);

            logger.LogInformation("Stopping a container \"{ContainerName}\"", containerName);

            var containers = await dockerClient.Containers
                .ListContainersAsync(new ContainersListParameters
                {
                    All = true,
                }, context.CancellationToken);

            var targetContainer = containers.FirstOrDefault(c => c.Names.Any(cn => cn.Contains(containerName!)));

            if (targetContainer == null)
            {
                logger.LogInformation("The container with the name \"{ContainerName}\" was not found", containerName);
                return;
            }
            
            await dockerClient.Containers.StopContainerAsync(
                targetContainer.ID,
                new ContainerStopParameters { WaitBeforeKillSeconds = 60 },
                cancellationToken: context.CancellationToken);

            logger.LogInformation("Container \"{ContainerName}\" has stopped", containerName);
        }
        catch (Exception ex)
        {
            // do you want the job to re-fire?
            throw new JobExecutionException(msg: "Cannot complete this job", refireImmediately: false, cause: ex);
        }
    }
}