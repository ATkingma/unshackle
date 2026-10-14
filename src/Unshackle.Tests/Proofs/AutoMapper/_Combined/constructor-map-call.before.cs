using AutoMapper;

namespace Shop.Mapping
{
    public class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
    }

    public record OrderDto(int Id, string Customer);

    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderDto>();
        }
    }
}

namespace Shop.Orders
{
    public class OrderService
    {
        private readonly IMapper _mapper;

        public OrderService(IMapper mapper)
        {
            _mapper = mapper;
        }

        public Mapping.OrderDto Get(Mapping.Order order) => _mapper.Map<Mapping.OrderDto>(order);
    }
}
