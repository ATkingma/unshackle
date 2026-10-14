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
        }
    }

    public static class OrderProfileExtensions
    {
        public static OrderDto ToOrderDto(this Order source) => new()
        {
            Id = source.Id,
            Customer = source.Customer,
        };
    }
}

namespace Shop.Application.UnitTests
{
    public class MappingTests
    {

        [Test]
        public void ShouldMapOrderToOrderDto()
        {
            var order = new Shop.Application.Order { Id = 1, Customer = "Ada" };

            var dto = order.ToOrderDto();

            if (dto.Customer != "Ada")
            {
                throw new System.InvalidOperationException();
            }
        }
    }
}
