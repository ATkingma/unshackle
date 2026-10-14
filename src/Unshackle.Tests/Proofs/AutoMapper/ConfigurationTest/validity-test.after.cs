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
}

namespace Shop.Application.UnitTests
{
    public class MappingTests
    {

        [Test]
        public void ShouldCreateOrder()
        {
            var order = new Shop.Application.Order { Id = 1 };
            System.Console.WriteLine(order.Id);
        }
    }
}
