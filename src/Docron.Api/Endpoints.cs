using Docker.DotNet;
using Docker.DotNet.Models;
using Docron.Common.Domain;
using Docron.Common.Dto;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Quartz.Impl.Matchers;
using TimeZoneConverter;

namespace Docron.Api;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/version", GetVersion)
            .Produces(200)
            .ProducesValidationProblem();
        
        endpoints
            .MapGet("/jobs", GetJobsAsync)
            .Produces(200)
            .ProducesValidationProblem();

        endpoints
            .MapGet("/containers", GetContainersAsync)
            .Produces(200)
            .ProducesValidationProblem();

        endpoints.MapPost("/jobs", CreateJobAsync)
            .Produces(200)
            .ProducesValidationProblem();

        endpoints.MapDelete("/jobs/{id}", DeleteJobAsync)
            .Produces(200)
            .ProducesValidationProblem();

        return endpoints;
    }

    private static IResult GetVersion(IConfiguration configuration)
    {
        var version = configuration.GetValue<string>("Version")!;

        return Results.Ok(version);
    }
    
    private static async Task<IResult> GetJobsAsync(ISchedulerFactory schedulerFactory)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(JobConstants.Group));

        var records = new List<JobRecordDto>();
        
        foreach (var jobKey in jobKeys)
        {
            var jobDetail = await scheduler.GetJobDetail(jobKey);
            var triggers = await scheduler.GetTriggersOfJob(jobKey);
            var firstTrigger = triggers.FirstOrDefault();

            var containerName = (string)jobDetail!.JobDataMap[JobConstants.ContainerName];
            var cron = (string)jobDetail.JobDataMap[JobConstants.Cron];
            var jobType = Enum.Parse<JobTypes>((string)jobDetail.JobDataMap[JobConstants.Type]);
            var nextFireTime = firstTrigger?.GetNextFireTimeUtc();

            records.Add(new JobRecordDto
            {
                Id = jobKey.Name,
                ContainerName = containerName,
                Description = jobDetail.Description!,
                Cron = cron,
                JobType = jobType,
                NextRun = nextFireTime,
            });
        }

        return Results.Ok(records);
    }

    private static async Task<IResult> GetContainersAsync(IDockerClient dockerClient, IConfiguration configuration)
    {
        var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Limit = 200,
        });
        
        var exclusions = configuration.GetContainerExclusions();

        var containerRecords = new List<ContainerRecordDto>();
        
        foreach (var container in containers)
        {
            var name = container.Names.First().Remove(0, 1);

            if (exclusions.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }
            
            containerRecords.Add(new ContainerRecordDto
            {
                Id = container.ID,
                Name = name
            });
        }

        return Results.Ok(containerRecords);
    }

    private static async Task<IResult> CreateJobAsync(
        [FromServices]ISchedulerFactory schedulerFactory,
        [FromBody]CreateJobRecordDto request)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        
        try
        {
            TriggerBuilder.Create()
                .WithCronSchedule(request.Cron)
                .Build();
        }
        catch (FormatException e)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>()
            {
                {"Validation", [e.Message]},
            });
        }

        var jobType = Enum.Parse<JobTypes>(request.JobType);
        var jobBuilder = JobBuilderFactory.For(jobType);

        var job = jobBuilder
            .UsingJobData(JobConstants.ContainerName, request.ContainerName)
            .UsingJobData(JobConstants.ContainerId, request.ContainerId)
            .UsingJobData(JobConstants.Cron, request.Cron)
            .UsingJobData(JobConstants.Type, request.JobType)
            .StoreDurably()
            .DisallowConcurrentExecution()
            .Build();

        var timeZone = TZConvert.GetTimeZoneInfo(request.TimeZoneId);

        var trigger = TriggerBuilder.Create()
            .WithIdentity(job.Key.Name, job.Key.Group)
            .WithCronSchedule(request.Cron, b => b.InTimeZone(timeZone))
            .Build();

        await scheduler.ScheduleJob(job, [trigger], replace: true);

        return Results.Ok();
    }

    private static async Task<IResult> DeleteJobAsync(
        [FromServices]ISchedulerFactory schedulerFactory,
        string id)
    {
        var scheduler = await schedulerFactory.GetScheduler();

        var jobKey = new JobKey(id, JobConstants.Group);
        
        await scheduler.DeleteJob(jobKey);

        return Results.Ok();
    }
}