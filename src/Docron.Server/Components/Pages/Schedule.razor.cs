using Docker.DotNet;
using Docker.DotNet.Models;
using Docron.Server.Domain;
using Docron.Server.Jobs;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Extensions;
using Quartz;
using Quartz.Impl.Matchers;

namespace Docron.Server.Components.Pages;

public partial class Schedule
{
    private List<ScheduleEntry> _scheduleEntries = [];
    private IQueryable<ScheduleEntry> _scheduleEntriesQueryable = default!;
    private FluentDataGrid<ScheduleEntry> _grid = default!;
    private int _index;
    
    private Dictionary<string, string> _containers = [];
    private string _selectedContainerId = default!;

    private readonly Dictionary<JobTypes, string> _jobTypes = Enum.GetValues<JobTypes>()
        .Where(jt => jt != JobTypes.None)
        .ToDictionary(jt => jt, jt => jt.GetDisplayName());
    private string _selectedJobType = default!;

    private string _cron = default!;

    [Inject] private ISchedulerFactory SchedulerFactory { get; set; } = default!;

    [Inject] private IDockerClient DockerClient { get; set; } = default!;

    [Inject] private IMessageService Messages { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var scheduler = await SchedulerFactory.GetScheduler();

        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());

        foreach (var key in jobKeys)
        {
            var jobDetail = await scheduler.GetJobDetail(key);
            var triggers = await scheduler.GetTriggersOfJob(key);
            var firstTrigger = triggers.FirstOrDefault();

            var containerName = (string)jobDetail!.JobDataMap[JobConstants.ContainerName];
            var cron = (string)jobDetail.JobDataMap[JobConstants.Cron];
            var nextFireTime = firstTrigger?.GetNextFireTimeUtc().ToLocalTime();

            _scheduleEntries.Add(new ScheduleEntry(
                ++_index,
                key,
                containerName,
                jobDetail.Description!,
                cron,
                nextFireTime));
        }

        _scheduleEntriesQueryable = _scheduleEntries.AsQueryable();

        var containers = await DockerClient.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Limit = 50,
        });

        foreach (var container in containers)
        {
           _containers.Add(container.ID, container.Names.First().Remove(0, 1));
        }
    }

    private async Task AddJobAsync()
    {
        if (string.IsNullOrEmpty(_selectedContainerId) ||
            string.IsNullOrEmpty(_selectedJobType) ||
            string.IsNullOrEmpty(_cron))
        {
            await Messages.ShowMessageBarAsync(opt =>
            {
                opt.Intent = MessageIntent.Warning;
                opt.Timeout = 5000;
                opt.Body = "Select a container, type and enter a cron expression";
                opt.Title = "Validation";
                opt.Section = "MESSAGES_TOP";
            });
            return;
        }

        try
        {
            TriggerBuilder.Create()
                .WithCronSchedule(_cron)
                .Build();
        }
        catch (FormatException e)
        {
            await Messages.ShowMessageBarAsync(opt =>
            {
                opt.Intent = MessageIntent.Warning;
                opt.Timeout = 5000;
                opt.Body = e.Message;
                opt.Title = "Validation";
                opt.Section = "MESSAGES_TOP";
            });
            
            return;
        }

        var scheduler = await SchedulerFactory.GetScheduler();

        var jobType = Enum.Parse<JobTypes>(_selectedJobType);

        var jobBuilder = jobType switch
        {
            JobTypes.StartContainer => JobBuilder.Create<StartContainerJob>()
                .WithIdentity(StartContainerJob.Key)
                .WithDescription(StartContainerJob.Description),
            JobTypes.StopContainer => JobBuilder.Create<StopContainerJob>()
                .WithIdentity(StopContainerJob.Key)
                .WithDescription(StopContainerJob.Description),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var job = jobBuilder
            .UsingJobData(JobConstants.ContainerName, _containers[_selectedContainerId])
            .UsingJobData(JobConstants.ContainerId, _selectedContainerId)
            .UsingJobData(JobConstants.Cron, _cron)
            .StoreDurably()
            .DisallowConcurrentExecution()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(job.Key.Name, job.Key.Group)
            .WithCronSchedule(_cron)
            .Build();

        _scheduleEntries.Add(new ScheduleEntry(
            _scheduleEntries.Count + 1,
            job.Key,
            _containers[_selectedContainerId],
            job.Description!,
            _cron,
            trigger.GetNextFireTimeUtc().ToLocalTime()));

        await scheduler.ScheduleJob(job, [trigger], replace: true);

        await _grid.RefreshDataAsync();
    }

    /*private Task EditAsync()
    {
        return Task.CompletedTask;
    }*/

    private async Task DeleteAsync(ScheduleEntry entry)
    {
        var scheduler = await SchedulerFactory.GetScheduler();

        await scheduler.DeleteJob(entry.Key);

        _scheduleEntries.Remove(entry);

        _index = 0;

        _scheduleEntries = _scheduleEntries
            .Select(se => se with { Index = ++_index })
            .ToList();

        await _grid.RefreshDataAsync();
    }

    private sealed record ScheduleEntry(
        int Index,
        JobKey Key,
        string ContainerName,
        string Description,
        string Cron,
        DateTimeOffset? NextFireTime);
}