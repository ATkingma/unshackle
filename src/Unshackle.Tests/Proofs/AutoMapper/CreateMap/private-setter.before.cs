using AutoMapper;

namespace Shop;

public class WebHookViewModel
{
    public string Url { get; set; } = "";
    public string WebHookId { get; set; } = "";
}

public class WebHookSettings
{
    public string Url { get; set; } = "";
    public string WebHookId { get; private set; } = "";
}

public class WebHookProfile : Profile
{
    public WebHookProfile()
    {
        CreateMap<WebHookViewModel, WebHookSettings>();
    }
}
