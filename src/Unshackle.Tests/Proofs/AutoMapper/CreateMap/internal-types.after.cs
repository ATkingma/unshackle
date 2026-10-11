using AutoMapper;

namespace Shop.Mapping
{
    internal class Order
    {
        public int Number { get; set; }
    }

    internal record OrderDto
    {
        public int Number { get; init; }
    }

    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
        }
    }

    internal static class OrderProfileExtensions
    {
        internal static OrderDto ToOrderDto(this Order source) => new()
        {
            Number = source.Number,
        };
    }
}
