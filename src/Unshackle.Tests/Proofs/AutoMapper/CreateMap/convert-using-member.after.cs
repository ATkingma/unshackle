using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace Identity;

public class Client
{
    public List<string> AllowedSigningAlgorithms { get; set; } = [];
    public DateTimeOffset? Published { get; set; }
}

public class ClientDto
{
    public string AllowedSigningAlgorithms { get; set; } = "";
    public DateTimeOffset Published { get; set; }
}

public class AllowedSigningAlgorithmsConverter :
    IValueConverter<List<string>, string>,
    IValueConverter<string, List<string>>
{
    public static AllowedSigningAlgorithmsConverter Converter = new AllowedSigningAlgorithmsConverter();

    public string Convert(List<string> sourceMember, ResolutionContext context)
    {
        if (sourceMember == null || !sourceMember.Any())
        {
            return null;
        }

        return sourceMember.Aggregate((x, y) => $"{x},{y}");
    }

    public List<string> Convert(string sourceMember, ResolutionContext context)
    {
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(sourceMember))
        {
            list.AddRange(sourceMember.Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct());
        }

        return list;
    }
}

public class DateTimeOffsetMapper : IValueConverter<DateTimeOffset?, DateTimeOffset>
{
    public DateTimeOffset Convert(DateTimeOffset? sourceMember, ResolutionContext context) => sourceMember?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
}

public class ClientProfile : Profile
{
    public ClientProfile()
    {
    }
}

public static class ClientProfileExtensions
{
    public static ClientDto ToClientDto(this Client source) => new()
    {
        AllowedSigningAlgorithms = AllowedSigningAlgorithmsConverter.Converter.Convert(source.AllowedSigningAlgorithms, null),
        Published = new DateTimeOffsetMapper().Convert(source.Published, null),
    };

    public static Client ToClient(this ClientDto source) => new()
    {
        AllowedSigningAlgorithms = AllowedSigningAlgorithmsConverter.Converter.Convert(source.AllowedSigningAlgorithms, null),
        Published = source.Published,
    };
}
