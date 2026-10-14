using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace Shop;

public class Job
{
    public long Id { get; set; }
    public string JobTitle { get; set; } = "";
}

public class JobDto
{
    public long Id { get; set; }
    public string JobTitle { get; set; } = "";
}

public class Employee
{
    public long Id { get; set; }
    public string FirstName { get; set; } = "";
    public Job Job { get; set; } = new();
    public List<Job> History { get; set; } = [];
}

public class EmployeeDto
{
    public long Id { get; set; }
    public string FirstName { get; set; } = "";
    public JobDto Job { get; set; } = new();
    public List<JobDto> History { get; set; } = [];
}

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
    }
}

public static class AutoMapperProfileExtensions
{
    public static EmployeeDto ToEmployeeDto(this Employee source) => new()
    {
        Id = source.Id,
        FirstName = source.FirstName,
        Job = source.Job?.ToJobDto(),
        History = source.History?.Select(x => x?.ToJobDto()).ToList() ?? [],
    };

    public static Employee ToEmployee(this EmployeeDto source) => new()
    {
        Id = source.Id,
        FirstName = source.FirstName,
        Job = source.Job?.ToJob(),
        History = source.History?.Select(x => x?.ToJob()).ToList() ?? [],
    };

    public static JobDto ToJobDto(this Job source) => new()
    {
        Id = source.Id,
        JobTitle = source.JobTitle,
    };

    public static Job ToJob(this JobDto source) => new()
    {
        Id = source.Id,
        JobTitle = source.JobTitle,
    };
}
