using AutoMapper;
using Shop.Mapping;

namespace Shop.Mapping
{
    public class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
        public string Note { get; set; } = "";
        public string CreatedBy { get; set; } = "system";
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
        public string Note { get; set; } = "";
    }

    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(d => d.Note, o => o.Ignore())
                .ReverseMap();
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

        public Mapping.Order Create(Mapping.OrderDto dto) => _mapper.Map<Mapping.Order>(dto);

        public void Update(Mapping.OrderDto dto, Mapping.Order order)
        {
            _mapper.Map(dto, order);
        }
    }
}
