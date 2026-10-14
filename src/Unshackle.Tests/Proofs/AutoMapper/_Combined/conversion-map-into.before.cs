using AutoMapper;

namespace Shop.Mapping
{
    public enum Status { Open, Closed }

    public class Order
    {
        public int? Quantity { get; set; }
        public Status Status { get; set; }
        public double Weight { get; set; }
    }

    public class OrderDto
    {
        public int Quantity { get; set; }
        public string Status { get; set; } = "";
        public int Weight { get; set; }
    }

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

        public void Update(Mapping.Order order, Mapping.OrderDto dto)
        {
            _mapper.Map(order, dto);
        }
    }
}
