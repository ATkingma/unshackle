using System.Collections.Generic;
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
        CreateMap<Employee, EmployeeDto>().ReverseMap();
        CreateMap<Job, JobDto>().ReverseMap();
    }
}
