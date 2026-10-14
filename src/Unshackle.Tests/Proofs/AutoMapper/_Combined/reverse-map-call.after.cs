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
        }
    }

    public static class OrderProfileExtensions
    {
        public static OrderDto ToOrderDto(this Order source) => new()
        {
            Id = source.Id,
            Customer = source.Customer,
        };

        public static Order ToOrder(this OrderDto source) => new()
        {
            Id = source.Id,
            Customer = source.Customer,
            Note = source.Note,
        };

        public static Order MapInto(this OrderDto source, Order destination)
        {
            destination.Id = source.Id;
            destination.Customer = source.Customer;
            destination.Note = source.Note;
            return destination;
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

        public Mapping.OrderDto Get(Mapping.Order order) => order.ToOrderDto();

        public Mapping.Order Create(Mapping.OrderDto dto) => dto.ToOrder();

        public void Update(Mapping.OrderDto dto, Mapping.Order order)
        {
            dto.MapInto(order);
        }
    }
}
