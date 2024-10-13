using System.Net;
using System.Text.Json;
using Docron.Common;
using Docron.Common.Domain;
using Docron.Common.Dto;
using Docron.UI.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Extensions;

namespace Docron.UI.Pages;

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

    [Inject] private IApiClient ApiClient { get; set; } = default!;
    
    [Inject] private IMessageService Messages { get; set; } = default!;
    
    protected override async Task OnInitializedAsync()
    {
        var jobsResponse = await ApiClient.GetJobsAsync();
        var jobs = jobsResponse.Content!;

        foreach (var job in jobs)
        {
            _scheduleEntries.Add(job.Id, new ScheduleEntry(
                ++_index,
                job.Id,
                job.ContainerName,
                job.Description,
                job.Cron,
                job.JobType,
                job.NextRun.ToLocalTime()));
        }

        _scheduleEntriesQueryable = _scheduleEntries.Values.AsQueryable();

        var containersResponse = await ApiClient.GetContainersAsync();
        var containers = containersResponse.Content!;

        foreach (var container in containers)
        {
            _containers.Add(container.Id, container.Name);
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

        var jobType = Enum.Parse<JobTypes>(_selectedJobType);
        var containerName = _containers[_selectedContainerId];
        
        if (_scheduleEntries.Any(se => se.Value.SameConfiguration(containerName, _cron, jobType)))
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

        var result = await ApiClient.CreateJobAsync(new CreateJobRecordDto
        {
            ContainerId = _selectedContainerId,
            ContainerName = containerName,
            Cron = _cron,
            JobType = jobType
        });

        if (result.StatusCode == HttpStatusCode.BadRequest)
        {
            var message = result.Error!.Message;
            var validationErrors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(message)!;

            var errors = validationErrors["Validation"];
            var validationMessage = string.Join(", ", errors);
            
            await Messages.ShowMessageBarAsync(opt =>
            {
                opt.Intent = MessageIntent.Warning;
                opt.Timeout = 5000;
                opt.Body = validationMessage;
                opt.Title = "Validation:";
                opt.Section = "MESSAGES_TOP";
            });

            return;
        }

        if (!result.IsSuccessStatusCode)
        {
            await Messages.ShowMessageBarAsync(opt =>
            {
                opt.Intent = MessageIntent.Error;
                opt.Timeout = 5000;
                opt.Body = "A new record cannot be created now. Check the logs for more details";
                opt.Title = "Error:";
                opt.Section = "MESSAGES_TOP";
            });

        }
        
        await RefreshGridAsync();
    }

    private async Task DeleteAsync(ScheduleEntry entry)
    {
        var result = await ApiClient.DeleteJobAsync(entry.Id);

        if (result.StatusCode == HttpStatusCode.BadRequest)
        {
            var message = result.Error!.Message;
            var validationErrors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(message)!;

            var errors = validationErrors["Validation"];
            var validationMessage = string.Join(", ", errors);
            
            await Messages.ShowMessageBarAsync(opt =>
            {
                opt.Intent = MessageIntent.Warning;
                opt.Timeout = 5000;
                opt.Body = validationMessage;
                opt.Title = "Validation:";
                opt.Section = "MESSAGES_TOP";
            });

            return;
        }

        if (!result.IsSuccessStatusCode)
        {
            await Messages.ShowMessageBarAsync(opt =>
            {
                opt.Intent = MessageIntent.Error;
                opt.Timeout = 5000;
                opt.Body = "A new record cannot be deleted now. Check the logs for more details";
                opt.Title = "Error:";
                opt.Section = "MESSAGES_TOP";
            });

        }
        
        await RefreshGridAsync();
    }

    private async Task RefreshGridAsync()
    {
        var jobsResponse = await ApiClient.GetJobsAsync();
        var jobs = jobsResponse.Content!;
        
        _scheduleEntries.Clear();

        foreach (var job in jobs)
        {
            _scheduleEntries.Add(job.Id, new ScheduleEntry(
                ++_index,
                job.Id,
                job.ContainerName,
                job.Description,
                job.Cron,
                job.JobType,
                job.NextRun.ToLocalTime()));
        }

        _scheduleEntriesQueryable = _scheduleEntries.Values.AsQueryable();

        //await _grid.RefreshDataAsync();
    }

    private sealed record ScheduleEntry(
        int Index,
        string Id,
        string ContainerName,
        string Description,
        string Cron,
        JobTypes Type,
        DateTimeOffset? NextRun)
    {
        public bool SameConfiguration(string containerName, string cron, JobTypes type)
        {
            return ContainerName == containerName &&
                   Cron == cron &&
                   Type == type;
        }
    }
}