using AutoMapper;

namespace Shop
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
            CreateMap<Order, OrderDto>();
            CreateMap<Invoice, InvoiceDto>().BeforeMap((s, d) => d.Id = 1);
        }
    }

    public class ShopService
    {
        private readonly IMapper _mapper;

        public ShopService(IMapper mapper)
        {
            _mapper = mapper;
        }

        public OrderDto Order(Order order) => _mapper.Map<OrderDto>(order);

        public InvoiceDto Invoice(Invoice invoice) => _mapper.Map<InvoiceDto>(invoice);
    }
}
