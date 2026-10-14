using AutoMapper;

namespace Shop.Mapping
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
            CreateMap<Order, OrderDto>(MemberList.Destination);
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
