using System;
using AutoMapper;

namespace Requests;

public class Claim
{
    public string Value { get; set; } = "";
}

public class ClaimCheckboxes
{
    public bool Enabled { get; set; }
    public string Value { get; set; } = "";
}

public class Code
{
    public int Number { get; set; }
}

public class Request
{
    public string Requested { get; set; } = "";
    public Claim Claim { get; set; } = new();
    public Code Code { get; set; } = new();
}

public class RequestDto
{
    public DateTime Requested { get; set; }
    public ClaimCheckboxes Claim { get; set; } = new();
    public int Code { get; set; }
}

public class StringToDateTimeConverter : ITypeConverter<string, DateTime>
{
    public DateTime Convert(string source, DateTime destination, ResolutionContext context)
    {
        if (string.IsNullOrEmpty(source))
        {
            return default(DateTime);
        }

        return DateTime.TryParse(source, out DateTime dateTime) ? dateTime : default(DateTime);
    }
}

public class ClaimsConverter : ITypeConverter<Claim, ClaimCheckboxes>
{
    public ClaimCheckboxes Convert(Claim source, ClaimCheckboxes destination, ResolutionContext context)
    {
        return new ClaimCheckboxes
        {
            Enabled = true,
            Value = source.Value
        };
    }
}

public class CodeConverter : ITypeConverter<Code, int>
{
    public int Convert(Code source, int destination, ResolutionContext context) => source?.Number ?? -1;
}

public class RequestProfile : Profile
{
    public RequestProfile()
    {
        CreateMap<Claim, ClaimCheckboxes>().ConvertUsing<ClaimsConverter>();
        CreateMap<string, DateTime>().ConvertUsing<StringToDateTimeConverter>();
        CreateMap<Code, int>().ConvertUsing(new CodeConverter());
        CreateMap<Request, RequestDto>();
    }
}
