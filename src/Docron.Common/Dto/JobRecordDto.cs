using Docron.Common.Domain;

namespace Docron.Common.Dto;

public sealed class JobRecordDto
{
    public required string Id { get; init; }

    public required string ContainerName { get; init; }

    public required string Description { get; init; }

    public required string Cron { get; init; }

    public required JobTypes JobType { get; init; }

    public DateTimeOffset? NextRun { get; init; }
}