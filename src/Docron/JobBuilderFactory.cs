using Docron.Domain;
using Docron.Jobs;
using MiniGuids;
using Quartz;

namespace Docron;

public static class JobBuilderFactory
{
    public static JobBuilder For(JobTypes jobType)
    {
        var jobKey = new JobKey(MiniGuid.NewGuid(), JobConstants.Group);
        
        switch (jobType)
        {
            case JobTypes.StartContainer:
                return JobBuilder.Create<StartContainerJob>()
                    .WithIdentity(jobKey)
                    .WithDescription(StartContainerJob.Description);
            case JobTypes.StopContainer:
                return JobBuilder.Create<StopContainerJob>()
                    .WithIdentity(jobKey)
                    .WithDescription(StopContainerJob.Description);
            case JobTypes.RestartContainer:
                return JobBuilder.Create<RestartContainerJob>()
                    .WithIdentity(jobKey)
                    .WithDescription(RestartContainerJob.Description);
            case JobTypes.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(jobType), jobType, null);
        }
    }
}