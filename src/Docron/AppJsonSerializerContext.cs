using System.Collections;
using System.Collections.Specialized;
using System.Text.Json.Serialization;
using Docron.Domain;
using Docron.Dto;
using Microsoft.AspNetCore.Mvc;
using Quartz;

namespace Docron;

[JsonSerializable(typeof(ContainerRecordDto[]))]
[JsonSerializable(typeof(CreateJobRecordDto))]
[JsonSerializable(typeof(JobRecordDto[]))]
[JsonSerializable(typeof(KeyValuePair<JobTypes, string>[]))]
[JsonSerializable(typeof(ICalendar))]
[JsonSerializable(typeof(CronExpression))]
[JsonSerializable(typeof(IDictionary))]
[JsonSerializable(typeof(JobDataMap))]
[JsonSerializable(typeof(JobKey))]
[JsonSerializable(typeof(TriggerKey))]
[JsonSerializable(typeof(NameValueCollection))]
[JsonSerializable(typeof(ITrigger))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(ProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}