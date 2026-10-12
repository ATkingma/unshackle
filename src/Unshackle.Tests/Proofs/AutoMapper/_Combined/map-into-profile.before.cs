using AutoMapper;
using Shop.Mapping;

namespace Shop.Mapping
{
    public class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
        public string Note { get; set; } = "";
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
        public string Note { get; set; } = "";
    }

    public class Customer
    {
        public string Name { get; set; } = "";
    }

    public class CustomerDto
    {
        public string Name { get; set; } = "";
    }

    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(d => d.Note, o => o.Ignore());
            CreateMap<Customer, CustomerDto>();
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
