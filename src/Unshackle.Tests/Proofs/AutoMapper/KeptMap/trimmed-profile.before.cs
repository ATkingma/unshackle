using AutoMapper;

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

    public class Invoice
    {
        public int Id { get; set; }
    }

    public class InvoiceDto
    {
        public int Id { get; set; }
    }

    public class ShopProfile : Profile
    {
        public ShopProfile()
        {
            CreateMap<Invoice, InvoiceDto>().BeforeMap((s, d) => d.Id = 1);
        }
    }

    public static class ShopProfileExtensions
    {
        public static OrderDto ToOrderDto(this Order source) => new()
        {
            Id = source.Id,
        };
    }
}
