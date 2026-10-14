using AutoMapper;
using Xunit;

namespace Xunit
{
    public class FactAttribute : System.Attribute
    {
    }
}

namespace Shop
{
    public class Order
    {
        public int Id { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
    }

    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderDto>();
        }
    }

    public class OrderProfileTests
    {
        private static readonly IConfigurationProvider s_shared = new MapperConfiguration(cfg => cfg.AddProfile(typeof(OrderProfile)));

        [Fact]
        public void ConfigurationIsValid()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>());
            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void MapperIsValid()
        {
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile(new OrderProfile())).CreateMapper();
            mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }

        [Fact]
        public void SharedIsValid() => s_shared.AssertConfigurationIsValid();

        [Fact]
        public void OrderHasId()
        {
            var order = new Order { Id = 3 };
            System.Console.WriteLine(order.Id);
        }
    }
}
