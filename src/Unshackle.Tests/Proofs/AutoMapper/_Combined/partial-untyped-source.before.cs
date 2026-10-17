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
            CreateMap<Order, OrderDto>();
            CreateMap<Customer, CustomerDto>();
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

        public CustomerDto Customer(object customer) => _mapper.Map<CustomerDto>(customer);
    }
}
