using AutoMapper;

namespace Shop
{
    public class Line
    {
        public int Id { get; set; }
    }

    public class LineDto
    {
        public int Id { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public Line Line { get; set; } = new();
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public LineDto Line { get; set; } = new();
    }

    public class Customer
    {
        public string Name { get; set; } = "";
    }

    public class CustomerDto
    {
        public string Name { get; set; } = "";
    }

    public class ShopProfile : Profile
    {
        public ShopProfile()
        {
            CreateMap<Order, OrderDto>().BeforeMap((s, d) => d.Id = 1);
            CreateMap<Line, LineDto>();
        }
    }

    public static class ShopProfileExtensions
    {
        public static CustomerDto ToCustomerDto(this Customer source) => new()
        {
            Name = source.Name,
        };
    }

    public class ShopService
    {
        private readonly IMapper _mapper;

        public ShopService(IMapper mapper)
        {
            _mapper = mapper;
        }

        public OrderDto Order(Order order) => _mapper.Map<OrderDto>(order);

        public LineDto Line(Line line) => _mapper.Map<LineDto>(line);

        public CustomerDto Customer(Customer customer) => customer.ToCustomerDto();
    }
}
