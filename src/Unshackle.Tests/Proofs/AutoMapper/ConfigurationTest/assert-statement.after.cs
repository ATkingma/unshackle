using AutoMapper;
using NUnit.Framework;

namespace NUnit.Framework
{
    public class TestAttribute : System.Attribute
    {
    }
}

namespace Shop.Infrastructure
{
    public class AdminMapperConfiguration : Profile
    {
    }

    public static class AutoMapperConfiguration
    {
        public static MapperConfiguration MapperConfiguration { get; private set; } = null!;

        public static void Init(MapperConfiguration config)
        {
            MapperConfiguration = config;
        }
    }

    public class AutoMapperConfigurationTest
    {
        [Test]
        public void ConfigurationIsValid()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(typeof(AdminMapperConfiguration));
            });

            AutoMapperConfiguration.Init(config);
        }
    }
}
