using Docron.Common.Domain;

namespace Docron.Common.Dto;

public sealed class CreateJobRecordDto
{
    public required string ContainerId { get; init; }
    
    public required string ContainerName { get; init; }

    public required string Cron { get; init; }

    public required JobTypes JobType { get; init; }
}