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

        [Fact]
        public void OrderHasId()
        {
            var order = new Order { Id = 3 };
            System.Console.WriteLine(order.Id);
        }
    }
}
