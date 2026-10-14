using AutoMapper;

namespace Shop
{
    public class OrderProfile : Profile
    {
    }

    public static class MappingCheck
    {
        public static void Validate()
        {
            Configure().AssertConfigurationIsValid();
        }

        private static MapperConfiguration Configure() => new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>());
    }
}
