using AutoMapper;

namespace ToDo;

public interface ICdnService
{
    string ImageUri(string name);
}

public class User
{
    public string Name { get; set; } = "";
    public string ImageName { get; set; } = "";
}

public class UserDto
{
    public string Name { get; set; } = "";
    public string ImageUri { get; set; } = "";
}

public class ImageUriResolver : IValueResolver<User, UserDto, string>
{
    private readonly ICdnService _cdnService;

    public ImageUriResolver(ICdnService cdnService)
    {
        _cdnService = cdnService;
    }

    public string Resolve(User source, UserDto destination, string destMember, ResolutionContext context) => _cdnService.ImageUri(source.ImageName);
}

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(x => x.ImageUri, opt => opt.MapFrom<ImageUriResolver>());
    }
}
