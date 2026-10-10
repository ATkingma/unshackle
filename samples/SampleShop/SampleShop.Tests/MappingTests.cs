using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace SampleShop.Tests;

public class MappingTests
{
    [Fact]
    public void Configuration_IsValid()
    {
        var mapper = new ServiceCollection().AddSampleShop().BuildServiceProvider().GetRequiredService<IMapper>();

        Action act = () => mapper.ConfigurationProvider.AssertConfigurationIsValid();
        act.Should().NotThrow();
    }
}
