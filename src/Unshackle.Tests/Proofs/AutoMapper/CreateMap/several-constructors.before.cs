using AutoMapper;

namespace DasBlog.Web.Mappers
{
    public interface ISiteSecurityManager
    {
    }

    public class User
    {
        public string Name { get; set; } = "";
    }

    public class UserViewModel
    {
        public string Name { get; set; } = "";
    }

    public class ProfileDasBlogUser : Profile
    {
        public ProfileDasBlogUser()
        {

        }

        public ProfileDasBlogUser(ISiteSecurityManager siteSecurityManager)
        {
            CreateMap<User, UserViewModel>();
        }
    }
}
