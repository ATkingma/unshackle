using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Shop.Mapping;

namespace Shop.Mapping
{
    public class Customer
    {
        public string Name { get; set; } = "";
    }

    public class OrderLine
    {
        public int Quantity { get; set; }
        public string Product { get; set; } = "";
    }

    public class Order
    {
        public int Id { get; set; }
        public string Note { get; set; } = "";
        public Customer? Customer { get; set; }
        public List<OrderLine> Lines { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }

    public class CustomerDto
    {
        public string Name { get; set; } = "";
    }

    public class OrderLineDto
    {
        public int Quantity { get; set; }
        public string Product { get; set; } = "";
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string Summary { get; set; } = "";
        public string Note { get; set; } = "";
        public CustomerDto? Customer { get; set; }
        public List<OrderLineDto> Lines { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }

    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(d => d.Note, o => o.Ignore())
                .ForMember(d => d.Summary, o => o.MapFrom(s => s.Note + " #" + s.Id));
            CreateMap<Customer, CustomerDto>();
            CreateMap<OrderLine, OrderLineDto>();
        }
    }
}

namespace Shop.Orders
{
    public class OrderQueries
    {
        private readonly IQueryable<Order> _orders;
        private readonly IMapper _mapper;

        public OrderQueries(IQueryable<Order> orders, IMapper mapper)
        {
            _orders = orders;
            _mapper = mapper;
        }

        public List<OrderDto> Open(int minimum) =>
            _orders
                .Where(o => o.Id >= minimum)
                .OrderBy(o => o.Id)
                .ProjectTo<OrderDto>(_mapper.ConfigurationProvider)
                .ToList();

        public OrderDto? One(int id) => _orders.Where(o => o.Id == id).ProjectTo<OrderDto>(_mapper.ConfigurationProvider).FirstOrDefault();

        public IQueryable<OrderDto> Query() => _mapper.ProjectTo<OrderDto>(_orders);
    }
}
