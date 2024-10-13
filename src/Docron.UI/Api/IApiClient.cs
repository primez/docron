using Docron.Common.Dto;
using Refit;

namespace Docron.UI.Api;

public interface IApiClient
{
    [Get("/jobs")]
    Task<IApiResponse<IReadOnlyCollection<JobRecordDto>>> GetJobsAsync();

    [Get("/containers")]
    Task<IApiResponse<IReadOnlyCollection<ContainerRecordDto>>> GetContainersAsync();

    [Post("/jobs")]
    Task<IApiResponse> CreateJobAsync([Body] CreateJobRecordDto request);

    [Delete("/jobs/{id}")]
    Task<IApiResponse> DeleteJobAsync(string id);
}