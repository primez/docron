namespace Docron.Dto;

public sealed class CreateJobRecordDto
{
    public required string ContainerName { get; init; }

    public required string Cron { get; init; }

    public required string JobType { get; init; }
    
    public required string TimeZoneId { get; init; }
}