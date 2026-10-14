using AutoMapper;

namespace Nop.Web.Areas.Admin.Infrastructure.Mapper;

public class Affiliate
{
    public int Id { get; set; }
}

public class AffiliateModel
{
    public int Id { get; set; }
}

public partial class AdminMapperConfiguration : Profile
{
    public AdminMapperConfiguration()
    {
        CreateMap<Affiliate, AffiliateModel>();
    }
}

public partial class AdminMapperConfiguration
{
    public int Order => 0;
}
