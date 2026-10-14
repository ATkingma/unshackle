using AutoMapper;
using Xunit;

namespace Xunit
{
    public class FactAttribute : System.Attribute
    {
    }

    public interface IClassFixture<TFixture>
    {
    }
}

namespace Assistant.Application
{
    public class Recipe
    {
        public string Name { get; set; } = "";
    }

    public class RecipeDto
    {
        public string Name { get; set; } = "";
    }

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Recipe, RecipeDto>();
        }
    }
}

namespace Assistant.Application.UnitTests.Mappings
{
    public class MappingTestsFixture
    {
        public MappingTestsFixture()
        {
            ConfigurationProvider = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<Assistant.Application.MappingProfile>();
            });

            Mapper = ConfigurationProvider.CreateMapper();
        }

        public IConfigurationProvider ConfigurationProvider { get; }

        public IMapper Mapper { get; }
    }

    public class MappingTests : IClassFixture<MappingTestsFixture>
    {
        private readonly IConfigurationProvider _configuration;

        public MappingTests(MappingTestsFixture fixture)
        {
            _configuration = fixture.ConfigurationProvider;
        }

        [Fact]
        public void ShouldHaveValidConfiguration()
        {
            _configuration.AssertConfigurationIsValid();
        }
    }
}
