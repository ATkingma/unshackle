using System.Reflection;
using AutoMapper;
using NUnit.Framework;

namespace NUnit.Framework
{
    public class TestAttribute : System.Attribute
    {
    }
}

namespace Shop.Application
{
    public class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
    }

    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderDto>();
        }
    }
}

namespace Shop.Application.UnitTests
{
    public class MappingTests
    {
        private readonly IConfigurationProvider _configuration;
        private readonly IMapper _mapper;

        public MappingTests()
        {
            _configuration = new MapperConfiguration(config =>
                config.AddMaps(Assembly.GetAssembly(typeof(Shop.Application.OrderProfile))));

            _mapper = _configuration.CreateMapper();
        }

        [Test]
        public void ShouldHaveValidConfiguration()
        {
            _configuration.AssertConfigurationIsValid();
        }

        [Test]
        public void ShouldMapOrderToOrderDto()
        {
            var order = new Shop.Application.Order { Id = 1, Customer = "Ada" };

            var dto = _mapper.Map<Shop.Application.OrderDto>(order);

            if (dto.Customer != "Ada")
            {
                throw new System.InvalidOperationException();
            }
        }
    }
}
