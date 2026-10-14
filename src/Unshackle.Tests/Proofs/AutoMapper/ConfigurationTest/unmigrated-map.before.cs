using AutoMapper;
using Xunit;

namespace Xunit
{
    public class FactAttribute : System.Attribute
    {
    }
}

namespace Ledger
{
    public class Account
    {
        public string Name { get; set; } = "";
    }

    public class AccountModel
    {
        public string Name { get; set; } = "";
    }

    public static class MapsV1
    {
        public static void CreateMapsForV1(this IProfileExpression options)
        {
            options.CreateMap<Account, AccountModel>();
        }
    }

    public class AccountProfile : Profile
    {
        public AccountProfile()
        {
            this.CreateMapsForV1();
        }
    }

    public class AccountMappingTests
    {
        [Fact]
        public void ConfigurationIsValid()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AccountProfile>());
            config.AssertConfigurationIsValid();
        }
    }
}
