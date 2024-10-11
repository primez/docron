using Docker.DotNet;
using Docker.DotNet.Models;
using Docron.Server.Domain.Jobs;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Extensions;
using Quartz;
using Quartz.Impl.Matchers;

namespace Docron.Server.Components.Pages;

public partial class Schedule
{
    private Dictionary<string, ScheduleEntry> _scheduleEntries = [];
    private IQueryable<ScheduleEntry> _scheduleEntriesQueryable = default!;
    private FluentDataGrid<ScheduleEntry> _grid = default!;
    private int _index;

    private readonly Dictionary<string, string> _containers = [];
    private string _selectedContainerId = default!;

    private readonly Dictionary<JobTypes, string> _jobTypes = Enum.GetValues<JobTypes>()
        .Where(jt => jt != JobTypes.None)
        .ToDictionary(jt => jt, jt => jt.GetDisplayName());

    private string _selectedJobType = default!;

    private string _cron = default!;

    private IScheduler _scheduler = default!;

    [Inject] private ISchedulerFactory SchedulerFactory { get; set; } = default!;

    [Inject] private IDockerClient DockerClient { get; set; } = default!;

    [Inject] private IMessageService Messages { get; set; } = default!;

    [Inject] private IConfiguration Configuration { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _scheduler = await SchedulerFactory.GetScheduler();

        var jobKeys = await _scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(JobConstants.Group));

        foreach (var jobKey in jobKeys)
        {
            var jobDetail = await _scheduler.GetJobDetail(jobKey);
            var triggers = await _scheduler.GetTriggersOfJob(jobKey);
            var firstTrigger = triggers.FirstOrDefault();

            var containerName = (string)jobDetail!.JobDataMap[JobConstants.ContainerName];
            var cron = (string)jobDetail.JobDataMap[JobConstants.Cron];
            var jobType = Enum.Parse<JobTypes>((string)jobDetail.JobDataMap[JobConstants.Type]);
            var nextFireTime = firstTrigger?.GetNextFireTimeUtc().ToLocalTime();

            _scheduleEntries.Add(jobKey.Name,
                new ScheduleEntry(
                ++_index,
                jobKey,
                containerName,
                jobDetail.Description!,
                cron,
                jobType,
                nextFireTime));
        }

        _scheduleEntriesQueryable = _scheduleEntries.Values.AsQueryable();

        var containers = await DockerClient.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Limit = 50,
        });

        var exclusions = Configuration.GetContainerExclusions();
        
        foreach (var container in containers)
        {
            var name = container.Names.First().Remove(0, 1);

            if (exclusions.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }
            
            _containers.Add(container.ID, name);
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
                opt.Title = "Validation:";
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
                opt.Title = "Validation:";
                opt.Section = "MESSAGES_TOP";
            });

            return;
        }

        var jobType = Enum.Parse<JobTypes>(_selectedJobType);
        
        var jobBuilder = JobBuilderFactory.For(jobType);

        var job = jobBuilder
            .UsingJobData(JobConstants.ContainerName, _containers[_selectedContainerId])
            .UsingJobData(JobConstants.ContainerId, _selectedContainerId)
            .UsingJobData(JobConstants.Cron, _cron)
            .UsingJobData(JobConstants.Type, jobType.ToString())
            .StoreDurably()
            .DisallowConcurrentExecution()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(job.Key.Name, job.Key.Group)
            .WithCronSchedule(_cron)
            .Build();

        var scheduleEntry = new ScheduleEntry(
            _scheduleEntries.Count + 1,
            job.Key,
            _containers[_selectedContainerId],
            job.Description!,
            _cron,
            jobType,
            trigger.GetNextFireTimeUtc().ToLocalTime());
        
        if (_scheduleEntries.Any(se => se.Value.SameConfiguration(scheduleEntry)))
        {
            await Messages.ShowMessageBarAsync(opt =>
            {
                opt.Intent = MessageIntent.Warning;
                opt.Timeout = 5000;
                opt.Body = "An entry with these parameters already exists";
                opt.Title = "Validation:";
                opt.Section = "MESSAGES_TOP";
            });

            return;
        }

        _scheduleEntries.Add(job.Key.Name, scheduleEntry);

        await _scheduler.ScheduleJob(job, [trigger], replace: true);

        await _grid.RefreshDataAsync();
    }

    /*private Task EditAsync()
    {
        return Task.CompletedTask;
    }*/

    private async Task DeleteAsync(ScheduleEntry entry)
    {
        await _scheduler.DeleteJob(entry.Key);

        _scheduleEntries.Remove(entry.Key.Name);

        _index = 0;

        var scheduleEntries = _scheduleEntries.Values.ToArray();

        foreach (var scheduleEntry in scheduleEntries)
        {
            _scheduleEntries[scheduleEntry.Key.Name] = scheduleEntry with { Index = ++_index };
        }

        await _grid.RefreshDataAsync();
    }

    private sealed record ScheduleEntry(
        int Index,
        JobKey Key,
        string ContainerName,
        string Description,
        string Cron,
        JobTypes Type,
        DateTimeOffset? NextFireTime)
    {
        public bool SameConfiguration(ScheduleEntry anotherEntry)
        {
            return ContainerName == anotherEntry.ContainerName &&
                   Cron == anotherEntry.Cron &&
                   Type == anotherEntry.Type;
        }
    }
}